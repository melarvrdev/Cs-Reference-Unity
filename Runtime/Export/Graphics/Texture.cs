// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using uei = UnityEngine.Internal;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Scripting;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine
{
    public struct RenderTextureDescriptor
    {
        public int width { get; set; }
        public int height { get; set; }
        public int msaaSamples { get; set; }
        public int volumeDepth { get; set; }
        public int mipCount { get; set; }

        private GraphicsFormat _graphicsFormat;// { get; set; }

        public GraphicsFormat graphicsFormat
        {
            get { return _graphicsFormat; }

            set
            {
                _graphicsFormat = value;
                SetOrClearRenderTextureCreationFlag(GraphicsFormatUtility.IsSRGBFormat(value), RenderTextureCreationFlags.SRGB);
            }
        }

        public GraphicsFormat stencilFormat { get; set; }

        public GraphicsFormat depthStencilFormat { get; set; }

        public RenderTextureFormat colorFormat
        {
            get { return GraphicsFormatUtility.GetRenderTextureFormat(graphicsFormat); }
            set { graphicsFormat = SystemInfo.GetCompatibleFormat(GraphicsFormatUtility.GetGraphicsFormat(value, sRGB), FormatUsage.Render); }
        }

        public bool sRGB
        {
            get { return GraphicsFormatUtility.IsSRGBFormat(graphicsFormat); }
            set { graphicsFormat = GraphicsFormatUtility.GetGraphicsFormat(colorFormat, value); }
            // The code below does not work (while it feels like it should)
            // This is because it does not take the project settings into account (unlike GraphicsFormatUtility.GetGraphicsFormat) and so we don't have the fallback kicking in (problem when some formats are not supported)
            // We should fix this once we do a cleanup of colorspace management inside Unity
            //set { graphicsFormat = (value) ? GraphicsFormatUtility.GetSRGBFormat(graphicsFormat)  : GraphicsFormatUtility.GetLinearFormat(graphicsFormat); }
        }


        public int depthBufferBits
        {
            get { return GraphicsFormatUtility.GetDepthBits(depthStencilFormat); }
            set { depthStencilFormat = GraphicsFormatUtility.GetDepthStencilFormat(value); }
        }

        public Rendering.TextureDimension dimension { get; set; }
        public Rendering.ShadowSamplingMode shadowSamplingMode { get; set; }
        public VRTextureUsage vrUsage { get; set; }
        private RenderTextureCreationFlags _flags;
        public RenderTextureCreationFlags flags { get { return _flags; } }
        public RenderTextureMemoryless memoryless { get; set; }

        public RenderTextureDescriptor(int width, int height)
            : this(width, height, RenderTextureFormat.Default)
        {
        }

        public RenderTextureDescriptor(int width, int height, RenderTextureFormat colorFormat)
            : this(width, height, colorFormat, 0)
        {
        }

        public RenderTextureDescriptor(int width, int height, RenderTextureFormat colorFormat, int depthBufferBits)
            : this(width, height, colorFormat, depthBufferBits, Texture.GenerateAllMips)
        {
        }

        [uei.ExcludeFromDocs]
        public RenderTextureDescriptor(int width, int height, GraphicsFormat colorFormat, int depthBufferBits)
            : this(width, height, colorFormat, depthBufferBits, Texture.GenerateAllMips)
        {
        }

        public RenderTextureDescriptor(int width, int height, RenderTextureFormat colorFormat, int depthBufferBits, int mipCount)
            : this(width, height, SystemInfo.GetCompatibleFormat(GraphicsFormatUtility.GetGraphicsFormat(colorFormat, false), FormatUsage.Render), depthBufferBits, mipCount)
        {
        }

        [uei.ExcludeFromDocs]
        public RenderTextureDescriptor(int width, int height, GraphicsFormat colorFormat, int depthBufferBits, int mipCount) : this()
        {
            _flags = RenderTextureCreationFlags.AutoGenerateMips | RenderTextureCreationFlags.AllowVerticalFlip; // Set before graphicsFormat to avoid erasing the flag set by graphicsFormat
            this.width = width;
            this.height = height;
            volumeDepth = 1;
            msaaSamples = 1;
            this.graphicsFormat = colorFormat;
            this.depthStencilFormat = GetDepthStencilFormatLegacy(depthBufferBits, colorFormat);
            this.mipCount = mipCount;
            dimension = Rendering.TextureDimension.Tex2D;
            shadowSamplingMode =  Rendering.ShadowSamplingMode.None;
            vrUsage = VRTextureUsage.None;
            memoryless = RenderTextureMemoryless.None;
        }

        private static GraphicsFormat GetDepthStencilFormatLegacy(int depthBits, GraphicsFormat colorFormat)
        {
            return (colorFormat == GraphicsFormat.ShadowAuto) ?
                GraphicsFormatUtility.GetDepthStencilFormat(depthBits, 0)
                : GraphicsFormatUtility.GetDepthStencilFormat(depthBits);
        }

        [uei.ExcludeFromDocs]
        public RenderTextureDescriptor(int width, int height, GraphicsFormat colorFormat, GraphicsFormat depthStencilFormat) : this(width, height, colorFormat, depthStencilFormat, Texture.GenerateAllMips)
        {
        }

        [uei.ExcludeFromDocs]
        public RenderTextureDescriptor(int width, int height, GraphicsFormat colorFormat, GraphicsFormat depthStencilFormat, int mipCount) : this()
        {
            _flags = RenderTextureCreationFlags.AutoGenerateMips | RenderTextureCreationFlags.AllowVerticalFlip; // Set before graphicsFormat to avoid erasing the flag set by graphicsFormat
            this.width = width;
            this.height = height;
            volumeDepth = 1;
            msaaSamples = 1;
            this.graphicsFormat = colorFormat;
            this.depthStencilFormat = depthStencilFormat;
            this.mipCount = mipCount;
            dimension = Rendering.TextureDimension.Tex2D;
            shadowSamplingMode = Rendering.ShadowSamplingMode.None;
            vrUsage = VRTextureUsage.None;
            memoryless = RenderTextureMemoryless.None;
        }

        private void SetOrClearRenderTextureCreationFlag(bool value, RenderTextureCreationFlags flag)
        {
            if (value)
            {
                _flags |= flag;
            }
            else
            {
                _flags &= ~flag;
            }
        }

        public bool useMipMap
        {
            get { return (_flags & RenderTextureCreationFlags.MipMap) != 0; }
            set { SetOrClearRenderTextureCreationFlag(value, RenderTextureCreationFlags.MipMap); }
        }

        public bool autoGenerateMips
        {
            get { return (_flags & RenderTextureCreationFlags.AutoGenerateMips) != 0; }
            set { SetOrClearRenderTextureCreationFlag(value, RenderTextureCreationFlags.AutoGenerateMips); }
        }

        public bool enableRandomWrite
        {
            get { return (_flags & RenderTextureCreationFlags.EnableRandomWrite) != 0; }
            set { SetOrClearRenderTextureCreationFlag(value, RenderTextureCreationFlags.EnableRandomWrite); }
        }

        public bool bindMS
        {
            get { return (_flags & RenderTextureCreationFlags.BindMS) != 0; }
            set { SetOrClearRenderTextureCreationFlag(value, RenderTextureCreationFlags.BindMS); }
        }

        internal bool createdFromScript
        {
            get { return (_flags & RenderTextureCreationFlags.CreatedFromScript) != 0; }
            set { SetOrClearRenderTextureCreationFlag(value, RenderTextureCreationFlags.CreatedFromScript); }
        }

        public bool useDynamicScale
        {
            get { return (_flags & RenderTextureCreationFlags.DynamicallyScalable) != 0; }
            set { SetOrClearRenderTextureCreationFlag(value, RenderTextureCreationFlags.DynamicallyScalable); }
        }
    }

    public partial class RenderTexture : Texture
    {
        [RequiredByNativeCode] // used to create builtin textures
        internal protected RenderTexture()
        {
        }

        public RenderTexture(RenderTextureDescriptor desc)
        {
            ValidateRenderTextureDesc(desc);
            Internal_Create(this);
            SetRenderTextureDescriptor(desc);
        }

        public RenderTexture(RenderTexture textureToCopy)
        {
            if (textureToCopy == null)
                throw new ArgumentNullException("textureToCopy");

            ValidateRenderTextureDesc(textureToCopy.descriptor);
            Internal_Create(this);
            SetRenderTextureDescriptor(textureToCopy.descriptor);
        }

        [uei.ExcludeFromDocs]
        public RenderTexture(int width, int height, int depth, DefaultFormat format)
            : this(width, height, depth, SystemInfo.GetGraphicsFormat(format))
        {
        }

        [uei.ExcludeFromDocs]
        public RenderTexture(int width, int height, int depth, GraphicsFormat format)
            : this(width, height, depth, format, Texture.GenerateAllMips)
        {
        }

        [uei.ExcludeFromDocs]
        public RenderTexture(int width, int height, int depth, GraphicsFormat format, int mipCount)
        {
            // Note: the code duplication here is because you can't set a descriptor with
            // zero width/height, which our own code (and possibly existing user code) relies on.
            if (!ValidateFormat(format, FormatUsage.Render))
                return;

            Internal_Create(this);
            this.depthStencilFormat = GetDepthStencilFormatLegacy(depth, format);
            this.width = width; this.height = height; this.graphicsFormat = format; SetMipMapCount(mipCount);

            SetSRGBReadWrite(GraphicsFormatUtility.IsSRGBFormat(format));
        }

        [uei.ExcludeFromDocs]
        public RenderTexture(int width, int height, GraphicsFormat colorFormat, GraphicsFormat depthStencilFormat, int mipCount)
        {
            // Note: the code duplication here is because you can't set a descriptor with
            // zero width/height, which our own code (and possibly existing user code) relies on.
            if (!ValidateFormat(colorFormat, FormatUsage.Render))
                return;

            Internal_Create(this);
            this.width = width; this.height = height; this.depthStencilFormat = depthStencilFormat;  this.graphicsFormat = colorFormat; SetMipMapCount(mipCount);

            SetSRGBReadWrite(GraphicsFormatUtility.IsSRGBFormat(colorFormat));
        }

        [uei.ExcludeFromDocs]
        public RenderTexture(int width, int height, GraphicsFormat colorFormat, GraphicsFormat depthStencilFormat)
            : this(width, height, colorFormat, depthStencilFormat, Texture.GenerateAllMips)
        {
        }

        public RenderTexture(int width, int height, int depth, [uei.DefaultValue("RenderTextureFormat.Default")] RenderTextureFormat format, [uei.DefaultValue("RenderTextureReadWrite.Default")] RenderTextureReadWrite readWrite)
        {
            Initialize(width, height, depth, format, readWrite, Texture.GenerateAllMips);
        }

        [uei.ExcludeFromDocs]
        public RenderTexture(int width, int height, int depth, RenderTextureFormat format)
            : this(width, height, depth, format, Texture.GenerateAllMips)
        {
        }

        [uei.ExcludeFromDocs]
        public RenderTexture(int width, int height, int depth)
            : this(width, height, depth, RenderTextureFormat.Default)
        {
        }

        [uei.ExcludeFromDocs]
        public RenderTexture(int width, int height, int depth, RenderTextureFormat format, int mipCount)
        {
            Initialize(width, height, depth, format, RenderTextureReadWrite.Default, mipCount);
        }

        private void Initialize(int width, int height, int depth, RenderTextureFormat format, RenderTextureReadWrite readWrite, int mipCount)
        {
            GraphicsFormat colorFormat = GetCompatibleFormat(format, readWrite);

            GraphicsFormat depthStencilFormat = GetDepthStencilFormatLegacy(depth, colorFormat);

            // Note: the code duplication here is because you can't set a descriptor with
            // zero width/height, which our own code (and possibly existing user code) relies on.
            if (!ValidateFormat(colorFormat, FormatUsage.Render))
                return;

            Internal_Create(this);
            this.width = width; this.height = height; this.depthStencilFormat = depthStencilFormat; this.graphicsFormat = colorFormat;

            SetMipMapCount(mipCount);
            SetSRGBReadWrite(GraphicsFormatUtility.IsSRGBFormat(colorFormat));
            if (format == RenderTextureFormat.Shadowmap)
            {
                SetShadowSamplingMode(Rendering.ShadowSamplingMode.CompareDepths);
            }
        }

        private static GraphicsFormat GetDepthStencilFormatLegacy(int depthBits, GraphicsFormat colorFormat)
        {
            return (colorFormat == GraphicsFormat.ShadowAuto) ?
                GraphicsFormatUtility.GetDepthStencilFormat(depthBits, 0)
                : GraphicsFormatUtility.GetDepthStencilFormat(depthBits);
        }

        public RenderTextureDescriptor descriptor
        {
            get { return GetDescriptor(); }
            set { ValidateRenderTextureDesc(value); SetRenderTextureDescriptor(value); }
        }


        private static void ValidateRenderTextureDesc(RenderTextureDescriptor desc)
        {
            if (desc.graphicsFormat == GraphicsFormat.None && desc.depthStencilFormat == GraphicsFormat.None)
                throw new ArgumentException("RenderTextureDesc graphicsFormat and depthStencilFormat cannot both be None.");
            if (desc.graphicsFormat != GraphicsFormat.None && !SystemInfo.IsFormatSupported(desc.graphicsFormat, FormatUsage.Render))
                throw new ArgumentException("RenderTextureDesc graphicsFormat must be a supported GraphicsFormat. " + desc.graphicsFormat + " is not supported on this platform.", "desc.graphicsFormat");
            if (desc.depthStencilFormat != GraphicsFormat.None &&
                !(GraphicsFormatUtility.IsDepthFormat(desc.depthStencilFormat) || GraphicsFormatUtility.IsStencilFormat(desc.depthStencilFormat)))
                throw new ArgumentException("RenderTextureDesc depthStencilFormat must be a supported depth/stencil GraphicsFormat. " + desc.depthStencilFormat + " is not supported on this platform.", "desc.depthStencilFormat");
            if (desc.width <= 0)
                throw new ArgumentException("RenderTextureDesc width must be greater than zero.", "desc.width");
            if (desc.height <= 0)
                throw new ArgumentException("RenderTextureDesc height must be greater than zero.", "desc.height");
            if (desc.volumeDepth <= 0)
                throw new ArgumentException("RenderTextureDesc volumeDepth must be greater than zero.", "desc.volumeDepth");
            if (desc.msaaSamples != 1 && desc.msaaSamples != 2 && desc.msaaSamples != 4 && desc.msaaSamples != 8)
                throw new ArgumentException("RenderTextureDesc msaaSamples must be 1, 2, 4, or 8.", "desc.msaaSamples");

            if (desc.graphicsFormat != GraphicsFormat.ShadowAuto && desc.graphicsFormat != GraphicsFormat.DepthAuto
                && (GraphicsFormatUtility.IsDepthFormat(desc.graphicsFormat) || GraphicsFormatUtility.IsStencilFormat(desc.graphicsFormat)))
                throw new ArgumentException("RenderTextureDesc graphicsFormat must not be a depth/stencil format. " + desc.graphicsFormat + " is not supported.", "desc.graphicsFormat");
        }
    }

    public partial class RenderTexture : Texture
    {
        internal static GraphicsFormat GetCompatibleFormat(RenderTextureFormat renderTextureFormat, RenderTextureReadWrite readWrite)
        {
            GraphicsFormat requestedFormat = GraphicsFormatUtility.GetGraphicsFormat(renderTextureFormat, readWrite);
            GraphicsFormat compatibleFormat = SystemInfo.GetCompatibleFormat(requestedFormat, FormatUsage.Render);

            if (requestedFormat == compatibleFormat)
            {
                return requestedFormat;
            }
            else
            {
                Debug.LogWarning(String.Format("'{0}' is not supported. RenderTexture::GetTemporary fallbacks to {1} format on this platform. Use 'SystemInfo.IsFormatSupported' C# API to check format support.", requestedFormat.ToString(), compatibleFormat.ToString()));
                return compatibleFormat;
            }
        }

        public static RenderTexture GetTemporary(RenderTextureDescriptor desc)
        {
            ValidateRenderTextureDesc(desc); desc.createdFromScript = true;
            return GetTemporary_Internal(desc);
        }

        // in old bindings "default args" were expanded into overloads and we must mimic that when migrating to new bindings
        // to keep things sane we will do internal methods WITH default args and do overloads that simply call it

        private static RenderTexture GetTemporaryImpl(int width, int height, int depthBuffer,
            GraphicsFormat colorFormat,
            int antiAliasing = 1, RenderTextureMemoryless memorylessMode = RenderTextureMemoryless.None,
            VRTextureUsage vrUsage = VRTextureUsage.None, bool useDynamicScale = false)
        {
            RenderTextureDescriptor desc = new RenderTextureDescriptor(width, height, colorFormat, GetDepthStencilFormatLegacy(depthBuffer, colorFormat));
            desc.msaaSamples = antiAliasing;
            desc.memoryless = memorylessMode;
            desc.vrUsage = vrUsage;
            desc.useDynamicScale = useDynamicScale;

            if (colorFormat == GraphicsFormat.ShadowAuto)
            {
                desc.shadowSamplingMode = Rendering.ShadowSamplingMode.CompareDepths;
            }

            return GetTemporary(desc);
        }

        // most detailed overload: use it to specify default values for docs
        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, GraphicsFormat format,
            [uei.DefaultValue("1")] int antiAliasing,
            [uei.DefaultValue("RenderTextureMemoryless.None")] RenderTextureMemoryless memorylessMode,
            [uei.DefaultValue("VRTextureUsage.None")] VRTextureUsage vrUsage,
            [uei.DefaultValue("false")] bool useDynamicScale)
        {
            return GetTemporaryImpl(width, height, depthBuffer, format, antiAliasing, memorylessMode, vrUsage, useDynamicScale);
        }

        // the rest will be excluded from docs (to "pretend" we have one method with default args)
        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, GraphicsFormat format, int antiAliasing, RenderTextureMemoryless memorylessMode, VRTextureUsage vrUsage)
        {
            return GetTemporaryImpl(width, height, depthBuffer, format, antiAliasing, memorylessMode, vrUsage);
        }

        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, GraphicsFormat format, int antiAliasing, RenderTextureMemoryless memorylessMode)
        {
            return GetTemporaryImpl(width, height, depthBuffer, format, antiAliasing, memorylessMode);
        }

        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, GraphicsFormat format, int antiAliasing)
        {
            return GetTemporaryImpl(width, height, depthBuffer, format, antiAliasing);
        }

        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, GraphicsFormat format)
        {
            return GetTemporaryImpl(width, height, depthBuffer, format);
        }

        // most detailed overload: use it to specify default values for docs
        public static RenderTexture GetTemporary(int width, int height,
            [uei.DefaultValue("0")] int depthBuffer, [uei.DefaultValue("RenderTextureFormat.Default")] RenderTextureFormat format,
            [uei.DefaultValue("RenderTextureReadWrite.Default")] RenderTextureReadWrite readWrite, [uei.DefaultValue("1")] int antiAliasing,
            [uei.DefaultValue("RenderTextureMemoryless.None")] RenderTextureMemoryless memorylessMode,
            [uei.DefaultValue("VRTextureUsage.None")] VRTextureUsage vrUsage, [uei.DefaultValue("false")] bool useDynamicScale
        )
        {
            return GetTemporaryImpl(width, height, depthBuffer, GetCompatibleFormat(format, readWrite), antiAliasing, memorylessMode, vrUsage, useDynamicScale);
        }

        // the rest will be excluded from docs (to "pretend" we have one method with default args)
        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing, RenderTextureMemoryless memorylessMode, VRTextureUsage vrUsage)
        {
            return GetTemporaryImpl(width, height, depthBuffer, GetCompatibleFormat(format, readWrite), antiAliasing, memorylessMode, vrUsage);
        }

        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing, RenderTextureMemoryless memorylessMode)
        {
            return GetTemporaryImpl(width, height, depthBuffer, GetCompatibleFormat(format, readWrite), antiAliasing, memorylessMode);
        }

        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing)
        {
            return GetTemporaryImpl(width, height, depthBuffer, GetCompatibleFormat(format, readWrite), antiAliasing);
        }

        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format, RenderTextureReadWrite readWrite)
        {
            return GetTemporaryImpl(width, height, depthBuffer, GetCompatibleFormat(format, readWrite));
        }

        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format)
        {
            return GetTemporaryImpl(width, height, depthBuffer, GetCompatibleFormat(format, RenderTextureReadWrite.Default));
        }

        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer)
        {
            return GetTemporaryImpl(width, height, depthBuffer, GetCompatibleFormat(RenderTextureFormat.Default, RenderTextureReadWrite.Default));
        }

        [uei.ExcludeFromDocs]
        public static RenderTexture GetTemporary(int width, int height)
        {
            return GetTemporaryImpl(width, height, 0, GetCompatibleFormat(RenderTextureFormat.Default, RenderTextureReadWrite.Default));
        }
    }

    public sealed partial class CustomRenderTexture : RenderTexture
    {
        // Be careful. We can't call base constructor here because it would create the native object twice.
        public CustomRenderTexture(int width, int height, RenderTextureFormat format, [uei.DefaultValue("RenderTextureReadWrite.Default")] RenderTextureReadWrite readWrite)
            : this(width, height, GetCompatibleFormat(format, readWrite))
        {
        }

        [uei.ExcludeFromDocs]
        public CustomRenderTexture(int width, int height, RenderTextureFormat format)
            : this(width, height, GetCompatibleFormat(format, RenderTextureReadWrite.Default))
        {
        }

        [uei.ExcludeFromDocs]
        public CustomRenderTexture(int width, int height)
            : this(width, height, SystemInfo.GetGraphicsFormat(DefaultFormat.LDR))
        {
        }

        [uei.ExcludeFromDocs]
        public CustomRenderTexture(int width, int height, [uei.DefaultValue("DefaultFormat.LDR")] DefaultFormat defaultFormat)
            : this(width, height, SystemInfo.GetGraphicsFormat(defaultFormat))
        {
        }

        [uei.ExcludeFromDocs]
        public CustomRenderTexture(int width, int height, GraphicsFormat format)
        {
            if (!ValidateFormat(format, FormatUsage.Render))
                return;

            Internal_CreateCustomRenderTexture(this);
            this.width = width;
            this.height = height;
            this.graphicsFormat = format;

            SetSRGBReadWrite(GraphicsFormatUtility.IsSRGBFormat(format));
        }
    }

    public partial class Texture : Object
    {
        public static readonly int GenerateAllMips = -1;

        internal bool ValidateFormat(RenderTextureFormat format)
        {
            if (SystemInfo.SupportsRenderTextureFormat(format))
            {
                return true;
            }
            else
            {
                Debug.LogError(String.Format("RenderTexture creation failed. '{0}' is not supported on this platform. Use 'SystemInfo.SupportsRenderTextureFormat' C# API to check format support.", format.ToString()), this);
                return false;
            }
        }

        internal bool ValidateFormat(TextureFormat format)
        {
            // If GPU support is detected, pass validation. Caveat support can also be used for uncompressed/non-IEEE754 formats.
            // In contrast to GraphicsFormat, TextureFormat can use fallbacks by design.
            if (SystemInfo.SupportsTextureFormat(format))
            {
                return true;
            }
            // If a compressed format is not supported natively, we check for decompressor support here.
            // If we are able to decompress, the data is decoded into a raw format. Otherwise, validation fails.
            else if (GraphicsFormatUtility.IsCompressedTextureFormat(format) && GraphicsFormatUtility.CanDecompressFormat(GraphicsFormatUtility.GetGraphicsFormat(format, false)))
            {
                return true;
            }
            else
            {
                Debug.LogError(String.Format("Texture creation failed. '{0}' is not supported on this platform. Use 'SystemInfo.SupportsTextureFormat' C# API to check format support.", format.ToString()), this);
                return false;
            }
        }

        internal bool ValidateFormat(GraphicsFormat format, FormatUsage usage)
        {
            //The auto formats are allowed to set as RenderTexture color format.
            if (usage != FormatUsage.Render && (format == GraphicsFormat.ShadowAuto || format == GraphicsFormat.DepthAuto))
            {
                Debug.LogWarning(String.Format("'{0}' is not allowed because it is an auto format and not an exact format. Use GraphicsFormatUtility.GetDepthStencilFormat to get an exact depth/stencil format.", format.ToString()), this);
                return false;
            }

            // *ONLY* GPU support is checked here. If it is not available, fail validation.
            // GraphicsFormat does not use fallbacks by design.
            if (SystemInfo.IsFormatSupported(format, usage))
            {
                return true;
            }
            else
            {
                Debug.LogError(String.Format("Texture creation failed. '{0}' is not supported for {1} usage on this platform. Use 'SystemInfo.IsFormatSupported' C# API to check format support.", format.ToString(), usage.ToString()), this);
                return false;
            }
        }

        internal UnityException CreateNonReadableException(Texture t)
        {
            return new UnityException(
                String.Format("Texture '{0}' is not readable, the texture memory can not be accessed from scripts. You can make the texture readable in the Texture Import Settings.", t.name)
            );
        }
    }


    public partial class Texture2D : Texture
    {
        internal bool ValidateFormat(TextureFormat format, int width, int height)
        {
            bool isValid = ValidateFormat(format);
            if (isValid)
            {
                bool requireSquarePOT = (TextureFormat.PVRTC_RGB2 <= format && format <= TextureFormat.PVRTC_RGBA4);
                if (requireSquarePOT && !(width == height && Mathf.IsPowerOfTwo(width)))
                    throw new UnityException(String.Format("'{0}' demands texture to be square and have power-of-two dimensions", format.ToString()));
            }
            return isValid;
        }

        internal bool ValidateFormat(GraphicsFormat format, int width, int height)
        {
            bool isValid = ValidateFormat(format, FormatUsage.Sample);
            if (isValid)
            {
                bool requireSquarePOT = GraphicsFormatUtility.IsPVRTCFormat(format);
                if (requireSquarePOT && !(width == height && Mathf.IsPowerOfTwo(width)))
                    throw new UnityException(String.Format("'{0}' demands texture to be square and have power-of-two dimensions", format.ToString()));
            }
            return isValid;
        }

        internal Texture2D(int width, int height, GraphicsFormat format, TextureCreationFlags flags, int mipCount, IntPtr nativeTex)
        {
            if (ValidateFormat(format, width, height))
                Internal_Create(this, width, height, mipCount, format, flags, nativeTex);
        }

        [uei.ExcludeFromDocs]
        public Texture2D(int width, int height, DefaultFormat format, TextureCreationFlags flags)
            : this(width, height, SystemInfo.GetGraphicsFormat(format), flags)
        {
        }

        [uei.ExcludeFromDocs]
        public Texture2D(int width, int height, GraphicsFormat format, TextureCreationFlags flags)
            : this(width, height, format, flags, Texture.GenerateAllMips, IntPtr.Zero)
        {
        }

        [uei.ExcludeFromDocs]
        public Texture2D(int width, int height, GraphicsFormat format, int mipCount, TextureCreationFlags flags)
            : this(width, height, format, flags, mipCount, IntPtr.Zero)
        {
        }

        internal Texture2D(int width, int height, TextureFormat textureFormat, int mipCount, bool linear, IntPtr nativeTex)
        {
            if (!ValidateFormat(textureFormat, width, height))
                return;

            GraphicsFormat format = GraphicsFormatUtility.GetGraphicsFormat(textureFormat, !linear);
            TextureCreationFlags flags = (mipCount != 1) ? TextureCreationFlags.MipChain : TextureCreationFlags.None;
            if (GraphicsFormatUtility.IsCrunchFormat(textureFormat))
                flags |= TextureCreationFlags.Crunch;
            Internal_Create(this, width, height, mipCount, format, flags, nativeTex);
        }

        public Texture2D(int width, int height, [uei.DefaultValue("TextureFormat.RGBA32")] TextureFormat textureFormat, [uei.DefaultValue("-1")] int mipCount, [uei.DefaultValue("false")] bool linear)
            : this(width, height, textureFormat, mipCount, linear, IntPtr.Zero)
        {
        }

        public Texture2D(int width, int height, [uei.DefaultValue("TextureFormat.RGBA32")] TextureFormat textureFormat, [uei.DefaultValue("true")] bool mipChain, [uei.DefaultValue("false")] bool linear)
            : this(width, height, textureFormat, mipChain ? -1 : 1, linear, IntPtr.Zero)
        {
        }

        [uei.ExcludeFromDocs]
        public Texture2D(int width, int height, TextureFormat textureFormat, bool mipChain)
            : this(width, height, textureFormat, mipChain ? -1 : 1, false, IntPtr.Zero)
        {
        }

        [uei.ExcludeFromDocs]
        public Texture2D(int width, int height)
            : this(width, height, TextureFormat.RGBA32, Texture.GenerateAllMips, false, IntPtr.Zero)
        {
        }

        public static Texture2D CreateExternalTexture(int width, int height, TextureFormat format, bool mipChain, bool linear, IntPtr nativeTex)
        {
            if (nativeTex == IntPtr.Zero)
                throw new ArgumentException("nativeTex can not be null");
            return new Texture2D(width, height, format, mipChain ? -1 : 1, linear, nativeTex);
        }

        [uei.ExcludeFromDocs]
        public void SetPixel(int x, int y, Color color)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            SetPixelImpl(0, 0, x, y, color);
        }

        public void SetPixel(int x, int y, Color color, [uei.DefaultValue("0")] int mipLevel)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            SetPixelImpl(0, mipLevel, x, y, color);
        }

        public void SetPixels(int x, int y, int blockWidth, int blockHeight, Color[] colors, [uei.DefaultValue("0")] int miplevel)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            SetPixelsImpl(x, y, blockWidth, blockHeight, colors, miplevel, 0);
        }

        [uei.ExcludeFromDocs]
        public void SetPixels(int x, int y, int blockWidth, int blockHeight, Color[] colors)
        {
            SetPixels(x, y, blockWidth, blockHeight, colors, 0);
        }

        public void SetPixels(Color[] colors, [uei.DefaultValue("0")] int miplevel)
        {
            int w = width >> miplevel; if (w < 1) w = 1;
            int h = height >> miplevel; if (h < 1) h = 1;
            SetPixels(0, 0, w, h, colors, miplevel);
        }

        [uei.ExcludeFromDocs]
        public void SetPixels(Color[] colors)
        {
            SetPixels(0, 0, width, height, colors, 0);
        }

        [uei.ExcludeFromDocs]
        public Color GetPixel(int x, int y)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return GetPixelImpl(0,  0, x, y);
        }

        public Color GetPixel(int x, int y, [uei.DefaultValue("0")] int mipLevel)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return GetPixelImpl(0, mipLevel, x, y);
        }

        [uei.ExcludeFromDocs]
        public Color GetPixelBilinear(float u, float v)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return GetPixelBilinearImpl(0, 0, u, v);
        }

        public Color GetPixelBilinear(float u, float v, [uei.DefaultValue("0")] int mipLevel)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return GetPixelBilinearImpl(0, mipLevel, u, v);
        }

        public void LoadRawTextureData(IntPtr data, int size)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (data == IntPtr.Zero || size == 0) { Debug.LogError("No texture data provided to LoadRawTextureData", this); return; }
            if (!LoadRawTextureDataImpl(data, size))
                throw new UnityException("LoadRawTextureData: not enough data provided (will result in overread).");
        }

        public void LoadRawTextureData(byte[] data)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (data == null || data.Length == 0) { Debug.LogError("No texture data provided to LoadRawTextureData", this); return; }
            if (!LoadRawTextureDataImplArray(data))
                throw new UnityException("LoadRawTextureData: not enough data provided (will result in overread).");
        }

        unsafe public void LoadRawTextureData<T>(NativeArray<T> data) where T : struct
        {
            if (!isReadable) throw CreateNonReadableException(this);
            if (!data.IsCreated || data.Length == 0) throw new UnityException("No texture data provided to LoadRawTextureData");
            if (!LoadRawTextureDataImpl((IntPtr)data.GetUnsafeReadOnlyPtr(), data.Length * UnsafeUtility.SizeOf<T>()))
                throw new UnityException("LoadRawTextureData: not enough data provided (will result in overread).");
        }

        public void SetPixelData<T>(T[] data, int mipLevel, [uei.DefaultValue("0")] int sourceDataStartIndex = 0)
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (data == null || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");
            SetPixelDataImplArray(data, mipLevel, System.Runtime.InteropServices.Marshal.SizeOf(data[0]), data.Length, sourceDataStartIndex);
        }

        unsafe public void SetPixelData<T>(NativeArray<T> data, int mipLevel, [uei.DefaultValue("0")] int sourceDataStartIndex = 0) where T : struct
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (!data.IsCreated || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");
            SetPixelDataImpl((IntPtr)data.GetUnsafeReadOnlyPtr(), mipLevel, UnsafeUtility.SizeOf<T>(), data.Length, sourceDataStartIndex);
        }

        public unsafe NativeArray<T> GetPixelData<T>(int mipLevel) where T : struct
        {
            if (!isReadable) throw CreateNonReadableException(this);

            int chainOffset = GetPixelDataOffset(mipLevel);
            int arraySize = GetPixelDataSize(mipLevel);
            int stride = UnsafeUtility.SizeOf<T>();

            IntPtr dataPtr = new IntPtr(GetWritableImageData(0).ToInt64() + chainOffset);

            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)dataPtr, (int)(arraySize / stride), Allocator.None);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, this.GetSafetyHandleForSlice(mipLevel));
            return array;
        }

        public unsafe NativeArray<T> GetRawTextureData<T>() where T : struct
        {
            if (!isReadable) throw CreateNonReadableException(this);

            int stride = UnsafeUtility.SizeOf<T>();
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)GetWritableImageData(0), (int)(GetRawImageDataSize() / stride), Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, Texture2D.GetSafetyHandle(this));
            return array;
        }

        public void Apply([uei.DefaultValue("true")] bool updateMipmaps, [uei.DefaultValue("false")] bool makeNoLongerReadable)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            ApplyImpl(updateMipmaps, makeNoLongerReadable);
        }

        [uei.ExcludeFromDocs] public void Apply(bool updateMipmaps) { Apply(updateMipmaps, false); }
        [uei.ExcludeFromDocs] public void Apply() { Apply(true, false); }

        public bool Reinitialize(int width, int height)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return ReinitializeImpl(width, height);
        }

        public bool Reinitialize(int width, int height, TextureFormat format, bool hasMipMap)
        {
            return ReinitializeWithFormatImpl(width, height, GraphicsFormatUtility.GetGraphicsFormat(format, activeTextureColorSpace == ColorSpace.Gamma), hasMipMap);
        }

        public bool Reinitialize(int width, int height, GraphicsFormat format, bool hasMipMap)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return ReinitializeWithFormatImpl(width, height, format, hasMipMap);
        }

        [Obsolete("Texture2D.Resize(int, int) has been deprecated because it actually reinitializes the texture. Use Texture2D.Reinitialize(int, int) instead (UnityUpgradable) -> Reinitialize(System.Int32, System.Int32)", false)]
        public bool Resize(int width, int height)
        {
            return Reinitialize(width, height);
        }

        [Obsolete("Texture2D.Resize(int, int, TextureFormat, bool) has been deprecated because it actually reinitializes the texture. Use Texture2D.Reinitialize(int, int, TextureFormat, bool) instead (UnityUpgradable) -> Reinitialize(System.Int32, System.Int32, UnityEngine.TextureFormat, System.Boolean)", false)]
        public bool Resize(int width, int height, TextureFormat format, bool hasMipMap)
        {
            return Reinitialize(width, height, format, hasMipMap);
        }

        [Obsolete("Texture2D.Resize(int, int, GraphicsFormat, bool) has been deprecated because it actually reinitializes the texture. Use Texture2D.Reinitialize(int, int, GraphicsFormat, bool) instead (UnityUpgradable) -> Reinitialize(System.Int32, System.Int32, UnityEngine.Experimental.Rendering.GraphicsFormat, System.Boolean)", false)]
        public bool Resize(int width, int height, GraphicsFormat format, bool hasMipMap)
        {
            return Reinitialize(width, height, format, hasMipMap);
        }

        public void ReadPixels(Rect source, int destX, int destY, [uei.DefaultValue("true")] bool recalculateMipMaps)
        {
            //if (ValidateFormat(GraphicsFormatUtility.GetGraphicsFormat(format, ), FormatUsage.ReadPixels))
            //    Debug.LogError("No texture data provided to LoadRawTextureData", this);
            if (!isReadable) throw CreateNonReadableException(this);
            ReadPixelsImpl(source, destX, destY, recalculateMipMaps);
        }

        [uei.ExcludeFromDocs] public void ReadPixels(Rect source, int destX, int destY) { ReadPixels(source, destX, destY, true); }

        public static bool GenerateAtlas(Vector2[] sizes, int padding, int atlasSize, List<Rect> results)
        {
            if (sizes == null)
                throw new ArgumentException("sizes array can not be null");
            if (results == null)
                throw new ArgumentException("results list cannot be null");
            if (padding < 0)
                throw new ArgumentException("padding can not be negative");
            if (atlasSize <= 0)
                throw new ArgumentException("atlas size must be positive");

            results.Clear();
            if (sizes.Length == 0)
                return true;

            NoAllocHelpers.EnsureListElemCount(results, sizes.Length);
            GenerateAtlasImpl(sizes, padding, atlasSize, NoAllocHelpers.ExtractArrayFromListT(results));
            return results.Count != 0;
        }

        public void SetPixels32(Color32[] colors, [uei.DefaultValue("0")] int miplevel)
        {
            SetAllPixels32(colors, miplevel);
        }

        [uei.ExcludeFromDocs]
        public void SetPixels32(Color32[] colors)
        {
            SetPixels32(colors, 0);
        }

        public void SetPixels32(int x, int y, int blockWidth, int blockHeight, Color32[] colors, [uei.DefaultValue("0")] int miplevel)
        {
            SetBlockOfPixels32(x, y, blockWidth, blockHeight, colors, miplevel);
        }

        [uei.ExcludeFromDocs]
        public void SetPixels32(int x, int y, int blockWidth, int blockHeight, Color32[] colors)
        {
            SetPixels32(x, y, blockWidth, blockHeight, colors, 0);
        }

        public Color[] GetPixels([uei.DefaultValue("0")] int miplevel)
        {
            int w = width >> miplevel; if (w < 1) w = 1;
            int h = height >> miplevel; if (h < 1) h = 1;
            return GetPixels(0, 0, w, h, miplevel);
        }

        [uei.ExcludeFromDocs]
        public Color[] GetPixels()
        {
            return GetPixels(0);
        }

        [Flags]
        public enum EXRFlags
        {
            None = 0,
            OutputAsFloat = 1 << 0, // Default is Half
            // Compression are mutually exclusive.
            CompressZIP = 1 << 1,
            CompressRLE = 1 << 2,
            CompressPIZ = 1 << 3,
        }
    }

    public sealed partial class Cubemap : Texture
    {
        internal bool ValidateFormat(TextureFormat format, int width)
        {
            bool isValid = ValidateFormat(format);
            if (isValid)
            {
                bool requireSquarePOT = (TextureFormat.PVRTC_RGB2 <= format && format <= TextureFormat.PVRTC_RGBA4);
                if (requireSquarePOT && !Mathf.IsPowerOfTwo(width))
                    throw new UnityException(String.Format("'{0}' demands texture to have power-of-two dimensions", format.ToString()));
            }
            return isValid;
        }

        internal bool ValidateFormat(GraphicsFormat format, int width)
        {
            bool isValid = ValidateFormat(format, FormatUsage.Sample);
            if (isValid)
            {
                bool requireSquarePOT = GraphicsFormatUtility.IsPVRTCFormat(format);
                if (requireSquarePOT && !Mathf.IsPowerOfTwo(width))
                    throw new UnityException(String.Format("'{0}' demands texture to have power-of-two dimensions", format.ToString()));
            }
            return isValid;
        }

        [uei.ExcludeFromDocs]
        public Cubemap(int width, DefaultFormat format, TextureCreationFlags flags)
            : this(width, SystemInfo.GetGraphicsFormat(format), flags)
        {
        }

        [uei.ExcludeFromDocs]
        [RequiredByNativeCode] // used to create builtin textures
        public Cubemap(int width, GraphicsFormat format, TextureCreationFlags flags)
        {
            if (ValidateFormat(format, width))
                Internal_Create(this, width, Texture.GenerateAllMips, format, flags, IntPtr.Zero);
        }

        public Cubemap(int width, TextureFormat format, int mipCount)
            : this(width, format, mipCount, IntPtr.Zero)
        {
        }

        [uei.ExcludeFromDocs]
        public Cubemap(int width, GraphicsFormat format, TextureCreationFlags flags, int mipCount)
        {
            if (!ValidateFormat(format, width))
                return;

            ValidateIsNotCrunched(flags); // Script created Crunched Cubemaps not supported

            Internal_Create(this, width, mipCount, format, flags, IntPtr.Zero);
        }

        internal Cubemap(int width, TextureFormat textureFormat, int mipCount, IntPtr nativeTex)
        {
            if (!ValidateFormat(textureFormat, width))
                return;

            GraphicsFormat format = GraphicsFormatUtility.GetGraphicsFormat(textureFormat, false);
            TextureCreationFlags flags = (mipCount != 1) ? TextureCreationFlags.MipChain : TextureCreationFlags.None;
            if (GraphicsFormatUtility.IsCrunchFormat(textureFormat))
                flags |= TextureCreationFlags.Crunch;
            ValidateIsNotCrunched(flags); // Script created Crunched Cubemaps not supported
            Internal_Create(this, width, mipCount, format, flags, nativeTex);
        }

        internal Cubemap(int width, TextureFormat textureFormat, bool mipChain, IntPtr nativeTex)
            : this(width, textureFormat, mipChain ? -1 : 1, nativeTex)
        {
        }

        public Cubemap(int width, TextureFormat textureFormat, bool mipChain)
            : this(width, textureFormat, mipChain ? -1 : 1, IntPtr.Zero)
        {
        }

        public static Cubemap CreateExternalTexture(int width, TextureFormat format, bool mipmap, IntPtr nativeTex)
        {
            if (nativeTex == IntPtr.Zero)
                throw new ArgumentException("nativeTex can not be null");
            return new Cubemap(width, format, mipmap, nativeTex);
        }

        public void SetPixelData<T>(T[] data, int mipLevel, CubemapFace face, [uei.DefaultValue("0")] int sourceDataStartIndex = 0)
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (data == null || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");
            SetPixelDataImplArray(data, mipLevel, (int)face, System.Runtime.InteropServices.Marshal.SizeOf(data[0]), data.Length, sourceDataStartIndex);
        }

        unsafe public void SetPixelData<T>(NativeArray<T> data, int mipLevel, CubemapFace face, [uei.DefaultValue("0")] int sourceDataStartIndex = 0) where T : struct
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (!data.IsCreated || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");

            SetPixelDataImpl((IntPtr)data.GetUnsafeReadOnlyPtr(), mipLevel, (int)face, UnsafeUtility.SizeOf<T>(), data.Length, sourceDataStartIndex);
        }

        public unsafe NativeArray<T> GetPixelData<T>(int mipLevel, CubemapFace face) where T : struct
        {
            if (!isReadable) throw CreateNonReadableException(this);

            int singleElementDataSize = GetPixelDataOffset(this.mipmapCount, (int)face);
            int chainOffset = GetPixelDataOffset(mipLevel, (int)face);
            int arraySize = GetPixelDataSize(mipLevel, (int)face);
            int stride = UnsafeUtility.SizeOf<T>();

            IntPtr dataPtr = new IntPtr(GetWritableImageData(0).ToInt64() + (singleElementDataSize * (int)face + chainOffset));
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)dataPtr, (int)(arraySize / stride), Allocator.None);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, this.GetSafetyHandleForSlice(mipLevel, (int)face));
            return array;
        }

        [uei.ExcludeFromDocs]
        public void SetPixel(CubemapFace face, int x, int y, Color color)
        {
            SetPixel(face, x, y, color, 0);
        }

        public void SetPixel(CubemapFace face, int x, int y, Color color, [uei.DefaultValue("0")] int mip)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            SetPixelImpl((int)face, mip, x, y, color);
        }

        [uei.ExcludeFromDocs]
        public Color GetPixel(CubemapFace face, int x, int y)
        {
            return GetPixel(face, x, y, 0);
        }

        public Color GetPixel(CubemapFace face, int x, int y, [uei.DefaultValue("0")] int mip)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return GetPixelImpl((int)face, mip, x, y);
        }

        public void Apply([uei.DefaultValue("true")] bool updateMipmaps, [uei.DefaultValue("false")] bool makeNoLongerReadable)
        {
            ApplyImpl(updateMipmaps, makeNoLongerReadable);
        }

        [uei.ExcludeFromDocs]  public void Apply(bool updateMipmaps) { Apply(updateMipmaps, false); }
        [uei.ExcludeFromDocs]  public void Apply() { Apply(true, false); }

        private static void ValidateIsNotCrunched(TextureCreationFlags flags)
        {
            if ((flags &= TextureCreationFlags.Crunch) != 0)
                throw new ArgumentException("Crunched Cubemap is not supported for textures created from script.");
        }
    }

    public sealed partial class Texture3D : Texture
    {
        [uei.ExcludeFromDocs]
        public Texture3D(int width, int height, int depth, DefaultFormat format, TextureCreationFlags flags)
            : this(width, height, depth, SystemInfo.GetGraphicsFormat(format), flags)
        {
        }

        [uei.ExcludeFromDocs]
        [RequiredByNativeCode] // used to create builtin textures
        public Texture3D(int width, int height, int depth, GraphicsFormat format, TextureCreationFlags flags)
            : this(width, height, depth, format, flags, Texture.GenerateAllMips)
        {
        }

        [uei.ExcludeFromDocs]
        public Texture3D(int width, int height, int depth, GraphicsFormat format, TextureCreationFlags flags, [uei.DefaultValue("-1")] int mipCount)
        {
            if (!ValidateFormat(format, FormatUsage.Sample))
                return;

            ValidateIsNotCrunched(flags);
            Internal_Create(this, width, height, depth, mipCount, format, flags, IntPtr.Zero);
        }

        [uei.ExcludeFromDocs]
        public Texture3D(int width, int height, int depth, TextureFormat textureFormat, int mipCount)
        {
            if (!ValidateFormat(textureFormat))
                return;

            GraphicsFormat format = GraphicsFormatUtility.GetGraphicsFormat(textureFormat, false);
            TextureCreationFlags flags = (mipCount != 1) ? TextureCreationFlags.MipChain : TextureCreationFlags.None;
            if (GraphicsFormatUtility.IsCrunchFormat(textureFormat))
                flags |= TextureCreationFlags.Crunch;
            ValidateIsNotCrunched(flags);
            Internal_Create(this, width, height, depth, mipCount, format, flags, IntPtr.Zero);
        }

        public Texture3D(int width, int height, int depth, TextureFormat textureFormat, int mipCount, [uei.DefaultValue("IntPtr.Zero")] IntPtr nativeTex)
        {
            if (!ValidateFormat(textureFormat))
                return;

            GraphicsFormat format = GraphicsFormatUtility.GetGraphicsFormat(textureFormat, false);
            TextureCreationFlags flags = (mipCount != 1) ? TextureCreationFlags.MipChain : TextureCreationFlags.None;
            if (GraphicsFormatUtility.IsCrunchFormat(textureFormat))
                flags |= TextureCreationFlags.Crunch;
            ValidateIsNotCrunched(flags);
            Internal_Create(this, width, height, depth, mipCount, format, flags, nativeTex);
        }

        [uei.ExcludeFromDocs]
        public Texture3D(int width, int height, int depth, TextureFormat textureFormat, bool mipChain)
            : this(width, height, depth, textureFormat, mipChain ? -1 : 1)
        {
        }

        public Texture3D(int width, int height, int depth, TextureFormat textureFormat, bool mipChain, [uei.DefaultValue("IntPtr.Zero")] IntPtr nativeTex)
            : this(width, height, depth, textureFormat, mipChain ? -1 : 1, nativeTex)
        {
        }

        public static Texture3D CreateExternalTexture(int width, int height, int depth, TextureFormat format, bool mipChain, IntPtr nativeTex)
        {
            if (nativeTex == IntPtr.Zero)
                throw new ArgumentException($"{nameof(nativeTex)} may not be zero");

            return new Texture3D(width, height, depth, format, mipChain ? -1 : 1, nativeTex);
        }

        public void Apply([uei.DefaultValue("true")] bool updateMipmaps, [uei.DefaultValue("false")] bool makeNoLongerReadable)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            ApplyImpl(updateMipmaps, makeNoLongerReadable);
        }

        [uei.ExcludeFromDocs]  public void Apply(bool updateMipmaps) { Apply(updateMipmaps, false); }
        [uei.ExcludeFromDocs]  public void Apply() { Apply(true, false); }

        [uei.ExcludeFromDocs]
        public void SetPixel(int x, int y, int z, Color color)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            SetPixelImpl(0, x, y, z, color);
        }

        public void SetPixel(int x, int y, int z, Color color, [uei.DefaultValue("0")] int mipLevel)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            SetPixelImpl(mipLevel, x, y, z, color);
        }

        [uei.ExcludeFromDocs]
        public Color GetPixel(int x, int y, int z)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return GetPixelImpl(0, x, y, z);
        }

        public Color GetPixel(int x, int y, int z, [uei.DefaultValue("0")] int mipLevel)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return GetPixelImpl(mipLevel, x, y, z);
        }

        [uei.ExcludeFromDocs]
        public Color GetPixelBilinear(float u, float v, float w)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return GetPixelBilinearImpl(0, u, v, w);
        }

        public Color GetPixelBilinear(float u, float v, float w, [uei.DefaultValue("0")] int mipLevel)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            return GetPixelBilinearImpl(mipLevel, u, v, w);
        }

        public void SetPixelData<T>(T[] data, int mipLevel, [uei.DefaultValue("0")] int sourceDataStartIndex = 0)
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (data == null || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");
            SetPixelDataImplArray(data, mipLevel, System.Runtime.InteropServices.Marshal.SizeOf(data[0]), data.Length, sourceDataStartIndex);
        }

        unsafe public void SetPixelData<T>(NativeArray<T> data, int mipLevel, [uei.DefaultValue("0")] int sourceDataStartIndex = 0) where T : struct
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (!data.IsCreated || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");

            SetPixelDataImpl((IntPtr)data.GetUnsafeReadOnlyPtr(), mipLevel, UnsafeUtility.SizeOf<T>(), data.Length, sourceDataStartIndex);
        }

        public unsafe NativeArray<T> GetPixelData<T>(int mipLevel) where T : struct
        {
            if (!isReadable) throw CreateNonReadableException(this);

            int chainOffset = GetPixelDataOffset(mipLevel);
            int arraySize = GetPixelDataSize(mipLevel);
            int stride = UnsafeUtility.SizeOf<T>();

            IntPtr dataPtr = new IntPtr(GetImageDataPointer().ToInt64() + chainOffset);
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)dataPtr, (int)(arraySize / stride), Allocator.None);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, this.GetSafetyHandleForSlice(mipLevel));
            return array;
        }

        private static void ValidateIsNotCrunched(TextureCreationFlags flags)
        {
            if ((flags &= TextureCreationFlags.Crunch) != 0)
                throw new ArgumentException("Crunched Texture3D is not supported.");
        }
    }

    public sealed partial class Texture2DArray : Texture
    {
        internal bool ValidateFormat(TextureFormat format, int width, int height)
        {
            bool isValid = ValidateFormat(format);
            if (isValid)
            {
                bool requireSquarePOT = (TextureFormat.PVRTC_RGB2 <= format && format <= TextureFormat.PVRTC_RGBA4);
                if (requireSquarePOT && !(width == height && Mathf.IsPowerOfTwo(width)))
                    throw new UnityException(String.Format("'{0}' demands texture to be square and have power-of-two dimensions", format.ToString()));
            }
            return isValid;
        }

        internal bool ValidateFormat(GraphicsFormat format, int width, int height)
        {
            bool isValid = ValidateFormat(format, FormatUsage.Sample);
            if (isValid)
            {
                bool requireSquarePOT = GraphicsFormatUtility.IsPVRTCFormat(format);
                if (requireSquarePOT && !(width == height && Mathf.IsPowerOfTwo(width)))
                    throw new UnityException(String.Format("'{0}' demands texture to be square and have power-of-two dimensions", format.ToString()));
            }
            return isValid;
        }

        [uei.ExcludeFromDocs]
        public Texture2DArray(int width, int height, int depth, DefaultFormat format, TextureCreationFlags flags)
            : this(width, height, depth, SystemInfo.GetGraphicsFormat(format), flags)
        {
        }

        [uei.ExcludeFromDocs]
        [RequiredByNativeCode] // used to create builtin textures
        public Texture2DArray(int width, int height, int depth, GraphicsFormat format, TextureCreationFlags flags)
            : this(width, height, depth, format, flags, Texture.GenerateAllMips)
        {
        }

        [uei.ExcludeFromDocs]
        public Texture2DArray(int width, int height, int depth, GraphicsFormat format, TextureCreationFlags flags, int mipCount)
        {
            if (!ValidateFormat(format, width, height))
                return;

            ValidateIsNotCrunched(flags);
            Internal_Create(this, width, height, depth, mipCount, format, flags);
        }

        public Texture2DArray(int width, int height, int depth, TextureFormat textureFormat, int mipCount, bool linear)
        {
            if (!ValidateFormat(textureFormat, width, height))
                return;

            GraphicsFormat format = GraphicsFormatUtility.GetGraphicsFormat(textureFormat, !linear);
            TextureCreationFlags flags = (mipCount != 1) ? TextureCreationFlags.MipChain : TextureCreationFlags.None;
            if (GraphicsFormatUtility.IsCrunchFormat(textureFormat))
                flags |= TextureCreationFlags.Crunch;
            ValidateIsNotCrunched(flags);
            Internal_Create(this, width, height, depth, mipCount, format, flags);
        }

        public Texture2DArray(int width, int height, int depth, TextureFormat textureFormat, bool mipChain, [uei.DefaultValue("false")] bool linear)
            : this(width, height, depth, textureFormat, mipChain ? -1 : 1, linear)
        {
        }

        [uei.ExcludeFromDocs]
        public Texture2DArray(int width, int height, int depth, TextureFormat textureFormat, bool mipChain)
            : this(width, height, depth, textureFormat, mipChain ? -1 : 1, false)
        {
        }

        public void Apply([uei.DefaultValue("true")] bool updateMipmaps, [uei.DefaultValue("false")] bool makeNoLongerReadable)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            ApplyImpl(updateMipmaps, makeNoLongerReadable);
        }

        [uei.ExcludeFromDocs] public void Apply(bool updateMipmaps) { Apply(updateMipmaps, false); }
        [uei.ExcludeFromDocs] public void Apply() { Apply(true, false); }

        public void SetPixelData<T>(T[] data, int mipLevel, int element, [uei.DefaultValue("0")] int sourceDataStartIndex = 0)
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (data == null || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");
            SetPixelDataImplArray(data, mipLevel, element, System.Runtime.InteropServices.Marshal.SizeOf(data[0]), data.Length, sourceDataStartIndex);
        }

        unsafe public void SetPixelData<T>(NativeArray<T> data, int mipLevel, int element, [uei.DefaultValue("0")] int sourceDataStartIndex = 0) where T : struct
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (!data.IsCreated || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");

            SetPixelDataImpl((IntPtr)data.GetUnsafeReadOnlyPtr(), mipLevel, element, UnsafeUtility.SizeOf<T>(), data.Length, sourceDataStartIndex);
        }

        public unsafe NativeArray<T> GetPixelData<T>(int mipLevel, int element) where T : struct
        {
            if (!isReadable) throw CreateNonReadableException(this);

            int singleElementDataSize = GetPixelDataOffset(this.mipmapCount, element);
            int chainOffset = GetPixelDataOffset(mipLevel, element);
            int arraySize = GetPixelDataSize(mipLevel, element);
            int stride = UnsafeUtility.SizeOf<T>();

            IntPtr dataPtr = new IntPtr(GetImageDataPointer().ToInt64() + (singleElementDataSize * element + chainOffset));
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)dataPtr, (int)(arraySize / stride), Allocator.None);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, this.GetSafetyHandleForSlice(mipLevel, element));
            return array;
        }

        private static void ValidateIsNotCrunched(TextureCreationFlags flags)
        {
            if ((flags &= TextureCreationFlags.Crunch) != 0)
                throw new ArgumentException("Crunched Texture2DArray is not supported.");
        }
    }

    public sealed partial class CubemapArray : Texture
    {
        [uei.ExcludeFromDocs]
        public CubemapArray(int width, int cubemapCount, DefaultFormat format, TextureCreationFlags flags)
            : this(width, cubemapCount, SystemInfo.GetGraphicsFormat(format), flags)
        {
        }

        [uei.ExcludeFromDocs]
        [RequiredByNativeCode]
        public CubemapArray(int width, int cubemapCount, GraphicsFormat format, TextureCreationFlags flags)
            : this(width, cubemapCount, format, flags, Texture.GenerateAllMips)
        {
        }

        [uei.ExcludeFromDocs]
        public CubemapArray(int width, int cubemapCount, GraphicsFormat format, TextureCreationFlags flags, [uei.DefaultValue("-1")] int mipCount)
        {
            if (!ValidateFormat(format, FormatUsage.Sample))
                return;

            ValidateIsNotCrunched(flags);
            Internal_Create(this, width, cubemapCount, mipCount, format, flags);
        }

        public CubemapArray(int width, int cubemapCount, TextureFormat textureFormat, int mipCount, bool linear)
        {
            if (!ValidateFormat(textureFormat))
                return;

            GraphicsFormat format = GraphicsFormatUtility.GetGraphicsFormat(textureFormat, !linear);
            TextureCreationFlags flags = (mipCount != 1) ? TextureCreationFlags.MipChain : TextureCreationFlags.None;
            if (GraphicsFormatUtility.IsCrunchFormat(textureFormat))
                flags |= TextureCreationFlags.Crunch;
            ValidateIsNotCrunched(flags);
            Internal_Create(this, width, cubemapCount, mipCount, format, flags);
        }

        public CubemapArray(int width, int cubemapCount, TextureFormat textureFormat, bool mipChain, [uei.DefaultValue("false")] bool linear)
            : this(width, cubemapCount, textureFormat, mipChain ? -1 : 1, linear)
        {
        }

        [uei.ExcludeFromDocs]
        public CubemapArray(int width, int cubemapCount, TextureFormat textureFormat, bool mipChain)
            : this(width, cubemapCount, textureFormat, mipChain ? -1 : 1, false)
        {
        }

        public void Apply([uei.DefaultValue("true")] bool updateMipmaps, [uei.DefaultValue("false")] bool makeNoLongerReadable)
        {
            if (!isReadable) throw CreateNonReadableException(this);
            ApplyImpl(updateMipmaps, makeNoLongerReadable);
        }

        [uei.ExcludeFromDocs] public void Apply(bool updateMipmaps) { Apply(updateMipmaps, false); }
        [uei.ExcludeFromDocs] public void Apply() { Apply(true, false); }

        public void SetPixelData<T>(T[] data, int mipLevel, CubemapFace face, int element, [uei.DefaultValue("0")] int sourceDataStartIndex = 0)
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (data == null || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");

            SetPixelDataImplArray(data, mipLevel, (int)face, element, System.Runtime.InteropServices.Marshal.SizeOf(data[0]), data.Length, sourceDataStartIndex);
        }

        unsafe public void SetPixelData<T>(NativeArray<T> data, int mipLevel, CubemapFace face, int element, [uei.DefaultValue("0")] int sourceDataStartIndex = 0) where T : struct
        {
            if (sourceDataStartIndex < 0) throw new UnityException("SetPixelData: sourceDataStartIndex cannot be less than 0.");

            if (!isReadable) throw CreateNonReadableException(this);
            if (!data.IsCreated || data.Length == 0) throw new UnityException("No texture data provided to SetPixelData.");

            SetPixelDataImpl((IntPtr)data.GetUnsafeReadOnlyPtr(), mipLevel, (int)face, element, UnsafeUtility.SizeOf<T>(), data.Length, sourceDataStartIndex);
        }

        public unsafe NativeArray<T> GetPixelData<T>(int mipLevel, CubemapFace face, int element) where T : struct
        {
            if (!isReadable) throw CreateNonReadableException(this);

            int elementOffset = element * 6 + (int)face;
            int singleElementDataSize = GetPixelDataOffset(this.mipmapCount, elementOffset);
            int chainOffset = GetPixelDataOffset(mipLevel, elementOffset);
            int arraySize = GetPixelDataSize(mipLevel, elementOffset);
            int stride = UnsafeUtility.SizeOf<T>();

            IntPtr dataPtr = new IntPtr(GetImageDataPointer().ToInt64() + (singleElementDataSize * elementOffset + chainOffset));
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)dataPtr, (int)(arraySize / stride), Allocator.None);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, this.GetSafetyHandleForSlice(mipLevel, (int)face, element));
            return array;
        }

        private static void ValidateIsNotCrunched(TextureCreationFlags flags)
        {
            if ((flags &= TextureCreationFlags.Crunch) != 0)
                throw new ArgumentException("Crunched TextureCubeArray is not supported.");
        }
    }

    public sealed partial class SparseTexture : Texture
    {
        internal bool ValidateSize(int width, int height, GraphicsFormat format)
        {
            if (GraphicsFormatUtility.GetBlockSize(format) * (width / GraphicsFormatUtility.GetBlockWidth(format)) * (height / GraphicsFormatUtility.GetBlockHeight(format)) < 65536)
            {
                Debug.LogError("SparseTexture creation failed. The minimum size in bytes of a SparseTexture is 64KB.", this);
                return false;
            }
            return true;
        }

        private static void ValidateIsNotCrunched(TextureFormat textureFormat)
        {
            if (GraphicsFormatUtility.IsCrunchFormat(textureFormat))
                throw new ArgumentException("Crunched SparseTexture is not supported.");
        }

        [uei.ExcludeFromDocs]
        public SparseTexture(int width, int height, DefaultFormat format, int mipCount)
            : this(width, height, SystemInfo.GetGraphicsFormat(format), mipCount)
        {
        }

        [uei.ExcludeFromDocs]
        public SparseTexture(int width, int height, GraphicsFormat format, int mipCount)
        {
            if (!ValidateFormat(format, FormatUsage.Sparse))
                return;

            if (!ValidateSize(width, height, format))
                return;

            Internal_Create(this, width, height, format, mipCount);
        }

        [uei.ExcludeFromDocs]
        public SparseTexture(int width, int height, TextureFormat textureFormat, int mipCount)
            : this(width, height, textureFormat, mipCount, false)
        {
        }

        public SparseTexture(int width, int height, TextureFormat textureFormat, int mipCount, [uei.DefaultValue("false")] bool linear)
        {
            if (!ValidateFormat(textureFormat))
                return;

            ValidateIsNotCrunched(textureFormat);

            GraphicsFormat format = GraphicsFormatUtility.GetGraphicsFormat(textureFormat, !linear);
            if (!ValidateSize(width, height, format))
                return;

            Internal_Create(this, width, height, format, mipCount);
        }
    }
}
