// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Modules/AssetPipelineEditor/Public/MonoImporter.h")]
    [NativeHeader("Modules/AssetPipelineEditor/Public/MonoImporter.bindings.h")]
    [ExcludeFromPreset]
    public class MonoImporter : AssetImporter
    {
        public extern void SetDefaultReferences(string[] name, Object[] target);

        [FreeFunction("MonoImporterBindings::GetAllRuntimeMonoScripts")]
        public static extern MonoScript[] GetAllRuntimeMonoScripts();

        [FreeFunction("MonoImporterBindings::SetMonoScriptExecutionOrder")]
        public static extern void SetExecutionOrder([NotNull("NullExceptionObject")] MonoScript script, int order);

        [FreeFunction("MonoImporterBindings::GetExecutionOrder")]
        public static extern int GetExecutionOrder([NotNull("NullExceptionObject")] MonoScript script);

        public extern MonoScript GetScript();

        public Object GetDefaultReference(string name)
        {
            return GetDefaultReference(name, out _);
        }
        internal extern Object GetDefaultReference(string name, out int instanceId);

        public extern void SetIcon(Texture2D icon);
        public extern Texture2D GetIcon();
    }
}
