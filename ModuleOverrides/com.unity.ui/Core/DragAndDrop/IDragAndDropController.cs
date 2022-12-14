// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal interface IDragAndDropController<in TArgs>
    {
        bool CanStartDrag(IEnumerable<int> itemIndices);
        StartDragArgs SetupDragAndDrop(IEnumerable<int> itemIndices, bool skipText = false);
        DragVisualMode HandleDragAndDrop(TArgs args);
        void OnDrop(TArgs args);
    }

    internal enum DragVisualMode
    {
        None,
        Copy,
        Move,
        Rejected
    }

    internal class StartDragArgs
    {
        public string title { get; }
        public object userData { get; }

        private readonly Hashtable m_GenericData = new Hashtable();

        internal Hashtable genericData => m_GenericData;
        internal IEnumerable<Object> unityObjectReferences { get; private set; } = null;

        internal StartDragArgs()
        {
            title = string.Empty;
        }

        public StartDragArgs(string title, object userData)
        {
            this.title = title;
            this.userData = userData;
        }

        public void SetGenericData(string key, object data)
        {
            m_GenericData[key] = data;
        }

        public void SetUnityObjectReferences(IEnumerable<Object> references)
        {
            unityObjectReferences = references;
        }
    }
}
