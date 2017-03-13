// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.Playables
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FrameData
    {
        [Flags]
        internal enum Flags
        {
            Evaluate    = 1,
            SeekOccured = 2
        }

        public enum EvaluationType
        {
            Evaluate,
            Playback
        }

        internal ulong m_FrameID;
        internal double m_DeltaTime;
        internal float m_Weight;
        internal float m_EffectiveWeight;
        internal float m_EffectiveSpeed;
        internal Flags m_Flags;

        public ulong frameId                    { get { return m_FrameID; } }
        public float deltaTime                  { get { return (float)m_DeltaTime; } }
        public float weight                     { get { return m_Weight; } }
        public float effectiveWeight            { get { return m_EffectiveWeight; } }
        public float effectiveSpeed             { get { return m_EffectiveSpeed; } }
        public EvaluationType evaluationType    { get { return ((m_Flags & Flags.Evaluate) != 0) ? EvaluationType.Evaluate : EvaluationType.Playback; } }
        public bool seekOccurred                { get { return (m_Flags & Flags.SeekOccured) != 0; } }
    }
}
