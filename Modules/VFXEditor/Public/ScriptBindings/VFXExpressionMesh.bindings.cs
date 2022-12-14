// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace UnityEditor.VFX
{
    [RequiredByNativeCode]
    [NativeType(Header = "Modules/VFX/Public/VFXExpressionMeshFunctions.h")]
    [StaticAccessor("VFXExpressionMeshFunctions", StaticAccessorType.DoubleColon)]
    internal class VFXExpressionMesh
    {
        extern static internal uint GetVertexCount(Mesh mesh);

        extern static internal uint GetVertexStride(Mesh mesh, uint channelIndex);
        extern static internal uint GetChannelOffset(Mesh mesh, uint channelIndex);
        extern static internal uint GetChannelInfos(Mesh mesh, uint channelIndex);

        extern static internal float GetFloat(Mesh mesh, uint vertexOffset, uint channelInfos);
        extern static internal Vector2 GetFloat2(Mesh mesh, uint vertexOffset, uint channelInfos);
        extern static internal Vector3 GetFloat3(Mesh mesh, uint vertexOffset, uint channelInfos);
        extern static internal Vector4 GetFloat4(Mesh mesh, uint vertexOffset, uint channelInfos);
        extern static internal Vector4 GetColor(Mesh mesh, uint vertexOffset, uint channelInfos);

        extern static internal uint GetIndexFormat(Mesh mesh);
        extern static internal uint GetIndexCount(Mesh mesh);
        extern static internal uint GetIndex(Mesh mesh, uint index, uint indexFormat);
    }
}
