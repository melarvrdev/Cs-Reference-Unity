// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Reporting;
using System.Globalization;
using System.Text.RegularExpressions;

namespace UnityEditor.Build.Reporting
{
    internal abstract class StrippingInfoWithSizeAnalysis : StrippingInfo
    {
        protected abstract Dictionary<string, string> GetModuleArtifacts();
        protected abstract Dictionary<string, string> GetSymbolArtifacts();
        protected abstract Dictionary<string, int> GetFunctionSizes();
        protected abstract void AddPlatformSpecificCodeOutputModules();

        static private Dictionary<string, string> GetIl2CPPAssemblyMapArtifacts(string path)
        {
            var result = new Dictionary<string, string>();
            var lines = File.ReadAllLines(path);
            foreach (var l in lines)
            {
                var split = l.Split('\t');
                if (split.Length == 3)
                {
                    result[split[0]] = split[2];
                }
            }
            return result;
        }

        static private void OutputSizes(Dictionary<string, int> sizes, int totalLines)
        {
            var functionSizesList = sizes.ToList();
            functionSizesList.Sort((firstPair, nextPair) =>
            {
                return nextPair.Value.CompareTo(firstPair.Value);
            }
            );
            foreach (var f in functionSizesList)
            {
                if (f.Value < 10000)
                    break;
                System.Console.WriteLine(f.Value.ToString("D6") + " " + (f.Value * 100.0 / totalLines).ToString("F2", CultureInfo.InvariantCulture.NumberFormat) + "% " + f.Key);
            }
        }

        static private void PrintSizesDictionary(Dictionary<string, int> sizes, int maxSize)
        {
            List<KeyValuePair<string, int>> myList = sizes.ToList();

            myList.Sort(
                delegate(KeyValuePair<string, int> pair1,
                    KeyValuePair<string, int> pair2)
                {
                    return pair2.Value.CompareTo(pair1.Value);
                }
            );

            for (int i = 0; i < maxSize && i < myList.Count; i++)
                System.Console.WriteLine(myList[i].Value.ToString("D8") + " " + myList[i].Key);
        }

        static private void PrintSizesDictionary(Dictionary<string, int> sizes)
        {
            PrintSizesDictionary(sizes, 500);
        }

        protected Dictionary<string, int> folderSizes = new Dictionary<string, int>();
        protected Dictionary<string, int> moduleSizes = new Dictionary<string, int>();
        protected Dictionary<string, int> assemblySizes = new Dictionary<string, int>();
        protected Dictionary<string, int> objectSizes = new Dictionary<string, int>();

        protected virtual string MethodMapPath => "Temp/StagingArea/Data/il2cppOutput/Symbols/MethodMap.tsv";

        public void Analyze()
        {
            var doSourceCodeAnalysis = Unsupported.IsDeveloperMode();

            var symbolArtifacts = GetSymbolArtifacts();
            var moduleArtifacts = GetModuleArtifacts();
            var assemblyArtifacts = GetIl2CPPAssemblyMapArtifacts(MethodMapPath);

            int moduleAccounted = 0;

            var functionSizes = GetFunctionSizes();

            Regex moduleNameParser = new Regex(@"WebGLSupport_(.*)Module_Dynamic", RegexOptions.IgnoreCase);

            foreach (var functionSize in functionSizes)
            {
                if (symbolArtifacts.ContainsKey(functionSize.Key))
                {
                    var objectFile = symbolArtifacts[functionSize.Key].Replace('\\', '/');
                    if (doSourceCodeAnalysis)
                    {
                        if (!objectSizes.ContainsKey(objectFile))
                            objectSizes[objectFile] = 0;
                        objectSizes[objectFile] += functionSize.Value;
                    }
                    if (objectFile.LastIndexOf('/') != -1)
                    {
                        var objectFolder = objectFile.Substring(0, objectFile.LastIndexOf('/'));
                        if (!folderSizes.ContainsKey(objectFolder))
                            folderSizes[objectFolder] = 0;
                        folderSizes[objectFolder] += functionSize.Value;
                    }
                }
                if (moduleArtifacts.ContainsKey(functionSize.Key))
                {
                    Match m = moduleNameParser.Match(moduleArtifacts[functionSize.Key]);
                    string moduleName = m.Success ? StrippingInfo.ModuleName(m.Groups[1].ToString()) : moduleArtifacts[functionSize.Key];

                    if (!moduleSizes.ContainsKey(moduleName))
                        moduleSizes[moduleName] = 0;

                    moduleSizes[moduleName] += functionSize.Value;

                    moduleAccounted += functionSize.Value;
                }
                if (assemblyArtifacts.ContainsKey(functionSize.Key))
                {
                    var assembly = assemblyArtifacts[functionSize.Key];
                    if (!assemblySizes.ContainsKey(assembly))
                        assemblySizes[assembly] = 0;

                    assemblySizes[assembly] += functionSize.Value;
                }
            }

            AddPlatformSpecificCodeOutputModules();

            int unaccounted = totalSize;
            foreach (var moduleSize in moduleSizes)
                if (modules.Contains(moduleSize.Key))
                    unaccounted -= moduleSize.Value;

            moduleSizes["Unaccounted"] = unaccounted;
            AddModule("Unaccounted", false);
            foreach (var moduleSize in moduleSizes)
                AddModuleSize(moduleSize.Key, moduleSize.Value);

            int totalAssemblySize = 0;
            foreach (var assemblySize in assemblySizes)
            {
                RegisterDependency("IL2CPP Generated", assemblySize.Key);
                sizes[assemblySize.Key] = assemblySize.Value;
                totalAssemblySize += assemblySize.Value;
                SetIcon(assemblySize.Key, "class/DefaultAsset");
            }
            RegisterDependency("IL2CPP Generated", "IL2CPP Unaccounted");
            sizes["IL2CPP Unaccounted"] = moduleSizes["IL2CPP Generated"] - totalAssemblySize;
            SetIcon("IL2CPP Unaccounted", "class/DefaultAsset");

            if (doSourceCodeAnalysis)
            {
                System.Console.WriteLine("Code size per module: ");
                PrintSizesDictionary(moduleSizes);
                System.Console.WriteLine("\n\n");

                System.Console.WriteLine("Code size per source folder: ");
                PrintSizesDictionary(folderSizes);
                System.Console.WriteLine("\n\n");

                System.Console.WriteLine("Code size per object file: ");
                PrintSizesDictionary(objectSizes);
                System.Console.WriteLine("\n\n");

                System.Console.WriteLine("Code size per function: ");
                PrintSizesDictionary(functionSizes);
                System.Console.WriteLine("\n\n");
            }
        }
    }
}
