// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Modules/AssetPipelineEditor/Public/AndroidAssetPackImporter.h")]
    [ExcludeFromPreset]
    public class AndroidAssetPackImporter : AssetImporter
    {
        extern public static AndroidAssetPackImporter[] GetAllImporters();
    }
}
