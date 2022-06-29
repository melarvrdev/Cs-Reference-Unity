// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [NativeHeader("ModuleOverrides/com.unity.ui/Core/Native/Renderer/UIRMeshBuilder.bindings.h")]
    internal static class MeshBuilderNative
    {
        public const float kEpsilon = 0.001f;

        public struct NativeColorPage
        {
            public int isValid;
            public Color32 pageAndID;
        }

        public struct NativeBorderParams
        {
            public Rect rect;

            public Color leftColor;
            public Color topColor;
            public Color rightColor;
            public Color bottomColor;

            public float leftWidth;
            public float topWidth;
            public float rightWidth;
            public float bottomWidth;

            public Vector2 topLeftRadius;
            public Vector2 topRightRadius;
            public Vector2 bottomRightRadius;
            public Vector2 bottomLeftRadius;

            internal NativeColorPage leftColorPage;
            internal NativeColorPage topColorPage;
            internal NativeColorPage rightColorPage;
            internal NativeColorPage bottomColorPage;
        }

        public struct NativeRectParams
        {
            public Rect rect;
            public Rect uv;
            public Rect uvRegion;
            public Color color;
            public ScaleMode scaleMode;

            public Vector2 topLeftRadius;
            public Vector2 topRightRadius;
            public Vector2 bottomRightRadius;
            public Vector2 bottomLeftRadius;

            public Vector2 textureSize;
            public float texturePixelsPerPoint;

            public int leftSlice;
            public int topSlice;
            public int rightSlice;
            public int bottomSlice;
            public float sliceScale;

            public NativeColorPage colorPage;
        }

        public static extern MeshWriteDataInterface MakeBorder(NativeBorderParams borderParams,float posZ);
        public static extern MeshWriteDataInterface MakeSolidRect(NativeRectParams rectParams,float posZ);
        public static extern MeshWriteDataInterface MakeTexturedRect(NativeRectParams rectParams,float posZ);
        public static extern MeshWriteDataInterface MakeVectorGraphicsStretchBackground(Vertex[] svgVertices, UInt16[] svgIndices, float svgWidth, float svgHeight, Rect targetRect, Rect sourceUV, ScaleMode scaleMode, Color tint, int settingIndexOffset, ref int finalVertexCount, ref int finalIndexCount);
        public static extern MeshWriteDataInterface MakeVectorGraphics9SliceBackground(Vertex[] svgVertices, UInt16[] svgIndices, float svgWidth, float svgHeight, Rect targetRect, Vector4 sliceLTRB, Color tint, int settingIndexOffset);
    }
}
