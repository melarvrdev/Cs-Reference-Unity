// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEngine.XR.Tango
{
    [RequiredByNativeCode]
    public static partial class TangoInputTracking
    {
        // Must match enum in Runtime/AR/Tango/TangoTypes.h
        private enum TrackingStateEventType
        {
            TrackingAcquired,
            TrackingLost
        }

        public static event Action<CoordinateFrame> trackingAcquired = null;
        public static event Action<CoordinateFrame> trackingLost = null;

        [RequiredByNativeCode]
        private static void InvokeTangoTrackingEvent(TrackingStateEventType eventType, CoordinateFrame frame)
        {
            Action<CoordinateFrame> callback = null;

            switch (eventType)
            {
                case TrackingStateEventType.TrackingAcquired:
                    callback = trackingAcquired;
                    break;
                case TrackingStateEventType.TrackingLost:
                    callback = trackingLost;
                    break;
                default:
                    throw new ArgumentException("TrackingEventHandler - Invalid EventType: " + eventType);
            }

            if (callback != null)
            {
                callback(frame);
            }
        }
    }
}
