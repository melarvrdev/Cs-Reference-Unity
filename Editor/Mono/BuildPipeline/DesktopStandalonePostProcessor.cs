// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Modules;
using UnityEditorInternal;
using UnityEngine;

internal abstract class DesktopStandalonePostProcessor : BeeBuildPostprocessor
{
    public override bool SupportsLz4Compression() => true;
    public override bool SupportsScriptsOnlyBuild() => true;
    public override bool SupportsInstallInBuildFolder() => true;
    protected abstract string GetPlatformString(BuildPostProcessArgs args);
    protected override IPluginImporterExtension GetPluginImpExtension() => new DesktopPluginImporterExtension();

    protected virtual string GetVariationName(BuildPostProcessArgs args)
    {
        return string.Format("{0}_{1}_{2}_{3}",
            GetPlatformString(args),
            GetServer(args) ? "server" : "player",
            GetDevelopment(args) ? "development" : "nondevelopment",
            GetUseIl2Cpp(args) ? "il2cpp" : "mono");
    }

    protected bool GetServer(BuildPostProcessArgs args) =>
        (args.target == BuildTarget.StandaloneWindows ||
            args.target == BuildTarget.StandaloneWindows64 ||
            args.target == BuildTarget.StandaloneOSX ||
            args.target == BuildTarget.StandaloneLinux64) &&
        (StandaloneBuildSubtarget)args.subtarget == StandaloneBuildSubtarget.Server;

    protected string GetVariationFolder(BuildPostProcessArgs args) =>
        $"{args.playerPackage}/Variations/{GetVariationName(args)}";

    public override void UpdateBootConfig(BuildTarget target, BootConfigData config, BuildOptions options)
    {
        base.UpdateBootConfig(target, config, options);
        if (PlayerSettings.forceSingleInstance)
            config.AddKey("single-instance");
        if (!PlayerSettings.useFlipModelSwapchain)
            config.AddKey("force-d3d11-bltblt-mode");
        if (IL2CPPUtils.UseIl2CppCodegenWithMonoBackend(BuildPipeline.GetBuildTargetGroup(target)))
            config.Set("mono-codegen", "il2cpp");
        if ((options & BuildOptions.EnableCodeCoverage) != 0)
            config.Set("enableCodeCoverage", "1");
        if (!PlayerSettings.usePlayerLog)
            config.AddKey("nolog");
    }

    public override void LaunchPlayer(BuildLaunchPlayerArgs args)
    {
        // This happens directly from BuildPlayer.cpp
    }

    readonly bool m_HasMonoPlayers;
    readonly bool m_HasIl2CppPlayers;

    protected DesktopStandalonePostProcessor(bool hasMonoPlayers, bool hasIl2CppPlayers)
    {
        m_HasMonoPlayers = hasMonoPlayers;
        m_HasIl2CppPlayers = hasIl2CppPlayers;
    }

    public override string PrepareForBuild(BuildOptions options, BuildTarget target)
    {
        if (!m_HasMonoPlayers)
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
            if (PlayerSettings.GetScriptingBackend(buildTargetGroup) != ScriptingImplementation.IL2CPP)
                return "Currently selected scripting backend (Mono) is not installed.";
        }

        if (!m_HasIl2CppPlayers)
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
            if (PlayerSettings.GetScriptingBackend(buildTargetGroup) == ScriptingImplementation.IL2CPP)
                return "Currently selected scripting backend (IL2CPP) is not installed.";
        }

        return null;
    }

    internal class ScriptingImplementations : DefaultScriptingImplementations
    {
    }
}
