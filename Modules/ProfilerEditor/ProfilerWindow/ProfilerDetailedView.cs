// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;

namespace UnityEditorInternal.Profiling
{
    internal abstract class ProfilerDetailedView
    {
        protected static readonly string kNoneText = LocalizationDatabase.GetLocalizedString("None");

        protected static class Styles
        {
            public static GUIContent emptyText = new GUIContent("");
            public static GUIContent selectLineText = EditorGUIUtility.TrTextContent("Select Line for the detailed information");

            public static readonly GUIStyle expandedArea = new GUIStyle();
            public static readonly GUIStyle callstackScroll = new GUIStyle("CN Box");
            public static readonly GUIStyle callstackTextArea = new GUIStyle("CN Message");

            static Styles()
            {
                expandedArea.stretchWidth = true;
                expandedArea.stretchHeight = true;
                expandedArea.padding = new RectOffset(0, 0, 0, 0);

                callstackScroll.padding = new RectOffset(5, 5, 5, 5);

                callstackTextArea.margin = new RectOffset(0, 0, 0, 0);
                callstackTextArea.padding = new RectOffset(3, 3, 3, 3);
                callstackTextArea.wordWrap = false;
                callstackTextArea.stretchWidth = true;
                callstackTextArea.stretchHeight = true;
            }
        }

        protected HierarchyFrameDataView m_FrameDataView;

        protected CPUOrGPUProfilerModule m_CPUOrGPUProfilerModule;
        [NonSerialized]
        protected ProfilerFrameDataHierarchyView m_ProfilerFrameDataHierarchyView;

        [SerializeField]
        protected int m_SelectedID = -1;

        protected void DrawEmptyPane(GUIStyle headerStyle)
        {
            GUILayout.Box(Styles.emptyText, headerStyle);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label(Styles.selectLineText, EditorStyles.wordWrappedLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public abstract void SaveViewSettings();
        public virtual void OnEnable(CPUOrGPUProfilerModule cpuOrGpuProfilerModule, ProfilerFrameDataHierarchyView profilerFrameDataHierarchyView)
        {
            m_CPUOrGPUProfilerModule = cpuOrGpuProfilerModule;
            m_ProfilerFrameDataHierarchyView = profilerFrameDataHierarchyView;
        }

        public abstract void OnDisable();
    }
}
