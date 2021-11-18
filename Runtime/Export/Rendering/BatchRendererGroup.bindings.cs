// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine.Scripting;
using UnityEngine.Bindings;

using Unity.Jobs;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Camera/BatchRendererGroup.h")]
    [NativeClass("BatchID")]
    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchID : IEquatable<BatchID>
    {
        public readonly static BatchID Null = new BatchID { value = 0 };

        public uint value;

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is BatchID)
            {
                return Equals((BatchID)obj);
            }

            return false;
        }

        public bool Equals(BatchID other)
        {
            return value == other.value;
        }

        public int CompareTo(BatchID other)
        {
            return value.CompareTo(other.value);
        }

        public static bool operator ==(BatchID a, BatchID b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(BatchID a, BatchID b)
        {
            return !a.Equals(b);
        }
    }

    [NativeHeader("Runtime/Camera/BatchRendererGroup.h")]
    [NativeClass("BatchMaterialID")]
    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchMaterialID : IEquatable<BatchMaterialID>
    {
        public readonly static BatchMaterialID Null = new BatchMaterialID { value = 0 };

        public uint value;

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is BatchMaterialID)
            {
                return Equals((BatchMaterialID)obj);
            }

            return false;
        }

        public bool Equals(BatchMaterialID other)
        {
            return value == other.value;
        }

        public int CompareTo(BatchMaterialID other)
        {
            return value.CompareTo(other.value);
        }

        public static bool operator ==(BatchMaterialID a, BatchMaterialID b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(BatchMaterialID a, BatchMaterialID b)
        {
            return !a.Equals(b);
        }
    }

    [NativeHeader("Runtime/Camera/BatchRendererGroup.h")]
    [NativeClass("BatchMeshID")]
    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchMeshID : IEquatable<BatchMeshID>
    {
        public readonly static BatchMeshID Null = new BatchMeshID { value = 0 };

        public uint value;

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is BatchMeshID)
            {
                return Equals((BatchMeshID)obj);
            }

            return false;
        }

        public bool Equals(BatchMeshID other)
        {
            return value == other.value;
        }

        public int CompareTo(BatchMeshID other)
        {
            return value.CompareTo(other.value);
        }

        public static bool operator ==(BatchMeshID a, BatchMeshID b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(BatchMeshID a, BatchMeshID b)
        {
            return !a.Equals(b);
        }
    }

    // Match with BatchDrawCommandFlags in C++ side
    [Flags]
    public enum BatchDrawCommandFlags : int
    {
        None = 0,
        FlipWinding = 1 << 0, // Flip triangle winding when rendering, e.g. when the scale is negative
        HasMotion = 1 << 1, // Draw command contains at least one instance that requires per-object motion vectors
        IsLightMapped = 1 << 2, // Draw command contains lightmapped objects, which has implications for setting some lighting constants
        HasSortingPosition = 1 << 3, // Draw command instances have explicit world space float3 sorting positions to be used for depth sorting
        LODCrossFade = 1 << 4, // Draw command instances have a 8-bit SNORM crossfade dither factor in the highest bits of their visible instance index
    }

    // Match with BatchCullingViewType in C++ side
    public enum BatchCullingViewType : int
    {
        Unknown = 0,
        Camera = 1,
        Light = 2,
        ShadowMap = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BatchPackedCullingViewID : IEquatable<BatchPackedCullingViewID>
    {
        internal ulong handle;

        public override int GetHashCode()
        {
            return handle.GetHashCode();
        }

        public bool Equals(BatchPackedCullingViewID other)
        {
            return handle == other.handle;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BatchPackedCullingViewID))
            {
                return false;
            }
            return this.Equals((BatchPackedCullingViewID)obj);
        }

        public static bool operator ==(BatchPackedCullingViewID lhs, BatchPackedCullingViewID rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(BatchPackedCullingViewID lhs, BatchPackedCullingViewID rhs)
        {
            return !lhs.Equals(rhs);
        }

        public BatchPackedCullingViewID(int instanceID, int sliceIndex)
        {
            handle = (uint) instanceID | ((ulong)sliceIndex << 32);
        }

        public int GetInstanceID()
        {
            return (int)(handle & 0xffffffff);
        }

        public int GetSliceIndex()
        {
            return (int)(handle >> 32);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BatchDrawCommand
    {
        public uint visibleOffset;
        public uint visibleCount;
        public BatchID batchID;
        public BatchMaterialID materialID;
        public BatchMeshID meshID;
        public uint submeshIndex;
        public BatchDrawCommandFlags flags; // includes flipWinding and other dynamic flags
        public int sortingPosition; // If HasSortingPosition is set, this points to a float3 in instanceSortingPositions. If not, it will be directly casted into float and used as the distance.
    }

    // Match with BatchFilterSettings in C++ side
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchFilterSettings
    {
        public uint renderingLayerMask;
        public byte layer;
        private byte m_motionMode;
        private byte m_shadowMode;
        private byte m_receiveShadows;
        private byte m_staticShadowCaster;
        private byte m_allDepthSorted;

        public MotionVectorGenerationMode motionMode
        {
            get => (MotionVectorGenerationMode)m_motionMode;
            set => m_motionMode = (byte)value;
        }

        public ShadowCastingMode shadowCastingMode
        {
            get => (ShadowCastingMode)m_shadowMode;
            set => m_shadowMode = (byte)value;
        }

        public bool receiveShadows
        {
            get => m_receiveShadows != 0;
            set => m_receiveShadows = (byte)(value ? 1 : 0);
        }

        public bool staticShadowCaster
        {
            get => m_staticShadowCaster != 0;
            set => m_staticShadowCaster = (byte)(value ? 1 : 0);
        }

        public bool allDepthSorted
        {
            get => m_allDepthSorted != 0;
            set => m_allDepthSorted = (byte)(value ? 1 : 0);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BatchDrawRange
    {
        // The first BatchDrawCommand of this range is at this index in BatchCullingOutputDrawCommands.drawCommands
        public uint drawCommandsBegin;
        // How many BatchDrawCommand structs this range has. Can be 0 if there are no draws.
        public uint drawCommandsCount;
        // Filter settings for every draw in the range. If the filter settings don't match, the entire range can be skipped.
        public BatchFilterSettings filterSettings;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct BatchCullingOutputDrawCommands
    {
        // TempJob allocated by C#, released by C++
        public BatchDrawCommand* drawCommands;
        // TempJob allocated by C#, released by C++
        public int* visibleInstances;
        // TempJob allocated by C#, released by C++
        public BatchDrawRange* drawRanges;
        // TempJob allocated by C#, released by C++
        public float* instanceSortingPositions;
        public int drawCommandCount;
        public int visibleInstanceCount;
        public int drawRangeCount;
        public int instanceSortingPositionFloatCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MetadataValue
    {
        public int NameID;
        public uint Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Camera/BatchRendererGroup.h")]
    [UsedByNativeCode]
    unsafe public struct BatchCullingContext
    {
        internal BatchCullingContext(
            NativeArray<Plane> inCullingPlanes,
            LODParameters inLodParameters,
            Matrix4x4 inCullingMatrix,
            float inNearPlane,
            BatchCullingViewType inViewType,
            ulong inViewID,
            uint inCullingLayerMask,
            ulong inSceneCullingMask)
        {
            cullingPlanes = inCullingPlanes;
            lodParameters = inLodParameters;
            cullingMatrix = inCullingMatrix;
            nearPlane = inNearPlane;
            viewType = inViewType;
            viewID = new BatchPackedCullingViewID { handle = inViewID };
            cullingLayerMask = inCullingLayerMask;
            sceneCullingMask = inSceneCullingMask;
        }

        readonly public NativeArray<Plane> cullingPlanes;
        readonly public LODParameters lodParameters;
        readonly public Matrix4x4 cullingMatrix;
        readonly public float nearPlane;
        readonly public BatchCullingViewType viewType;
        readonly public BatchPackedCullingViewID viewID;
        readonly public uint cullingLayerMask;
        readonly public ulong sceneCullingMask;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BatchCullingOutput
    {
        // One-element NativeArray to make it writable from C#
        public NativeArray<BatchCullingOutputDrawCommands> drawCommands;
    }


    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Camera/BatchRendererGroup.h")]
    [UsedByNativeCode]
    unsafe struct BatchRendererCullingOutput
    {
        public JobHandle cullingJobsFence;
        public Matrix4x4 cullingMatrix;
        public Plane* cullingPlanes;
        public int cullingPlanesCount;
        public float nearPlane;
        public BatchCullingViewType viewType;
        public ulong viewID;
        public uint  cullingLayerMask;
        public ulong sceneCullingMask;
        public BatchCullingOutputDrawCommands* drawCommands;
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Camera/BatchRendererGroup.h")]
    public struct ThreadedBatchContext
    {
        public IntPtr batchRendererGroup;

        [FreeFunction("BatchRendererGroup::AddDrawCommandBatch_Threaded", IsThreadSafe = true)]
        private extern static BatchID AddDrawCommandBatch(IntPtr brg, IntPtr values, int count, GraphicsBufferHandle buffer);

        [FreeFunction("BatchRendererGroup::RemoveDrawCommandBatch_Threaded", IsThreadSafe = true)]
        private extern static void RemoveDrawCommandBatch(IntPtr brg, BatchID batchID);


        unsafe public BatchID AddBatch(NativeArray<MetadataValue> batchMetadata, GraphicsBufferHandle buffer)
        {
            return AddDrawCommandBatch(batchRendererGroup, (IntPtr)batchMetadata.GetUnsafeReadOnlyPtr(), batchMetadata.Length, buffer);
        }

        public void RemoveBatch(BatchID batchID)
        {
            RemoveDrawCommandBatch(batchRendererGroup, batchID);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Math/Matrix4x4.h")]
    [NativeHeader("Runtime/Camera/BatchRendererGroup.h")]
    [RequiredByNativeCode]
    public class BatchRendererGroup : IDisposable
    {
        IntPtr m_GroupHandle = IntPtr.Zero;
        OnPerformCulling m_PerformCulling;

        unsafe public delegate JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext);

        public unsafe BatchRendererGroup(OnPerformCulling cullingCallback, IntPtr userContext)
        {
            m_PerformCulling = cullingCallback;
            m_GroupHandle = Create(this, (void*)userContext);
        }

        public void Dispose()
        {
            Destroy(m_GroupHandle);
            m_GroupHandle = IntPtr.Zero;
        }

        public ThreadedBatchContext GetThreadedBatchContext()
        {
            return new ThreadedBatchContext { batchRendererGroup = m_GroupHandle };
        }

        private extern BatchID AddDrawCommandBatch(IntPtr values, int count, GraphicsBufferHandle buffer);
        unsafe public BatchID AddBatch(NativeArray<MetadataValue> batchMetadata, GraphicsBufferHandle buffer)
        {
            return AddDrawCommandBatch((IntPtr)batchMetadata.GetUnsafeReadOnlyPtr(), batchMetadata.Length, buffer);
        }

        private extern void RemoveDrawCommandBatch(BatchID batchID);
        public void RemoveBatch(BatchID batchID) { RemoveDrawCommandBatch(batchID); }

        public extern BatchMaterialID RegisterMaterial(Material material);
        public extern void UnregisterMaterial(BatchMaterialID material);
        public extern Material GetRegisteredMaterial(BatchMaterialID material);

        public extern BatchMeshID RegisterMesh(Mesh mesh);
        public extern void UnregisterMesh(BatchMeshID mesh);
        public extern Mesh GetRegisteredMesh(BatchMeshID mesh);

        public extern void SetGlobalBounds(Bounds bounds);

        static extern unsafe IntPtr Create(BatchRendererGroup group, void* userContext);
        static extern void Destroy(IntPtr groupHandle);

        [RequiredByNativeCode]
        unsafe static void InvokeOnPerformCulling(BatchRendererGroup group, ref BatchRendererCullingOutput context, ref LODParameters lodParameters, IntPtr userContext)
        {
            NativeArray<Plane> cullingPlanes = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Plane>(context.cullingPlanes, context.cullingPlanesCount, Allocator.Invalid);
            NativeArray<BatchCullingOutputDrawCommands> drawCommands = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<BatchCullingOutputDrawCommands>(
                context.drawCommands, 1, Allocator.Invalid);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref cullingPlanes, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref drawCommands, AtomicSafetyHandle.Create());

            try
            {
                BatchCullingOutput cullingOutput = new BatchCullingOutput
                {
                    drawCommands = drawCommands,
                };
                context.cullingJobsFence = group.m_PerformCulling(
                    group, new BatchCullingContext(
                        cullingPlanes,
                        lodParameters,
                        context.cullingMatrix,
                        context.nearPlane,
                        context.viewType,
                        context.viewID,
                        context.cullingLayerMask,
                        context.sceneCullingMask
                    ),
                    cullingOutput,
                    userContext
                );
            }
            finally
            {
                JobHandle.ScheduleBatchedJobs();

                //@TODO: Check that the no jobs using the buffers have been scheduled that are not returned here...
                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(cullingPlanes));
                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(drawCommands));
            }
        }
    }
}
