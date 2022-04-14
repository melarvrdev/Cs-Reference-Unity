// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Profiling;

namespace UnityEngine
{
    public enum RenderMode
    {
        ScreenSpaceOverlay = 0,
        ScreenSpaceCamera = 1,
        WorldSpace = 2
    }

    public enum StandaloneRenderResize
    {
        Enabled = 0,
        Disabled = 1
    }

    [Flags]
    public enum AdditionalCanvasShaderChannels
    {
        None = 0,
        TexCoord1 = 1 << 0,
        TexCoord2 = 1 << 1,
        TexCoord3 = 1 << 2,
        Normal = 1 << 3,
        Tangent = 1 << 4
    }

    [RequireComponent(typeof(RectTransform)),
     NativeClass("UI::Canvas"),
     NativeHeader("Modules/UI/Canvas.h"),
     NativeHeader("Modules/UI/CanvasManager.h"),
     NativeHeader("Modules/UI/UIStructs.h")]
    public sealed class Canvas : Behaviour
    {
        public delegate void WillRenderCanvases();
        public static event WillRenderCanvases preWillRenderCanvases;
        public static event WillRenderCanvases willRenderCanvases;

        public extern RenderMode renderMode { get; set; }
        public extern bool isRootCanvas { get; }
        public extern Rect pixelRect { get; }
        public extern float scaleFactor { get; set; }
        public extern float referencePixelsPerUnit { get; set; }
        public extern bool overridePixelPerfect { get; set; }
        public extern bool pixelPerfect { get; set; }
        public extern float planeDistance { get; set; }
        public extern int renderOrder { get; }
        public extern bool overrideSorting  { get; set; }
        public extern int sortingOrder  { get; set; }
        public extern int targetDisplay  { get; set; }
        public extern int sortingLayerID { get; set; }
        public extern int cachedSortingLayerValue { get; }
        public extern AdditionalCanvasShaderChannels additionalShaderChannels { get; set; }
        public extern string sortingLayerName { get; set; }
        public extern Canvas rootCanvas { get; }

        public extern Vector2 renderingDisplaySize { get; }
        public extern StandaloneRenderResize updateRectTransformForStandalone { get; set; }

        internal static Action<int> externBeginRenderOverlays { get; set; }
        internal static Action<int, int> externRenderOverlaysBefore { get; set; }
        internal static Action<int> externEndRenderOverlays { get; set; }

        [FreeFunction("UI::CanvasManager::SetExternalCanvasEnabled")] internal static extern void SetExternalCanvasEnabled(bool enabled);

        [NativeProperty("Camera", false, TargetType.Function)] public extern Camera worldCamera { get; set; }
        [NativeProperty("SortingBucketNormalizedSize", false, TargetType.Function)] public extern float normalizedSortingGridSize { get; set; }

        [Obsolete("Setting normalizedSize via a int is not supported. Please use normalizedSortingGridSize", false)]
        [NativeProperty("SortingBucketNormalizedSize", false, TargetType.Function)] public extern int sortingGridNormalizedSize { get; set; }

        [Obsolete("Shared default material now used for text and general UI elements, call Canvas.GetDefaultCanvasMaterial()", false)]
        [FreeFunction("UI::GetDefaultUIMaterial")] public static extern Material GetDefaultCanvasTextMaterial();

        [FreeFunction("UI::GetDefaultUIMaterial")] public static extern Material GetDefaultCanvasMaterial();
        [FreeFunction("UI::GetETC1SupportedCanvasMaterial")] public static extern Material GetETC1SupportedCanvasMaterial();

        internal extern void UpdateCanvasRectTransform(bool alignWithCamera);

        internal extern byte stagePriority { get; set; }

        public static void ForceUpdateCanvases()
        {
            SendPreWillRenderCanvases();
            SendWillRenderCanvases();
        }

        [RequiredByNativeCode]
        private static void SendPreWillRenderCanvases()
        {
            preWillRenderCanvases?.Invoke();
        }

        [RequiredByNativeCode]
        private static void SendWillRenderCanvases()
        {
            willRenderCanvases?.Invoke();
        }

        [RequiredByNativeCode]
        private static void BeginRenderExtraOverlays(int displayIndex)
        {
            externBeginRenderOverlays?.Invoke(displayIndex);
        }

        [RequiredByNativeCode]
        private static void RenderExtraOverlaysBefore(int displayIndex, int sortingOrder)
        {
            externRenderOverlaysBefore?.Invoke(displayIndex, sortingOrder);
        }

        [RequiredByNativeCode]
        private static void EndRenderExtraOverlays(int displayIndex)
        {
            externEndRenderOverlays?.Invoke(displayIndex);
        }
    }

    [IgnoredByDeepProfiler]
    [NativeHeader("Modules/UI/Canvas.h"),
     StaticAccessor("UI::SystemProfilerApi", StaticAccessorType.DoubleColon)]
    public static class UISystemProfilerApi
    {
        public enum SampleType
        {
            Layout,
            Render
        }

        public static extern void BeginSample(SampleType type);
        public static extern void EndSample(SampleType type);
        public static extern void AddMarker(string name, Object obj);
    }
}
