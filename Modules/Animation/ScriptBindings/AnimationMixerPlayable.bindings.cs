// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Animations
{
    [NativeHeader("Modules/Animation/ScriptBindings/AnimationMixerPlayable.bindings.h")]
    [NativeHeader("Modules/Animation/Director/AnimationMixerPlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("AnimationMixerPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct AnimationMixerPlayable : IPlayable, IEquatable<AnimationMixerPlayable>
    {
        PlayableHandle m_Handle;

        static readonly AnimationMixerPlayable m_NullPlayable = new AnimationMixerPlayable(PlayableHandle.Null);
        public static AnimationMixerPlayable Null { get { return m_NullPlayable; } }

        [Obsolete("normalizeWeights is obsolete. It has no effect and will be removed.")]
        public static AnimationMixerPlayable Create(PlayableGraph graph, int inputCount, bool normalizeWeights)
        {
            return Create(graph, inputCount);
        }

        public static AnimationMixerPlayable Create(PlayableGraph graph, int inputCount = 0)
        {
            var handle = CreateHandle(graph, inputCount);
            return new AnimationMixerPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, int inputCount = 0)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateHandleInternal(graph, ref handle))
                return PlayableHandle.Null;
            handle.SetInputCount(inputCount);
            return handle;
        }

        internal AnimationMixerPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AnimationMixerPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimationMixerPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(AnimationMixerPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator AnimationMixerPlayable(Playable playable)
        {
            return new AnimationMixerPlayable(playable.GetHandle());
        }

        public bool Equals(AnimationMixerPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }

        [NativeThrows]
        extern private static bool CreateHandleInternal(PlayableGraph graph, ref PlayableHandle handle);
    }
}
