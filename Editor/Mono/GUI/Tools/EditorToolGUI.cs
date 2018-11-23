// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UObject = UnityEngine.Object;
using UnityEditor.EditorTools;

namespace UnityEditor
{
    public sealed partial class EditorGUILayout
    {
        static readonly EditorToolGUI.ReusableArrayPool<GUIContent> s_ButtonArrays = new EditorToolGUI.ReusableArrayPool<GUIContent>();
        static readonly EditorToolGUI.ReusableArrayPool<bool> s_BoolArrays = new EditorToolGUI.ReusableArrayPool<bool>();
        static readonly List<EditorTool> s_CustomEditorTools = new List<EditorTool>();

        public static void EditorToolbarForTarget(UObject target)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            var targetType = target.GetType();
            s_CustomEditorTools.Clear();
            EditorToolContext.GetCustomEditorTools(targetType, s_CustomEditorTools);
            EditorToolbar<EditorTool>(s_CustomEditorTools);
        }

        public static void EditorToolbar(params EditorTool[] tools)
        {
            EditorToolbar<EditorTool>(tools);
        }

        public static void EditorToolbar<T>(IList<T> tools) where T : EditorTool
        {
            int toolsLength = tools.Count;
            int selected = -1;
            var buttons = s_ButtonArrays.Get(toolsLength);
            var enabled = s_BoolArrays.Get(toolsLength);

            for (int i = 0; i < toolsLength; i++)
            {
                // can happen if the user deletes a tool through scripting
                if (tools[i] == null)
                {
                    buttons[i] = GUIContent.none;
                    continue;
                }

                if (tools[i] == EditorToolContext.activeTool)
                    selected = i;

                enabled[i] = tools[i].IsAvailable();
                buttons[i] = tools[i].toolbarIcon ?? GUIContent.none;
            }

            int previous = selected;

            EditorGUI.BeginChangeCheck();

            selected = GUILayout.Toolbar(selected, buttons, enabled, "Command");

            if (EditorGUI.EndChangeCheck())
            {
                if (selected == previous)
                    EditorToolContext.RestorePreviousTool();
                else
                    EditorToolContext.activeTool = tools[selected];
            }
        }
    }

    static class EditorToolGUI
    {
        static class Styles
        {
            public static GUIContent selectionToolsWindowTitle = EditorGUIUtility.TrTextContent("Tools");
            public static GUIContent recentTools = EditorGUIUtility.TrTextContent("Recent");
            public static GUIContent selectionTools = EditorGUIUtility.TrTextContent("Selection");
            public static GUIContent availableTools = EditorGUIUtility.TrTextContent("Available");
            public static GUIContent noToolsAvailable = EditorGUIUtility.TrTextContent("No tools for selected components.");
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal class ReusableArrayPool<T>
        {
            Dictionary<int, T[]> m_Pool = new Dictionary<int, T[]>();
            int m_MaxEntries = 8;

            public int maxEntries
            {
                get { return m_MaxEntries; }
                set { m_MaxEntries = value; }
            }

            public T[] Get(int count)
            {
                T[] res;
                if (m_Pool.TryGetValue(count, out res))
                    return res;
                if (m_Pool.Count > m_MaxEntries)
                    m_Pool.Clear();
                m_Pool.Add(count, res = new T[count]);
                return res;
            }
        }

        static readonly List<EditorTool> s_ToolList = new List<EditorTool>();

        static GUIContent[] s_PivotIcons = new GUIContent[]
        {
            EditorGUIUtility.TrTextContentWithIcon("Center", "Toggle Tool Handle Position\n\nThe tool handle is placed at the center of the selection.", "ToolHandleCenter"),
            EditorGUIUtility.TrTextContentWithIcon("Pivot", "Toggle Tool Handle Position\n\nThe tool handle is placed at the active object's pivot point.", "ToolHandlePivot"),
        };

        static GUIContent[] s_PivotRotation = new GUIContent[]
        {
            EditorGUIUtility.TrTextContentWithIcon("Local", "Toggle Tool Handle Rotation\n\nTool handles are in the active object's rotation.", "ToolHandleLocal"),
            EditorGUIUtility.TrTextContentWithIcon("Global", "Toggle Tool Handle Rotation\n\nTool handles are in global rotation.", "ToolHandleGlobal")
        };

        static readonly List<EditorTool> s_EditorToolModes = new List<EditorTool>(8);

        internal static Rect GetThinArea(Rect pos)
        {
            return new Rect(pos.x, 7, pos.width, 18);
        }

        internal static Rect GetThickArea(Rect pos)
        {
            return new Rect(pos.x, 5, pos.width, 24);
        }

        internal static void DoBuiltinToolSettings(Rect rect)
        {
            GUI.SetNextControlName("ToolbarToolPivotPositionButton");
            Tools.pivotMode = (PivotMode)EditorGUI.CycleButton(new Rect(rect.x, rect.y, rect.width / 2, rect.height), (int)Tools.pivotMode, s_PivotIcons, "ButtonLeft");
            if (Tools.current == Tool.Scale && Selection.transforms.Length < 2)
                GUI.enabled = false;
            GUI.SetNextControlName("ToolbarToolPivotOrientationButton");
            PivotRotation tempPivot = (PivotRotation)EditorGUI.CycleButton(new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, rect.height), (int)Tools.pivotRotation, s_PivotRotation, "ButtonRight");
            if (Tools.pivotRotation != tempPivot)
            {
                Tools.pivotRotation = tempPivot;
                if (tempPivot == PivotRotation.Global)
                    Tools.ResetGlobalHandleRotation();
            }

            if (Tools.current == Tool.Scale)
                GUI.enabled = true;

            if (GUI.changed)
                Tools.RepaintAllToolViews();
        }

        public static void DrawSceneViewTools(SceneView sceneView)
        {
            SceneViewOverlay.Window(Styles.selectionToolsWindowTitle, DoContextualToolbarOverlay, int.MaxValue,
                SceneViewOverlay.WindowDisplayOption.OneWindowPerTitle);
        }

        static void DoContextualToolbarOverlay(UnityEngine.Object target, SceneView sceneView)
        {
            GUILayout.BeginHorizontal(GUIStyle.none, GUILayout.MinWidth(210), GUILayout.Height(30));

            s_EditorToolModes.Clear();
            EditorToolContext.GetCustomEditorTools(s_EditorToolModes);

            if (s_EditorToolModes.Count > 0)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.EditorToolbar(s_EditorToolModes);
                if (EditorGUI.EndChangeCheck())
                    foreach (var editor in sceneView.GetActiveEditors())
                        editor.Repaint();
            }
            else
            {
                var fontStyle = EditorStyles.label.fontStyle;
                EditorStyles.label.fontStyle = FontStyle.Italic;
                GUILayout.Label(Styles.noToolsAvailable, EditorStyles.centeredGreyMiniLabel);
                EditorStyles.label.fontStyle = fontStyle;
            }
            GUILayout.EndHorizontal();
        }

        internal static void DoToolHistoryContextMenu()
        {
            var toolHistoryMenu = new GenericMenu()
            {
                allowDuplicateNames = true
            };

            s_ToolList.Clear();
            EditorToolContext.GetToolHistory(s_ToolList, true);

            // recent history
            if (EditorToolContext.GetLastTool() != null)
            {
                toolHistoryMenu.AddDisabledItem(Styles.recentTools);

                for (var i = 0; i < s_ToolList.Count; i++)
                {
                    var tool = s_ToolList[i];

                    if (EditorToolUtility.IsCustomEditorTool(tool.GetType()))
                        continue;

                    toolHistoryMenu.AddItem(
                        new GUIContent(EditorToolUtility.GetToolName(tool)),
                        false,
                        () => { EditorToolContext.activeTool = tool; });
                }

                toolHistoryMenu.AddSeparator("");
            }

            s_ToolList.Clear();
            EditorToolContext.GetCustomEditorTools(s_ToolList);

            if (s_ToolList.Any())
            {
                toolHistoryMenu.AddDisabledItem(Styles.selectionTools);

                for (var i = 0; i < s_ToolList.Count; i++)
                {
                    var tool = s_ToolList[i];

                    if (!EditorToolUtility.IsCustomEditorTool(tool.GetType()))
                        continue;

                    toolHistoryMenu.AddItem(
                        new GUIContent(EditorToolUtility.GetToolName(tool)),
                        false,
                        () => { EditorToolContext.activeTool = tool; });
                }

                toolHistoryMenu.AddSeparator("");
            }

            toolHistoryMenu.AddDisabledItem(Styles.availableTools);

            foreach (var toolType in EditorToolUtility.GetCustomEditorToolsForType(null))
            {
                toolHistoryMenu.AddItem(
                    new GUIContent(EditorToolUtility.GetToolName(toolType)),
                    false,
                    () => { EditorTools.EditorTools.SetActiveTool(toolType); });
            }

            toolHistoryMenu.ShowAsContext();
        }
    }
}
