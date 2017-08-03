// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    public interface IDataWatchHandle : IDisposable
    {
        Object watched { get; }
        bool disposed { get; }
    }

    public interface IDataWatchService
    {
        IDataWatchHandle AddWatch(VisualElement watcher, Object watched, Action OnDataChanged);
        void ForceDirtyNextPoll(Object obj);
    }
}
