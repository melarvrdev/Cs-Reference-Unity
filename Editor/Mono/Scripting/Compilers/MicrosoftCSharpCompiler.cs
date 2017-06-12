// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Modules;
using UnityEditor.Utils;

namespace UnityEditor.Scripting.Compilers
{
    internal class MicrosoftCSharpCompiler : ScriptCompilerBase
    {
        public MicrosoftCSharpCompiler(MonoIsland island, bool runUpdater)
            : base(island)
        {
        }

        private BuildTarget BuildTarget { get { return _island._target; } }

        internal static string ProgramFilesDirectory
        {
            get
            {
                string programFiles;

                string programFilesOS64 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                // Note: For 32 bit applications quering env variable 'ProgramFiles' will return same string as quering 'ProgramFiles(x86)'

                if (Directory.Exists(programFilesOS64)) programFiles = programFilesOS64;
                else
                {
                    UnityEngine.Debug.Log("Env variables ProgramFiles(x86) & ProgramFiles didn't exist, trying hard coded paths");
                    string mainLetter = Path.GetFullPath(Environment.GetEnvironmentVariable("windir") + "\\..\\..");
                    string hardCodedProgramFilesOS64 = mainLetter + "Program Files (x86)";
                    string hardCodedprogramFilesOS32 = mainLetter + "Program Files";

                    // First check Program Files (x86) if such directory doesn't exist, it means we're on 32 bit system.
                    if (Directory.Exists(hardCodedProgramFilesOS64)) programFiles = hardCodedProgramFilesOS64;
                    else if (Directory.Exists(hardCodedprogramFilesOS32)) programFiles = hardCodedprogramFilesOS32;
                    else
                    {
                        throw new System.Exception("Path '" + hardCodedProgramFilesOS64 + "' or '" + hardCodedprogramFilesOS32 + "' doesn't exist.");
                    }
                }
                return programFiles;
            }
        }

        private static string[] GetReferencesFromMonoDistribution()
        {
            return new[]
            {
                "mscorlib.dll",
                "System.dll",
                "System.Core.dll",
                "System.Runtime.Serialization.dll",
                "System.Xml.dll",
                "System.Xml.Linq.dll",
                "UnityScript.dll",
                "UnityScript.Lang.dll",
                "Boo.Lang.dll",
            };
        }

        internal static string GetNETCoreFrameworkReferencesDirectory(WSASDK wsaSDK)
        {
            switch (wsaSDK)
            {
                case WSASDK.SDK80:
                    return ProgramFilesDirectory + @"\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5";
                case WSASDK.SDK81:
                    return ProgramFilesDirectory + @"\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5.1";
                case WSASDK.PhoneSDK81:
                    return ProgramFilesDirectory + @"\Reference Assemblies\Microsoft\Framework\WindowsPhoneApp\v8.1";
                case WSASDK.UWP:
                    // For UWP, framework path doesn't exist, to get assemblies you need to use project.lock with NuGetAssemblyResolver file
                    return null;
                default:
                    throw new Exception("Unknown Windows SDK: " + wsaSDK.ToString());
            }
        }

        private string[] GetClassLibraries()
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(BuildTarget);
            if (PlayerSettings.GetScriptingBackend(buildTargetGroup) != ScriptingImplementation.WinRTDotNET)
            {
                var monoAssemblyDirectory = GetMonoProfileLibDirectory();
                var classLibraries = new List<string>();
                classLibraries.AddRange(GetReferencesFromMonoDistribution().Select(dll => Path.Combine(monoAssemblyDirectory, dll)));

                if (PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup) == ApiCompatibilityLevel.NET_4_6)
                {
                    // Assemblies compiled against .NET 4.6 in Visual Studio might reference these 5 assemblies.
                    // In mono class libraries, they have no type definitions, and just forward all types to mscorlib
                    var facadesDirectory = Path.Combine(monoAssemblyDirectory, "Facades");
                    classLibraries.Add(Path.Combine(facadesDirectory, "System.ObjectModel.dll"));
                    classLibraries.Add(Path.Combine(facadesDirectory, "System.Runtime.dll"));
                    classLibraries.Add(Path.Combine(facadesDirectory, "System.Runtime.InteropServices.WindowsRuntime.dll"));
                    classLibraries.Add(Path.Combine(monoAssemblyDirectory, "System.Numerics.dll"));
                    classLibraries.Add(Path.Combine(monoAssemblyDirectory, "System.Numerics.Vectors.dll"));
                }

                return classLibraries.ToArray();
            }

            if (BuildTarget != BuildTarget.WSAPlayer)
                throw new InvalidOperationException(string.Format("MicrosoftCSharpCompiler cannot build for .NET Scripting backend for BuildTarget.{0}.", BuildTarget));

            var wsaSDK = WSASDK.UWP;
            if (wsaSDK != WSASDK.UWP)
                return Directory.GetFiles(GetNETCoreFrameworkReferencesDirectory(wsaSDK), "*.dll");

            var resolver = new NuGetPackageResolver { ProjectLockFile = @"UWP\project.lock.json" };
            return resolver.Resolve();
        }

        private void FillCompilerOptions(List<string> arguments, out string argsPrefix)
        {
            // This will ensure that csc.exe won't include csc.rsp
            // csc.rsp references .NET 4.5 assemblies which cause conflicts for us
            argsPrefix = "/noconfig ";
            arguments.Add("/nostdlib+");

            // Case 755238: Always use english for outputing errors, the same way as Mono compilers do
            arguments.Add("/preferreduilang:en-US");

            var platformSupportModule = ModuleManager.FindPlatformSupportModule(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget));
            var compilationExtension = platformSupportModule.CreateCompilationExtension();

            arguments.AddRange(GetClassLibraries().Select(r => "/reference:\"" + r + "\""));
            arguments.AddRange(compilationExtension.GetAdditionalAssemblyReferences().Select(r => "/reference:\"" + r + "\""));
            arguments.AddRange(compilationExtension.GetWindowsMetadataReferences().Select(r => "/reference:\"" + r + "\""));
            arguments.AddRange(compilationExtension.GetAdditionalDefines().Select(d => "/define:" + d));
            arguments.AddRange(compilationExtension.GetAdditionalSourceFiles());
        }

        private static void ThrowCompilerNotFoundException(string path)
        {
            throw new Exception(string.Format("'{0}' not found. Is your Unity installation corrupted?", path));
        }

        private Program StartCompilerImpl(List<string> arguments, string argsPrefix)
        {
            foreach (string dll in _island._references)
                arguments.Add("/reference:" + PrepareFileName(dll));

            foreach (string define in _island._defines.Distinct())
                arguments.Add("/define:" + define);

            foreach (string source in _island._files)
            {
                arguments.Add(PrepareFileName(source).Replace('/', '\\'));
            }

            var coreRun = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "Roslyn", "CoreRun.exe").Replace('/', '\\');
            var csc = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "Roslyn", "csc.exe").Replace('/', '\\');

            if (!File.Exists(coreRun))
                ThrowCompilerNotFoundException(coreRun);

            if (!File.Exists(csc))
                ThrowCompilerNotFoundException(csc);

            AddCustomResponseFileIfPresent(arguments, "csc.rsp");

            var responseFile = CommandLineFormatter.GenerateResponseFile(arguments);
            var psi = new ProcessStartInfo() { Arguments = "\"" + csc + "\" " + argsPrefix + "@" + responseFile, FileName = coreRun, CreateNoWindow = true };
            var program = new Program(psi);
            program.Start();
            return program;
        }

        protected override Program StartCompiler()
        {
            var outputPath = PrepareFileName(_island._output);

            // Always build with "/debug:pdbonly", "/optimize+", because even if the assembly is optimized
            // it seems you can still succesfully debug C# scripts in Visual Studio
            var arguments = new List<string>
            {
                "/debug:pdbonly",
                "/optimize+",
                "/target:library",
                "/nowarn:0169",
                "/unsafe",
                "/out:" + outputPath
            };

            string argsPrefix;
            FillCompilerOptions(arguments, out argsPrefix);
            return StartCompilerImpl(arguments, argsPrefix);
        }

        protected override string[] GetStreamContainingCompilerMessages()
        {
            return GetStandardOutput();
        }

        protected override CompilerOutputParserBase CreateOutputParser()
        {
            return new MicrosoftCSharpCompilerOutputParser();
        }
    }
}
