// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;


namespace UnityEngine.XR.WSA.Sharing
{
    [MovedFrom("UnityEngine.VR.WSA.Sharing")]
    public enum SerializationCompletionReason
    {
        Succeeded = 0,
        NotSupported = 1,
        AccessDenied = 2,
        UnknownError = 3
    }

    [NativeHeader("Modules/VR/HoloLens/WorldAnchor/WorldAnchorTransferBatch.h")]
    [NativeHeader("VRScriptingClasses.h")]
    [MovedFrom("UnityEngine.VR.WSA.Sharing")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]   // needed for IntPtr binding classes
    public partial class WorldAnchorTransferBatch : IDisposable
    {
        internal IntPtr m_NativePtr = IntPtr.Zero;

        public delegate void SerializationDataAvailableDelegate(byte[] data);
        public delegate void SerializationCompleteDelegate(SerializationCompletionReason completionReason);
        public delegate void DeserializationCompleteDelegate(SerializationCompletionReason completionReason, WorldAnchorTransferBatch deserializedTransferBatch);

        private WorldAnchorTransferBatch(IntPtr nativePtr)
        {
            m_NativePtr = nativePtr;
        }

        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.2. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public static void ExportAsync(WorldAnchorTransferBatch transferBatch, SerializationDataAvailableDelegate onDataAvailable, SerializationCompleteDelegate onCompleted)
        {
            if (transferBatch == null)
            {
                throw new ArgumentNullException("transferBatch");
            }
            if (onDataAvailable == null)
            {
                throw new ArgumentNullException("onDataAvailable");
            }
            if (onCompleted == null)
            {
                throw new ArgumentNullException("onCompleted");
            }

            onCompleted(SerializationCompletionReason.NotSupported);
        }


        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.2. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public static void ImportAsync(byte[] serializedData, DeserializationCompleteDelegate onComplete)
        {
            ImportAsync(serializedData, 0, serializedData.Length, onComplete);
        }

        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.2. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public static void ImportAsync(byte[] serializedData, int offset, int length, DeserializationCompleteDelegate onComplete)
        {
            if (serializedData == null)
            {
                throw new ArgumentNullException("serializedData");
            }
            if (serializedData.Length < 1)
            {
                throw new ArgumentException("serializedData is empty!", "serializedData");
            }
            if ((offset + length) > serializedData.Length)
            {
                throw new ArgumentException("offset + length is greater that serializedData.Length!");
            }
            if (onComplete == null)
            {
                throw new ArgumentNullException("onComplete");
            }

            onComplete(SerializationCompletionReason.NotSupported, new WorldAnchorTransferBatch());
        }


        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.2. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public bool AddWorldAnchor(string id, WorldAnchor anchor)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("id is null or empty!", "id");
            }
            if (anchor == null)
            {
                throw new ArgumentNullException("anchor");
            }
            return false;
        }


        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.2. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public extern int anchorCount { get; }

        public int GetAllIds(string[] ids)
        {
            if (ids == null)
                throw new ArgumentNullException("ids");

            return 0;
        }

        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.2. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public string[] GetAllIds()
        {
            return new string[0];
        }


        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.2. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public WorldAnchor LockObject(string id, GameObject go)
        {
            return null;
        }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("LoadAnchor")]
        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.2. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        private extern bool LoadAnchor_Internal(string id, WorldAnchor anchor);

        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.2. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public WorldAnchorTransferBatch()
        {
            m_NativePtr = Create_Internal();
        }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("Create")]
        private extern static IntPtr Create_Internal();

        ~WorldAnchorTransferBatch()
        {
            if (m_NativePtr != IntPtr.Zero)
            {
                DisposeThreaded_Internal();
                m_NativePtr = IntPtr.Zero;
            }
        }

        [ThreadAndSerializationSafe()]
        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("DisposeThreaded")]
        private extern void DisposeThreaded_Internal();

        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.2. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public void Dispose()
        {
            if (m_NativePtr != IntPtr.Zero)
            {
                Dispose_Internal();
                m_NativePtr = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("Dispose")]
        private extern void Dispose_Internal();

        [RequiredByNativeCode]
        private static void InvokeWorldAnchorSerializationDataAvailableDelegate(SerializationDataAvailableDelegate onSerializationDataAvailable, byte[] data)
        {
            onSerializationDataAvailable(data);
        }

        [RequiredByNativeCode]
        private static void InvokeWorldAnchorSerializationCompleteDelegate(SerializationCompleteDelegate onSerializationComplete, SerializationCompletionReason completionReason)
        {
            onSerializationComplete(completionReason);
        }

        [RequiredByNativeCode]
        private static void InvokeWorldAnchorDeserializationCompleteDelegate(DeserializationCompleteDelegate onDeserializationComplete, SerializationCompletionReason completionReason, IntPtr nativePtr)
        {
            WorldAnchorTransferBatch watb = new WorldAnchorTransferBatch(nativePtr);
            onDeserializationComplete(completionReason, watb);
        }
    }
}

