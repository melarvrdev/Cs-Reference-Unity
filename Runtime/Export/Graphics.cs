// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using uei = UnityEngine.Internal;

namespace UnityEngine
{
    internal sealed partial class NoAllocHelpers
    {
        public static T[] ExtractArrayFromListT<T>(List<T> list) { return (T[])ExtractArrayFromList(list); }

        public static void EnsureListElemCount<T>(List<T> list, int count)
        {
            list.Clear();

            // make sure capacity is enough (that's where alloc WILL happen if needed)
            if (list.Capacity < count)
                list.Capacity = count;

            ResizeList(list, count);
        }
    }
}


namespace UnityEngine
{
    [UsedByNativeCode]
    public struct Resolution
    {
        // Keep in sync with ScreenManager::Resolution
        private int m_Width;
        private int m_Height;
        private int m_RefreshRate;

        public int width        { get { return m_Width; } set { m_Width = value; } }
        public int height       { get { return m_Height; } set { m_Height = value; } }
        public int refreshRate  { get { return m_RefreshRate; } set { m_RefreshRate = value; } }

        public override string ToString()
        {
            return UnityString.Format("{0} x {1} @ {2}Hz", m_Width, m_Height, m_RefreshRate);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    public sealed partial class LightmapData
    {
        internal Texture2D m_Light;
        internal Texture2D m_Dir;
        internal Texture2D m_ShadowMask;

        [System.Obsolete("Use lightmapColor property (UnityUpgradable) -> lightmapColor", false)]
        public Texture2D lightmapLight { get { return m_Light; }        set { m_Light = value; } }

        public Texture2D lightmapColor { get { return m_Light; }        set { m_Light = value; } }
        public Texture2D lightmapDir   { get { return m_Dir; }          set { m_Dir = value; } }
        public Texture2D shadowMask    { get { return m_ShadowMask; }   set { m_ShadowMask = value; } }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RenderBuffer
    {
        internal int m_RenderTextureInstanceID;
        internal IntPtr m_BufferPtr;

        internal void SetLoadAction(Rendering.RenderBufferLoadAction action)    { RenderBufferHelper.SetLoadAction(out this, (int)action); }
        internal void SetStoreAction(Rendering.RenderBufferStoreAction action)  { RenderBufferHelper.SetStoreAction(out this, (int)action); }

        internal Rendering.RenderBufferLoadAction loadAction
        {
            get { return (Rendering.RenderBufferLoadAction)RenderBufferHelper.GetLoadAction(out this); }
            set { SetLoadAction(value); }
        }
        internal Rendering.RenderBufferStoreAction storeAction
        {
            get { return (Rendering.RenderBufferStoreAction)RenderBufferHelper.GetStoreAction(out this); }
            set { SetStoreAction(value); }
        }

        public IntPtr GetNativeRenderBufferPtr()    { return RenderBufferHelper.GetNativeRenderBufferPtr(m_BufferPtr); }
    }

    public struct RenderTargetSetup
    {
        public RenderBuffer[]   color;
        public RenderBuffer     depth;

        public int              mipLevel;
        public CubemapFace      cubemapFace;
        public int              depthSlice;

        public Rendering.RenderBufferLoadAction[]   colorLoad;
        public Rendering.RenderBufferStoreAction[]  colorStore;

        public Rendering.RenderBufferLoadAction     depthLoad;
        public Rendering.RenderBufferStoreAction    depthStore;


        public RenderTargetSetup(
            RenderBuffer[] color, RenderBuffer depth, int mip, CubemapFace face,
            Rendering.RenderBufferLoadAction[] colorLoad, Rendering.RenderBufferStoreAction[] colorStore,
            Rendering.RenderBufferLoadAction depthLoad, Rendering.RenderBufferStoreAction depthStore
            )
        {
            this.color          = color;
            this.depth          = depth;

            this.mipLevel       = mip;
            this.cubemapFace    = face;
            this.depthSlice     = 0;

            this.colorLoad      = colorLoad;
            this.colorStore     = colorStore;

            this.depthLoad      = depthLoad;
            this.depthStore     = depthStore;
        }

        internal static Rendering.RenderBufferLoadAction[] LoadActions(RenderBuffer[] buf)
        {
            // preserve old discard behaviour: render surface flags are applied only on first activation
            // this will be used only in ctor without load/store actions specified
            Rendering.RenderBufferLoadAction[] ret = new Rendering.RenderBufferLoadAction[buf.Length];
            for (int i = 0; i < buf.Length; ++i)
            {
                ret[i] = buf[i].loadAction;
                buf[i].loadAction = Rendering.RenderBufferLoadAction.Load;
            }
            return ret;
        }

        internal static Rendering.RenderBufferStoreAction[] StoreActions(RenderBuffer[] buf)
        {
            // preserve old discard behaviour: render surface flags are applied only on first activation
            // this will be used only in ctor without load/store actions specified
            Rendering.RenderBufferStoreAction[] ret = new Rendering.RenderBufferStoreAction[buf.Length];
            for (int i = 0; i < buf.Length; ++i)
            {
                ret[i] = buf[i].storeAction;
                buf[i].storeAction = Rendering.RenderBufferStoreAction.Store;
            }
            return ret;
        }

        // TODO: when we enable default arguments support these can be combined into one method
        public RenderTargetSetup(RenderBuffer color, RenderBuffer depth)
            : this(new RenderBuffer[] { color }, depth)
        {
        }
        public RenderTargetSetup(RenderBuffer color, RenderBuffer depth, int mipLevel)
            : this(new RenderBuffer[] { color }, depth, mipLevel)
        {
        }
        public RenderTargetSetup(RenderBuffer color, RenderBuffer depth, int mipLevel, CubemapFace face)
            : this(new RenderBuffer[] { color }, depth, mipLevel, face)
        {
        }
        public RenderTargetSetup(RenderBuffer color, RenderBuffer depth, int mipLevel, CubemapFace face, int depthSlice)
            : this(new RenderBuffer[] { color }, depth, mipLevel, face)
        {
            this.depthSlice = depthSlice;
        }

        // TODO: when we enable default arguments support these can be combined into one method
        public RenderTargetSetup(RenderBuffer[] color, RenderBuffer depth)
            : this(color, depth, 0, CubemapFace.Unknown)
        {
        }

        public RenderTargetSetup(RenderBuffer[] color, RenderBuffer depth, int mipLevel)
            : this(color, depth, mipLevel, CubemapFace.Unknown)
        {
        }

        public RenderTargetSetup(RenderBuffer[] color, RenderBuffer depth, int mip, CubemapFace face)
            : this(color, depth, mip, face, LoadActions(color), StoreActions(color), depth.loadAction, depth.storeAction)
        {
        }
    }
}


//
// Graphics.SetRenderTarget
//


namespace UnityEngine
{
    public partial class Graphics
    {
        internal static void CheckLoadActionValid(Rendering.RenderBufferLoadAction load, string bufferType)
        {
            if (load != Rendering.RenderBufferLoadAction.Load && load != Rendering.RenderBufferLoadAction.DontCare)
                throw new ArgumentException(UnityString.Format("Bad {0} LoadAction provided.", bufferType));
        }

        internal static void CheckStoreActionValid(Rendering.RenderBufferStoreAction store, string bufferType)
        {
            if (store != Rendering.RenderBufferStoreAction.Store && store != Rendering.RenderBufferStoreAction.DontCare)
                throw new ArgumentException(UnityString.Format("Bad {0} StoreAction provided.", bufferType));
        }

        internal static void SetRenderTargetImpl(RenderTargetSetup setup)
        {
            if (setup.color.Length == 0)
                throw new ArgumentException("Invalid color buffer count for SetRenderTarget");
            if (setup.color.Length != setup.colorLoad.Length)
                throw new ArgumentException("Color LoadAction and Buffer arrays have different sizes");
            if (setup.color.Length != setup.colorStore.Length)
                throw new ArgumentException("Color StoreAction and Buffer arrays have different sizes");

            foreach (var load in setup.colorLoad)
                CheckLoadActionValid(load, "Color");
            foreach (var store in setup.colorStore)
                CheckStoreActionValid(store, "Color");

            CheckLoadActionValid(setup.depthLoad, "Depth");
            CheckStoreActionValid(setup.depthStore, "Depth");

            if ((int)setup.cubemapFace < (int)CubemapFace.Unknown || (int)setup.cubemapFace > (int)CubemapFace.NegativeZ)
                throw new ArgumentException("Bad CubemapFace provided");

            Internal_SetMRTFullSetup(
                setup.color, out setup.depth, setup.mipLevel, setup.cubemapFace, setup.depthSlice,
                setup.colorLoad, setup.colorStore, setup.depthLoad, setup.depthStore
                );
        }

        internal static void SetRenderTargetImpl(RenderBuffer colorBuffer, RenderBuffer depthBuffer, int mipLevel, CubemapFace face, int depthSlice)
        {
            RenderBuffer color = colorBuffer, depth = depthBuffer;
            Internal_SetRTSimple(out color, out depth, mipLevel, face, depthSlice);
        }

        internal static void SetRenderTargetImpl(RenderTexture rt, int mipLevel, CubemapFace face, int depthSlice)
        {
            if (rt)  SetRenderTargetImpl(rt.colorBuffer, rt.depthBuffer, mipLevel, face, depthSlice);
            else    Internal_SetNullRT();
        }

        internal static void SetRenderTargetImpl(RenderBuffer[] colorBuffers, RenderBuffer depthBuffer, int mipLevel, CubemapFace face, int depthSlice)
        {
            RenderBuffer depth = depthBuffer;
            Internal_SetMRTSimple(colorBuffers, out depth, mipLevel, face, depthSlice);
        }
    }

    public partial class Graphics
    {
        // TODO: when we enable default arguments support these can be combined into one method
        public static void SetRenderTarget(RenderTexture rt)
        {
            SetRenderTargetImpl(rt, 0, CubemapFace.Unknown, 0);
        }

        public static void SetRenderTarget(RenderTexture rt, int mipLevel)
        {
            SetRenderTargetImpl(rt, mipLevel, CubemapFace.Unknown, 0);
        }

        public static void SetRenderTarget(RenderTexture rt, int mipLevel, CubemapFace face)
        {
            SetRenderTargetImpl(rt, mipLevel, face, 0);
        }

        public static void SetRenderTarget(RenderTexture rt, int mipLevel, CubemapFace face, int depthSlice)
        {
            SetRenderTargetImpl(rt, mipLevel, face, depthSlice);
        }

        // TODO: when we enable default arguments support these can be combined into one method
        public static void SetRenderTarget(RenderBuffer colorBuffer, RenderBuffer depthBuffer)
        {
            SetRenderTargetImpl(colorBuffer, depthBuffer, 0, CubemapFace.Unknown, 0);
        }

        public static void SetRenderTarget(RenderBuffer colorBuffer, RenderBuffer depthBuffer, int mipLevel)
        {
            SetRenderTargetImpl(colorBuffer, depthBuffer, mipLevel, CubemapFace.Unknown, 0);
        }

        public static void SetRenderTarget(RenderBuffer colorBuffer, RenderBuffer depthBuffer, int mipLevel, CubemapFace face)
        {
            SetRenderTargetImpl(colorBuffer, depthBuffer, mipLevel, face, 0);
        }

        public static void SetRenderTarget(RenderBuffer colorBuffer, RenderBuffer depthBuffer, int mipLevel, CubemapFace face, int depthSlice)
        {
            SetRenderTargetImpl(colorBuffer, depthBuffer, mipLevel, face, depthSlice);
        }

        public static void SetRenderTarget(RenderBuffer[] colorBuffers, RenderBuffer depthBuffer)
        {
            SetRenderTargetImpl(colorBuffers, depthBuffer, 0, CubemapFace.Unknown, 0);
        }

        public static void SetRenderTarget(RenderTargetSetup setup)
        {
            SetRenderTargetImpl(setup);
        }

        // TODO: when we enable default arguments support these can be combined into one method
        public static void CopyTexture(Texture src, Texture dst)
        {
            CopyTexture_Full(src, dst);
        }

        public static void CopyTexture(Texture src, int srcElement, Texture dst, int dstElement)
        {
            CopyTexture_Slice_AllMips(src, srcElement, dst, dstElement);
        }

        public static void CopyTexture(Texture src, int srcElement, int srcMip, Texture dst, int dstElement, int dstMip)
        {
            CopyTexture_Slice(src, srcElement, srcMip, dst, dstElement, dstMip);
        }

        public static void CopyTexture(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, Texture dst, int dstElement, int dstMip, int dstX, int dstY)
        {
            CopyTexture_Region(src, srcElement, srcMip, srcX, srcY, srcWidth, srcHeight, dst, dstElement, dstMip, dstX, dstY);
        }

        // TODO: when we enable default arguments support these can be combined into one method
        public static bool ConvertTexture(Texture src, Texture dst)
        {
            return ConvertTexture_Full(src, dst);
        }

        public static bool ConvertTexture(Texture src, int srcElement, Texture dst, int dstElement)
        {
            return ConvertTexture_Slice(src, srcElement, dst, dstElement);
        }
    }
}


//
// Graphics.Draw*
//


namespace UnityEngine
{
    internal struct Internal_DrawMeshMatrixArguments
    {
        public int layer, submeshIndex;
        public Matrix4x4 matrix;
        public int castShadows, receiveShadows;
        public int reflectionProbeAnchorInstanceID;
        public bool useLightProbes;
    }

    [VisibleToOtherModules("UnityEngine.IMGUIModule")]
    internal struct Internal_DrawTextureArguments
    {
        public Rect screenRect, sourceRect;
        public int leftBorder, rightBorder, topBorder, bottomBorder;
        public Color32 color;
        public Vector4 borderWidths;
        public Vector4 cornerRadiuses;
        public int pass;
        public Texture texture;
        public Material mat;
    }


    public partial class Graphics
    {
        // NB: currently our c# toolchain do not accept default arguments (bindins generator will create actual functions that pass default values)
        // when we start to accept default params we can move the rest of DrawMesh out of bindings to c#
        private static void DrawMeshImpl(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, Rendering.ShadowCastingMode castShadows, bool receiveShadows, Transform probeAnchor, bool useLightProbes)
        {
            Internal_DrawMeshMatrixArguments args = new Internal_DrawMeshMatrixArguments();
            args.layer = layer;
            args.submeshIndex = submeshIndex;
            args.matrix = matrix;
            args.castShadows = (int)castShadows;
            args.receiveShadows = receiveShadows ? 1 : 0;
            args.reflectionProbeAnchorInstanceID = probeAnchor != null ? probeAnchor.GetInstanceID() : 0;
            args.useLightProbes = useLightProbes;

            Internal_DrawMeshMatrix(ref args, properties, material, mesh, camera);
        }

        // NB: currently our c# toolchain do not accept default arguments (bindins generator will create actual functions that pass default values)
        // when we start to accept default params we can move the rest of DrawMesh out of bindings to c#
        private static void DrawTextureImpl(Rect screenRect, Texture texture, Rect sourceRect, int leftBorder, int rightBorder, int topBorder, int bottomBorder, Color color, Material mat, int pass)
        {
            Internal_DrawTextureArguments args = new Internal_DrawTextureArguments();
            args.screenRect = screenRect; args.sourceRect = sourceRect;
            args.leftBorder = leftBorder; args.rightBorder = rightBorder; args.topBorder = topBorder; args.bottomBorder = bottomBorder;
            args.color = color;
            args.pass = pass;
            args.texture = texture;
            args.mat = mat;

            Internal_DrawTexture(ref args);
        }

        public static void DrawMeshNow(Mesh mesh, Vector3 position, Quaternion rotation)
        {
            DrawMeshNow(mesh, position, rotation, -1);
        }

        public static void DrawMeshNow(Mesh mesh, Vector3 position, Quaternion rotation, int materialIndex)
        {
            if (mesh == null)
                throw new ArgumentNullException("mesh");
            Internal_DrawMeshNow1(mesh, materialIndex, position, rotation);
        }

        public static void DrawMeshNow(Mesh mesh, Matrix4x4 matrix)
        {
            DrawMeshNow(mesh, matrix, -1);
        }

        public static void DrawMeshNow(Mesh mesh, Matrix4x4 matrix, int materialIndex)
        {
            if (mesh == null)
                throw new ArgumentNullException("mesh");
            Internal_DrawMeshNow2(mesh, materialIndex, matrix);
        }

        // NB: currently our c# toolchain do not accept default arguments (bindins generator will create actual functions that pass default values)
        // when we start to accept default params we can move the rest of DrawMesh out of bindings to c#
        private static void DrawMeshInstancedImpl(Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, int count, MaterialPropertyBlock properties, Rendering.ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera)
        {
            if (!SystemInfo.supportsInstancing)
                throw new InvalidOperationException("Instancing is not supported.");
            if (mesh == null)
                throw new ArgumentNullException("mesh");
            if (submeshIndex < 0 || submeshIndex >= mesh.subMeshCount)
                throw new ArgumentOutOfRangeException("submeshIndex", "submeshIndex out of range.");
            if (material == null)
                throw new ArgumentNullException("material");
            if (!material.enableInstancing)
                throw new InvalidOperationException("Material needs to enable instancing for use with DrawMeshInstanced.");
            if (matrices == null)
                throw new ArgumentNullException("matrices");
            if (count < 0 || count > Mathf.Min(kMaxDrawMeshInstanceCount, matrices.Length))
                throw new ArgumentOutOfRangeException("count", String.Format("Count must be in the range of 0 to {0}.", Mathf.Min(kMaxDrawMeshInstanceCount, matrices.Length)));

            if (count > 0)
                Internal_DrawMeshInstanced(mesh, submeshIndex, material, matrices, count, properties, castShadows, receiveShadows, layer, camera);
        }

        private static void DrawMeshInstancedImpl(Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices, MaterialPropertyBlock properties, Rendering.ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera)
        {
            if (matrices == null)
                throw new ArgumentNullException("matrices");
            if (matrices.Count > kMaxDrawMeshInstanceCount)
                throw new ArgumentOutOfRangeException("matrices", String.Format("Matrix list count must be in the range of 0 to {0}.", kMaxDrawMeshInstanceCount));

            DrawMeshInstancedImpl(mesh, submeshIndex, material, NoAllocHelpers.ExtractArrayFromListT(matrices), matrices.Count, properties, castShadows, receiveShadows, layer, camera);
        }

        // NB: currently our c# toolchain do not accept default arguments (bindins generator will create actual functions that pass default values)
        // when we start to accept default params we can move the rest of DrawMesh out of bindings to c#
        private static void DrawMeshInstancedIndirectImpl(Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties, Rendering.ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera)
        {
            if (!SystemInfo.supportsInstancing)
                throw new InvalidOperationException("Instancing is not supported.");
            if (mesh == null)
                throw new ArgumentNullException("mesh");
            if (submeshIndex < 0 || submeshIndex >= mesh.subMeshCount)
                throw new ArgumentOutOfRangeException("submeshIndex", "submeshIndex out of range.");
            if (material == null)
                throw new ArgumentNullException("material");
            if (bufferWithArgs == null)
                throw new ArgumentNullException("bufferWithArgs");

            Internal_DrawMeshInstancedIndirect(mesh, submeshIndex, material, bounds, bufferWithArgs, argsOffset, properties, castShadows, receiveShadows, layer, camera);
        }
    }
}


//
// Graphics.Blit*
//


namespace UnityEngine
{
    public partial class Graphics
    {
        public static void Blit(Texture source, RenderTexture dest)
        {
            Blit2(source, dest);
        }

        public static void Blit(Texture source, RenderTexture dest, Vector2 scale, Vector2 offset)
        {
            Blit4(source, dest, scale, offset);
        }

        public static void Blit(Texture source, RenderTexture dest, Material mat, [uei.DefaultValue("-1")] int pass)
        {
            Internal_BlitMaterial(source, dest, mat, pass, true);
        }

        public static void Blit(Texture source, RenderTexture dest, Material mat)
        {
            Blit(source, dest, mat, -1);
        }

        public static void Blit(Texture source, Material mat, [uei.DefaultValue("-1")] int pass)
        {
            Internal_BlitMaterial(source, null, mat, pass, false);
        }

        public static void Blit(Texture source, Material mat)
        {
            Blit(source, mat, -1);
        }

        public static void BlitMultiTap(Texture source, RenderTexture dest, Material mat, params Vector2[] offsets)
        {
            Internal_BlitMultiTap(source, dest, mat, offsets);
        }
    }
}


//
// MaterialPropertyBlock
//


namespace UnityEngine
{
    public sealed partial class MaterialPropertyBlock
    {
        internal void SetValue<T>(int name, T value)    { SetValueImpl(name, value, typeof(T)); }
        internal void SetValue<T>(string name, T value) { SetValueImpl(Shader.PropertyToID(name), value, typeof(T)); }

        internal T    GetValue<T>(int name)     { return (T)GetValueImpl(name, typeof(T)); }
        internal T    GetValue<T>(string name)  { return (T)GetValueImpl(Shader.PropertyToID(name), typeof(T)); }

        internal void SetValueArray<T>(int name, List<T> values)    { SetValueArrayImpl(name, NoAllocHelpers.ExtractArrayFromListT(values), values.Count, typeof(T)); }
        internal void SetValueArray<T>(string name, List<T> values) { SetValueArray<T>(Shader.PropertyToID(name), values); }
        internal void SetValueArray<T>(int name, T[] values)        { SetValueArrayImpl(name, values, values.Length, typeof(T)); }
        internal void SetValueArray<T>(string name, T[] values)     { SetValueArray<T>(Shader.PropertyToID(name), values); }

        internal int  GetValueArrayCount<T>(int name)   { return GetValueArrayCountImpl(name, typeof(T)); }

        // currently we do not support returning null from native if return type is array
        internal T[] GetValueArray<T>(int name)     { return GetValueArrayCount<T>(name) != 0 ? (T[])GetValueArrayImpl(name, typeof(T)) : null; }
        internal T[] GetValueArray<T>(string name)  { return GetValueArray<T>(Shader.PropertyToID(name)); }

        internal void ExtractValueArray<T>(int name, List<T> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            values.Clear();

            int count = GetValueArrayCount<T>(name);
            if (count > 0)
            {
                NoAllocHelpers.EnsureListElemCount(values, count);
                ExtractValueArrayImpl(name, NoAllocHelpers.ExtractArrayFromList(values), typeof(T));
            }
        }

        internal void ExtractValueArray<T>(string name, List<T> values) { ExtractValueArray<T>(Shader.PropertyToID(name), values); }
    }

    public sealed partial class MaterialPropertyBlock
    {
        internal IntPtr m_Ptr;

        public MaterialPropertyBlock()    { m_Ptr = CreateImpl(); }
        ~MaterialPropertyBlock()          { Dispose(); }

        // should we make it IDisposable?
        private void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                DestroyImpl(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        public void SetFloat(string name, float value)          { SetValue(name, value); }
        public void SetFloat(int name, float value)             { SetValue(name, value); }
        public void SetVector(string name, Vector4 value)       { SetValue(name, value); }
        public void SetVector(int name, Vector4 value)          { SetValue(name, value); }
        public void SetColor(string name, Color value)          { SetValue(name, value); }
        public void SetColor(int name, Color value)             { SetValue(name, value); }
        public void SetMatrix(string name, Matrix4x4 value)     { SetValue(name, value); }
        public void SetMatrix(int name, Matrix4x4 value)        { SetValue(name, value); }
        public void SetBuffer(string name, ComputeBuffer value) { SetValue(name, value); }
        public void SetBuffer(int name, ComputeBuffer value)    { SetValue(name, value); }
        public void SetTexture(string name, Texture value)      { SetValue(name, value); }
        public void SetTexture(int name, Texture value)         { SetValue(name, value); }

        public void SetFloatArray(string name, List<float> values)  { SetValueArray(name, values); }
        public void SetFloatArray(int name,    List<float> values)  { SetValueArray(name, values); }
        public void SetFloatArray(string name, float[] values)      { SetValueArray(name, values); }
        public void SetFloatArray(int name,    float[] values)      { SetValueArray(name, values); }

        public void SetVectorArray(string name, List<Vector4> values)   { SetValueArray(name, values); }
        public void SetVectorArray(int name,    List<Vector4> values)   { SetValueArray(name, values); }
        public void SetVectorArray(string name, Vector4[] values)       { SetValueArray(name, values); }
        public void SetVectorArray(int name,    Vector4[] values)       { SetValueArray(name, values); }

        public void SetMatrixArray(string name, List<Matrix4x4> values) { SetValueArray(name, values); }
        public void SetMatrixArray(int name,    List<Matrix4x4> values) { SetValueArray(name, values); }
        public void SetMatrixArray(string name, Matrix4x4[] values)     { SetValueArray(name, values); }
        public void SetMatrixArray(int name,    Matrix4x4[] values)     { SetValueArray(name, values); }

        public float     GetFloat(string name)      { return GetValue<float>(name); }
        public float     GetFloat(int name)         { return GetValue<float>(name); }
        public Vector4   GetVector(string name)     { return GetValue<Vector4>(name); }
        public Vector4   GetVector(int name)        { return GetValue<Vector4>(name); }
        public Color     GetColor(string name)      { return GetValue<Color>(name); }
        public Color     GetColor(int name)         { return GetValue<Color>(name); }
        public Matrix4x4 GetMatrix(string name)     { return GetValue<Matrix4x4>(name); }
        public Matrix4x4 GetMatrix(int name)        { return GetValue<Matrix4x4>(name); }
        public Texture   GetTexture(string name)    { return GetValue<Texture>(name); }
        public Texture   GetTexture(int name)       { return GetValue<Texture>(name); }

        public float[]      GetFloatArray(string name)  { return GetValueArray<float>(name); }
        public float[]      GetFloatArray(int name)     { return GetValueArray<float>(name); }
        public Vector4[]    GetVectorArray(string name) { return GetValueArray<Vector4>(name); }
        public Vector4[]    GetVectorArray(int name)    { return GetValueArray<Vector4>(name); }
        public Matrix4x4[]  GetMatrixArray(string name) { return GetValueArray<Matrix4x4>(name); }
        public Matrix4x4[]  GetMatrixArray(int name)    { return GetValueArray<Matrix4x4>(name); }

        public void GetFloatArray(string name, List<float> values)      { ExtractValueArray<float>(name, values); }
        public void GetFloatArray(int name, List<float> values)         { ExtractValueArray<float>(name, values); }
        public void GetVectorArray(string name, List<Vector4> values)   { ExtractValueArray<Vector4>(name, values); }
        public void GetVectorArray(int name, List<Vector4> values)      { ExtractValueArray<Vector4>(name, values); }
        public void GetMatrixArray(string name, List<Matrix4x4> values) { ExtractValueArray<Matrix4x4>(name, values); }
        public void GetMatrixArray(int name, List<Matrix4x4> values)    { ExtractValueArray<Matrix4x4>(name, values); }
    }
}


//
// Shader
//


namespace UnityEngine
{
    // internal generic methods to have one entry point for using native-scripting glue
    public sealed partial class Shader
    {
        internal static void SetGlobalValue<T>(int name, T value)       { SetGlobalValueImpl(name, value, typeof(T)); }
        internal static void SetGlobalValue<T>(string name, T value)    { SetGlobalValueImpl(Shader.PropertyToID(name), value, typeof(T)); }

        internal static T    GetGlobalValue<T>(int name)    { return (T)GetGlobalValueImpl(name, typeof(T)); }
        internal static T    GetGlobalValue<T>(string name) { return (T)GetGlobalValueImpl(Shader.PropertyToID(name), typeof(T)); }

        internal static void SetGlobalValueArray<T>(int name, List<T> values)    { SetGlobalValueArrayImpl(name, NoAllocHelpers.ExtractArrayFromListT(values), values.Count, typeof(T)); }
        internal static void SetGlobalValueArray<T>(string name, List<T> values) { SetGlobalValueArray<T>(Shader.PropertyToID(name), values); }
        internal static void SetGlobalValueArray<T>(int name, T[] values)        { SetGlobalValueArrayImpl(name, values, values.Length, typeof(T)); }
        internal static void SetGlobalValueArray<T>(string name, T[] values)     { SetGlobalValueArray<T>(Shader.PropertyToID(name), values); }

        internal static int  GetGlobalValueArrayCount<T>(int name) { return GetGlobalValueArrayCountImpl(name, typeof(T)); }

        // currently we do not support returning null from native if return type is array
        internal static T[] GetGlobalValueArray<T>(int name)    { return GetGlobalValueArrayCount<T>(name) != 0 ? (T[])GetGlobalValueArrayImpl(name, typeof(T)) : null; }
        internal static T[] GetGlobalValueArray<T>(string name) { return GetGlobalValueArray<T>(Shader.PropertyToID(name)); }

        internal static void ExtractGlobalValueArray<T>(int name, List<T> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            values.Clear();

            int count = GetGlobalValueArrayCount<T>(name);
            if (count > 0)
            {
                NoAllocHelpers.EnsureListElemCount(values, count);
                ExtractGlobalValueArrayImpl(name, NoAllocHelpers.ExtractArrayFromList(values), typeof(T));
            }
        }

        internal static void ExtractGlobalValueArray<T>(string name, List<T> values) { ExtractGlobalValueArray<T>(Shader.PropertyToID(name), values); }
    }

    public sealed partial class Shader
    {
        public static void SetGlobalFloat(string name, float value)             { SetGlobalValue(name, value); }
        public static void SetGlobalFloat(int name, float value)                { SetGlobalValue(name, value); }
        public static void SetGlobalInt(string name, int value)                 { SetGlobalValue(name, value); }
        public static void SetGlobalInt(int name, int value)                    { SetGlobalValue(name, value); }
        public static void SetGlobalVector(string name, Vector4 value)          { SetGlobalValue(name, value); }
        public static void SetGlobalVector(int name, Vector4 value)             { SetGlobalValue(name, value); }
        public static void SetGlobalColor(string name, Color value)             { SetGlobalValue(name, value); }
        public static void SetGlobalColor(int name, Color value)                { SetGlobalValue(name, value); }
        public static void SetGlobalMatrix(string name, Matrix4x4 value)        { SetGlobalValue(name, value); }
        public static void SetGlobalMatrix(int name, Matrix4x4 value)           { SetGlobalValue(name, value); }
        public static void SetGlobalTexture(string name, Texture value)         { SetGlobalValue(name, value); }
        public static void SetGlobalTexture(int name, Texture value)            { SetGlobalValue(name, value); }
        public static void SetGlobalBuffer(string name, ComputeBuffer value)    { SetGlobalValue(name, value); }
        public static void SetGlobalBuffer(int name, ComputeBuffer value)       { SetGlobalValue(name, value); }

        public static void SetGlobalFloatArray(string name, List<float> values) { SetGlobalValueArray(name, values); }
        public static void SetGlobalFloatArray(int name, List<float> values)    { SetGlobalValueArray(name, values); }
        public static void SetGlobalFloatArray(string name, float[] values)     { SetGlobalValueArray(name, values); }
        public static void SetGlobalFloatArray(int name, float[] values)        { SetGlobalValueArray(name, values); }

        public static void SetGlobalVectorArray(string name, List<Vector4> values)  { SetGlobalValueArray(name, values); }
        public static void SetGlobalVectorArray(int name, List<Vector4> values)     { SetGlobalValueArray(name, values); }
        public static void SetGlobalVectorArray(string name, Vector4[] values)      { SetGlobalValueArray(name, values); }
        public static void SetGlobalVectorArray(int name, Vector4[] values)         { SetGlobalValueArray(name, values); }

        public static void SetGlobalMatrixArray(string name, List<Matrix4x4> values) { SetGlobalValueArray(name, values); }
        public static void SetGlobalMatrixArray(int name, List<Matrix4x4> values)   { SetGlobalValueArray(name, values); }
        public static void SetGlobalMatrixArray(string name, Matrix4x4[] values)    { SetGlobalValueArray(name, values); }
        public static void SetGlobalMatrixArray(int name, Matrix4x4[] values)       { SetGlobalValueArray(name, values); }

        public static float       GetGlobalFloat(string name)       { return GetGlobalValue<float>(name); }
        public static float       GetGlobalFloat(int name)          { return GetGlobalValue<float>(name); }
        public static int         GetGlobalInt(string name)         { return GetGlobalValue<int>(name); }
        public static int         GetGlobalInt(int name)            { return GetGlobalValue<int>(name); }
        public static Vector4     GetGlobalVector(string name)      { return GetGlobalValue<Vector4>(name); }
        public static Vector4     GetGlobalVector(int name)         { return GetGlobalValue<Vector4>(name); }
        public static Color       GetGlobalColor(string name)       { return GetGlobalValue<Color>(name); }
        public static Color       GetGlobalColor(int name)          { return GetGlobalValue<Color>(name); }
        public static Matrix4x4   GetGlobalMatrix(string name)      { return GetGlobalValue<Matrix4x4>(name); }
        public static Matrix4x4   GetGlobalMatrix(int name)         { return GetGlobalValue<Matrix4x4>(name); }
        public static Texture     GetGlobalTexture(string name)     { return GetGlobalValue<Texture>(name); }
        public static Texture     GetGlobalTexture(int name)        { return GetGlobalValue<Texture>(name); }

        public static float[]     GetGlobalFloatArray(string name)  { return GetGlobalValueArray<float>(name); }
        public static float[]     GetGlobalFloatArray(int name)     { return GetGlobalValueArray<float>(name); }
        public static Vector4[]   GetGlobalVectorArray(string name) { return GetGlobalValueArray<Vector4>(name); }
        public static Vector4[]   GetGlobalVectorArray(int name)    { return GetGlobalValueArray<Vector4>(name); }
        public static Matrix4x4[] GetGlobalMatrixArray(string name) { return GetGlobalValueArray<Matrix4x4>(name); }
        public static Matrix4x4[] GetGlobalMatrixArray(int name)    { return GetGlobalValueArray<Matrix4x4>(name); }

        public static void GetGlobalFloatArray(string name, List<float> values)      { ExtractGlobalValueArray<float>(name, values); }
        public static void GetGlobalFloatArray(int name, List<float> values)         { ExtractGlobalValueArray<float>(name, values); }
        public static void GetGlobalVectorArray(string name, List<Vector4> values)   { ExtractGlobalValueArray<Vector4>(name, values); }
        public static void GetGlobalVectorArray(int name, List<Vector4> values)      { ExtractGlobalValueArray<Vector4>(name, values); }
        public static void GetGlobalMatrixArray(string name, List<Matrix4x4> values) { ExtractGlobalValueArray<Matrix4x4>(name, values); }
        public static void GetGlobalMatrixArray(int name, List<Matrix4x4> values)    { ExtractGlobalValueArray<Matrix4x4>(name, values); }

        private Shader() {}
    }
}


//
// Material
//


namespace UnityEngine
{
    // internal generic methods to have one entry point for using native-scripting glue
    public partial class Material
    {
        internal void SetValue<T>(int name, T value)       { SetValueImpl(name, value, typeof(T)); }
        internal void SetValue<T>(string name, T value)    { SetValueImpl(Shader.PropertyToID(name), value, typeof(T)); }

        internal T    GetValue<T>(int name)     { return (T)GetValueImpl(name, typeof(T)); }
        internal T    GetValue<T>(string name)  { return (T)GetValueImpl(Shader.PropertyToID(name), typeof(T)); }

        internal void SetValueArray<T>(int name, List<T> values)    { SetValueArrayImpl(name, NoAllocHelpers.ExtractArrayFromListT(values), values.Count, typeof(T)); }
        internal void SetValueArray<T>(string name, List<T> values) { SetValueArray<T>(Shader.PropertyToID(name), values); }
        internal void SetValueArray<T>(int name, T[] values)        { SetValueArrayImpl(name, values, values.Length, typeof(T)); }
        internal void SetValueArray<T>(string name, T[] values)     { SetValueArray<T>(Shader.PropertyToID(name), values); }

        internal int  GetValueArrayCount<T>(int name)   { return GetValueArrayCountImpl(name, typeof(T)); }

        // currently we do not support returning null from native if return type is array
        internal T[] GetValueArray<T>(int name)     { return GetValueArrayCount<T>(name) != 0 ? (T[])GetValueArrayImpl(name, typeof(T)) : null; }
        internal T[] GetValueArray<T>(string name)  { return GetValueArray<T>(Shader.PropertyToID(name)); }

        internal void ExtractValueArray<T>(int name, List<T> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            values.Clear();

            int count = GetValueArrayCount<T>(name);
            if (count > 0)
            {
                NoAllocHelpers.EnsureListElemCount(values, count);
                ExtractValueArrayImpl(name, NoAllocHelpers.ExtractArrayFromList(values), typeof(T));
            }
        }

        internal void ExtractValueArray<T>(string name, List<T> values) { ExtractValueArray<T>(Shader.PropertyToID(name), values); }
    }

    public partial class Material
    {
        public void SetFloat(string name, float value)          { SetValue(name, value); }
        public void SetFloat(int name, float value)             { SetValue(name, value); }
        public void SetInt(string name, int value)              { SetValue(name, value); }
        public void SetInt(int name, int value)                 { SetValue(name, value); }
        public void SetColor(string name, Color value)          { SetValue(name, value); }
        public void SetColor(int name, Color value)             { SetValue(name, value); }
        public void SetVector(string name, Vector4 value)       { SetValue(name, value); }
        public void SetVector(int name, Vector4 value)          { SetValue(name, value); }
        public void SetMatrix(string name, Matrix4x4 value)     { SetValue(name, value); }
        public void SetMatrix(int name, Matrix4x4 value)        { SetValue(name, value); }
        public void SetTexture(string name, Texture value)      { SetValue(name, value); }
        public void SetTexture(int name, Texture value)         { SetValue(name, value); }
        public void SetBuffer(string name, ComputeBuffer value) { SetValue(name, value); }
        public void SetBuffer(int name, ComputeBuffer value)    { SetValue(name, value); }

        public void SetFloatArray(string name, List<float> values)  { SetValueArray(name, values); }
        public void SetFloatArray(int name,    List<float> values)  { SetValueArray(name, values); }
        public void SetFloatArray(string name, float[] values)      { SetValueArray(name, values); }
        public void SetFloatArray(int name,    float[] values)      { SetValueArray(name, values); }

        public void SetColorArray(string name, List<Color> values)  { SetValueArray(name, values); }
        public void SetColorArray(int name,    List<Color> values)  { SetValueArray(name, values); }
        public void SetColorArray(string name, Color[] values)      { SetValueArray(name, values); }
        public void SetColorArray(int name,    Color[] values)      { SetValueArray(name, values); }

        public void SetVectorArray(string name, List<Vector4> values)   { SetValueArray(name, values); }
        public void SetVectorArray(int name,    List<Vector4> values)   { SetValueArray(name, values); }
        public void SetVectorArray(string name, Vector4[] values)       { SetValueArray(name, values); }
        public void SetVectorArray(int name,    Vector4[] values)       { SetValueArray(name, values); }

        public void SetMatrixArray(string name, List<Matrix4x4> values) { SetValueArray(name, values); }
        public void SetMatrixArray(int name,    List<Matrix4x4> values) { SetValueArray(name, values); }
        public void SetMatrixArray(string name, Matrix4x4[] values)     { SetValueArray(name, values); }
        public void SetMatrixArray(int name,    Matrix4x4[] values)     { SetValueArray(name, values); }

        public float     GetFloat(string name)  { return GetValue<float>(name); }
        public float     GetFloat(int name)     { return GetValue<float>(name); }
        public int       GetInt(string name)    { return GetValue<int>(name); }
        public int       GetInt(int name)       { return GetValue<int>(name); }
        public Color     GetColor(string name)  { return GetValue<Color>(name); }
        public Color     GetColor(int name)     { return GetValue<Color>(name); }
        public Vector4   GetVector(string name) { return GetValue<Vector4>(name); }
        public Vector4   GetVector(int name)    { return GetValue<Vector4>(name); }
        public Matrix4x4 GetMatrix(string name) { return GetValue<Matrix4x4>(name); }
        public Matrix4x4 GetMatrix(int name)    { return GetValue<Matrix4x4>(name); }
        public Texture   GetTexture(string name) { return GetValue<Texture>(name); }
        public Texture   GetTexture(int name)   { return GetValue<Texture>(name); }

        public float[]      GetFloatArray(string name)  { return GetValueArray<float>(name); }
        public float[]      GetFloatArray(int name)     { return GetValueArray<float>(name); }
        public Color[]      GetColorArray(string name)  { return GetValueArray<Color>(name); }
        public Color[]      GetColorArray(int name)     { return GetValueArray<Color>(name); }
        public Vector4[]    GetVectorArray(string name) { return GetValueArray<Vector4>(name); }
        public Vector4[]    GetVectorArray(int name)    { return GetValueArray<Vector4>(name); }
        public Matrix4x4[]  GetMatrixArray(string name) { return GetValueArray<Matrix4x4>(name); }
        public Matrix4x4[]  GetMatrixArray(int name)    { return GetValueArray<Matrix4x4>(name); }

        public void GetFloatArray(string name, List<float> values)      { ExtractValueArray<float>(name, values); }
        public void GetFloatArray(int name, List<float> values)         { ExtractValueArray<float>(name, values); }
        public void GetColorArray(string name, List<Color> values)      { ExtractValueArray<Color>(name, values); }
        public void GetColorArray(int name, List<Color> values)         { ExtractValueArray<Color>(name, values); }
        public void GetVectorArray(string name, List<Vector4> values)   { ExtractValueArray<Vector4>(name, values); }
        public void GetVectorArray(int name, List<Vector4> values)      { ExtractValueArray<Vector4>(name, values); }
        public void GetMatrixArray(string name, List<Matrix4x4> values) { ExtractValueArray<Matrix4x4>(name, values); }
        public void GetMatrixArray(int name, List<Matrix4x4> values)    { ExtractValueArray<Matrix4x4>(name, values); }

        public void SetTextureOffset(string name, Vector2 value) { SetTextureOffsetImpl(Shader.PropertyToID(name), value); }
        public void SetTextureOffset(int name, Vector2 value)    { SetTextureOffsetImpl(name, value); }
        public void SetTextureScale(string name, Vector2 value)  { SetTextureScaleImpl(Shader.PropertyToID(name), value); }
        public void SetTextureScale(int name, Vector2 value)     { SetTextureScaleImpl(name, value); }

        public Vector2 GetTextureOffset(string name) { return GetTextureOffset(Shader.PropertyToID(name)); }
        public Vector2 GetTextureOffset(int name)    { Vector4 st = GetTextureScaleAndOffsetImpl(name); return new Vector2(st.z, st.w); }
        public Vector2 GetTextureScale(string name)  { return GetTextureScale(Shader.PropertyToID(name)); }
        public Vector2 GetTextureScale(int name)     { Vector4 st = GetTextureScaleAndOffsetImpl(name); return new Vector2(st.x, st.y); }
    }
}


//
// QualitySettings
//


namespace UnityEngine
{
    public sealed partial class QualitySettings
    {
        public static void IncreaseLevel([uei.DefaultValue("false")] bool applyExpensiveChanges)
        {
            SetQualityLevel(GetQualityLevel() + 1, applyExpensiveChanges);
        }

        public static void DecreaseLevel([uei.DefaultValue("false")] bool applyExpensiveChanges)
        {
            SetQualityLevel(GetQualityLevel() - 1, applyExpensiveChanges);
        }

        public static void SetQualityLevel(int index) { SetQualityLevel(index, true); }
        public static void IncreaseLevel() { IncreaseLevel(false); }
        public static void DecreaseLevel() { DecreaseLevel(false); }
    }
}

//
// Attributes
//


namespace UnityEngine
{
    [UsedByNativeCode]
    public sealed partial class ImageEffectTransformsToLDR : Attribute
    {
    }

    public sealed partial class ImageEffectAllowedInSceneView : Attribute
    {
    }

    [UsedByNativeCode]
    public sealed partial class ImageEffectOpaque : Attribute
    {
    }

    [UsedByNativeCode]
    public sealed partial class ImageEffectAfterScale : Attribute
    {
    }
}
