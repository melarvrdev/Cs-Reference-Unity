// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.VFX
{
    public enum VFXSpawnerLoopState
    {
        Finished,
        DelayingBeforeLoop,
        Looping,
        DelayingAfterLoop
    }

    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(Header = "Modules/VFX/Public/VFXSpawnerState.h")]
    public sealed class VFXSpawnerState : IDisposable
    {
        private IntPtr m_Ptr;
        private bool m_Owner;

        public VFXSpawnerState() : this(Internal_Create(), true)
        {
        }

        internal VFXSpawnerState(IntPtr ptr, bool owner)
        {
            m_Ptr = ptr;
            m_Owner = owner;
        }

        extern static internal IntPtr Internal_Create();

        [RequiredByNativeCode]
        internal static VFXSpawnerState CreateSpawnerStateWrapper()
        {
            var spawnerState = new VFXSpawnerState(IntPtr.Zero, false);
            return spawnerState;
        }

        [RequiredByNativeCode]
        internal void SetWrapValue(IntPtr ptr)
        {
            if (m_Owner)
            {
                throw new Exception("VFXSpawnerState : SetWrapValue is reserved to CreateWrapper object");
            }
            m_Ptr = ptr;
        }

        internal IntPtr GetPtr()
        {
            return m_Ptr;
        }

        private void Release()
        {
            if (m_Ptr != IntPtr.Zero && m_Owner)
            {
                Internal_Destroy(m_Ptr);
            }
            m_Ptr = IntPtr.Zero;
        }

        ~VFXSpawnerState()
        {
            Release();
        }

        public void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }

        [NativeMethod(IsThreadSafe = true)]
        extern static private void Internal_Destroy(IntPtr ptr);

        public bool playing
        {
            get
            {
                return loopState == VFXSpawnerLoopState.Looping;
            }
            set
            {
                loopState = value ? VFXSpawnerLoopState.Looping : VFXSpawnerLoopState.Finished;
            }
        }
        extern public bool newLoop { get; }
        extern public VFXSpawnerLoopState loopState { get; set; }
        extern public float spawnCount { get; set; }
        extern public float deltaTime { get; set; }
        extern public float totalTime { get; set; }
        extern public float delayBeforeLoop { get; set; }
        extern public float loopDuration { get; set; }
        extern public float delayAfterLoop { get; set; }
        extern public int loopIndex { get; set; }
        extern public int loopCount { get; set; }
        extern public VFXEventAttribute vfxEventAttribute { get; }
    }
}
