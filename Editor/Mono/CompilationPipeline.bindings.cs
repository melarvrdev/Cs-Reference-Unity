// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.Compilation
{
    [Flags]
    enum CompilationSetupErrors     // Keep in sync with enum CompilationSetupErrors::Flags in ScriptCompilationPipeline.h
    {
        None                        = 0,
        LoadError                   = 1 << 0, // set when AssemblyDefinitionException is thrown
        All                         = LoadError,
    };

    [NativeHeader("Editor/Src/ScriptCompilation/ScriptCompilationPipeline.h")]
    public static partial class CompilationPipeline
    {
        [FreeFunction]
        internal static extern void DisableScriptDebugInfo();

        [FreeFunction]
        internal static extern void EnableScriptDebugInfo();

        [FreeFunction]
        internal static extern bool IsScriptDebugInfoEnabled();
    }
}
