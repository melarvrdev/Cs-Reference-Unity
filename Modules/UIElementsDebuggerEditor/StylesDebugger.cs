// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using Cursor = UnityEngine.UIElements.Cursor;

namespace UnityEditor.UIElements.Debugger
{
    internal class StylesDebugger : VisualElement
    {
        private DebuggerSelection m_DebuggerSelection;
        private ScrollView m_ScrollView;
        private BoxModelView m_BoxModelView;
        private IMGUIContainer m_IMGUIStylesDebugger;
        private StylePropertyDebugger m_StylePropertyDebugger;

        private IPanelDebug m_PanelDebug;
        private VisualElement selectedElement
        {
            get
            {
                return m_SelectedElement;
            }
            set
            {
                if (m_SelectedElement == value)
                    return;

                m_SelectedElement = value;

                m_BoxModelView.selectedElement = m_SelectedElement;
                m_StylePropertyDebugger.selectedElement = m_SelectedElement;
                m_SelectedElementUxml = null;
                m_ClassList = null;

                m_IMGUIStylesDebugger.IncrementVersion(VersionChangeType.Layout);
                GetElementMatchers();
            }
        }

        public StylesDebugger(DebuggerSelection debuggerSelection)
        {
            m_DebuggerSelection = debuggerSelection;
            m_DebuggerSelection.onPanelDebugChanged += pdbg => m_PanelDebug = pdbg;
            m_DebuggerSelection.onSelectedElementChanged += element => selectedElement = element;

            m_PanelDebug = m_DebuggerSelection.panelDebug;
            selectedElement = m_DebuggerSelection.element;

            m_ScrollView = new ScrollView();

            m_BoxModelView = new BoxModelView();
            m_ScrollView.Add(m_BoxModelView);

            m_IMGUIStylesDebugger = new IMGUIContainer(OnGUI);
            m_ScrollView.Add(m_IMGUIStylesDebugger);

            m_StylePropertyDebugger = new StylePropertyDebugger(selectedElement);
            m_ScrollView.Add(m_StylePropertyDebugger);

            Add(m_ScrollView);
        }

        // ---

        private HashSet<int> m_CurFoldout = new HashSet<int>();

        private string m_DumpId = "dump";
        private bool m_UxmlDumpExpanded;
        private bool m_UxmlDumpStyleFields;
        private bool m_NewLineOnAttributes;
        private bool m_AutoNameElements;

        private VisualElement m_SelectedElement;

        public void OnGUI()
        {
            if (m_PanelDebug == null || selectedElement == null)
                return;

            DrawSelection();
        }

        public void Refresh()
        {
            m_BoxModelView.Refresh();
            m_StylePropertyDebugger.Refresh();
        }

        private void DrawSelection()
        {
            if (m_SelectedElement == null)
                return;

            DrawUxmlDump(m_SelectedElement);
            DrawMatchingRules();
            DrawProperties();
        }

        private void DrawUxmlDump(VisualElement selectedElement)
        {
            m_UxmlDumpExpanded = EditorGUILayout.Foldout(m_UxmlDumpExpanded, Styles.uxmlContent);
            if (m_UxmlDumpExpanded)
            {
                EditorGUI.BeginChangeCheck();
                m_DumpId = EditorGUILayout.TextField("Template id", m_DumpId);
                m_UxmlDumpStyleFields = EditorGUILayout.Toggle("Include style fields", m_UxmlDumpStyleFields);
                m_NewLineOnAttributes = EditorGUILayout.Toggle("Line breaks on attributes", m_NewLineOnAttributes);
                m_AutoNameElements = EditorGUILayout.Toggle("Auto name elements", m_AutoNameElements);
                if (EditorGUI.EndChangeCheck())
                {
                    m_SelectedElementUxml = null;
                }
                if (m_SelectedElementUxml == null)
                {
                    UxmlExporter.ExportOptions options = UxmlExporter.ExportOptions.None;

                    if (m_UxmlDumpStyleFields)
                        options = UxmlExporter.ExportOptions.StyleFields;

                    if (m_NewLineOnAttributes)
                        options |= UxmlExporter.ExportOptions.NewLineOnAttributes;
                    if (m_AutoNameElements)
                        options |= UxmlExporter.ExportOptions.AutoNameElements;

                    m_SelectedElementUxml = UxmlExporter.Dump(selectedElement, m_DumpId ?? "template", options);
                }
                EditorGUILayout.TextArea(m_SelectedElementUxml);
            }
        }

        private MatchedRulesExtractor m_MatchedRulesExtractor = new MatchedRulesExtractor();
        private string m_SelectedElementUxml;
        private ReorderableList m_ClassList;
        private string m_NewClass;

        private void GetElementMatchers()
        {
            if (m_SelectedElement == null || m_SelectedElement.elementPanel == null)
                return;
            m_MatchedRulesExtractor.selectedElementRules.Clear();
            m_MatchedRulesExtractor.selectedElementStylesheets.Clear();
            m_MatchedRulesExtractor.FindMatchingRules(m_SelectedElement);
        }

        private void DrawProperties()
        {
            EditorGUILayout.LabelField(Styles.elementStylesContent, Styles.KInspectorTitle);

            m_SelectedElement.name = EditorGUILayout.TextField("Name", m_SelectedElement.name);
            EditorGUILayout.LabelField("Debug Id", m_SelectedElement.controlid.ToString());
            var textElement = m_SelectedElement as ITextElement;
            if (textElement != null)
            {
                textElement.text = EditorGUILayout.TextField("Text", textElement.text);
            }

            bool cacheContents = EditorGUILayout.Toggle("Cache Contents", m_SelectedElement.cacheAsBitmap);
            m_SelectedElement.cacheAsBitmap = cacheContents;
            if (m_SelectedElement.cacheAsBitmap && m_SelectedElement.computedStyle.overflow.value == Overflow.Visible)
            {
                EditorGUILayout.HelpBox("Bitmap caching will be ignored for this element because it's not clipped", MessageType.Warning);
            }

            m_SelectedElement.pickingMode = (PickingMode)EditorGUILayout.EnumPopup("Picking Mode", m_SelectedElement.pickingMode);

            if (m_SelectedElement.pseudoStates != 0)
            {
                EditorGUILayout.LabelField("Pseudo States", m_SelectedElement.pseudoStates.ToString());
            }
            else
            {
                EditorGUILayout.LabelField("Pseudo States", "None");
            }

            EditorGUILayout.LabelField("Focusable", m_SelectedElement.focusable.ToString());

            EditorGUILayout.LabelField("Layout", m_SelectedElement.layout.ToString());
            EditorGUILayout.LabelField("World Bound", m_SelectedElement.worldBound.ToString());
            EditorGUILayout.LabelField("World Clip", m_SelectedElement.worldClip.ToString());
            EditorGUILayout.LabelField("Bounding Box", m_SelectedElement.boundingBox.ToString());

            if (m_ClassList == null)
                InitClassList();
            m_ClassList.DoLayoutList();
        }

        private void InitClassList()
        {
            Action refresh = () => m_ClassList.list = m_SelectedElement.GetClasses().ToList();
            m_ClassList = new ReorderableList(m_SelectedElement.GetClasses().ToList(), typeof(string), false, true, true, true);
            m_ClassList.onRemoveCallback = _ =>
            {
                m_SelectedElement.RemoveFromClassList((string)m_ClassList.list[m_ClassList.index]);
                refresh();
            };
            m_ClassList.drawHeaderCallback = r =>
            {
                r.width /= 2;
                EditorGUI.LabelField(r, "Classes");
                r.x += r.width;
                m_NewClass = EditorGUI.TextField(r, m_NewClass);
            };
            m_ClassList.onCanAddCallback = _ => !String.IsNullOrEmpty(m_NewClass) && !m_SelectedElement.ClassListContains(m_NewClass);
            m_ClassList.onAddCallback = _ =>
            {
                m_SelectedElement.AddToClassList(m_NewClass);
                m_NewClass = "";
                refresh();
            };
        }

        private void DrawMatchingRules()
        {
            if (m_MatchedRulesExtractor.selectedElementStylesheets != null && m_MatchedRulesExtractor.selectedElementStylesheets.Count > 0)
            {
                EditorGUILayout.LabelField(Styles.stylesheetsContent, Styles.KInspectorTitle);
                foreach (string sheet in m_MatchedRulesExtractor.selectedElementStylesheets)
                {
                    if (GUILayout.Button(sheet) && CanOpenStyleSheet(sheet))
                        InternalEditorUtility.OpenFileAtLineExternal(sheet, 0, 0);
                }
            }

            if (m_MatchedRulesExtractor.selectedElementRules != null && m_MatchedRulesExtractor.selectedElementRules.Count > 0)
            {
                EditorGUILayout.LabelField(Styles.selectorsContent, Styles.KInspectorTitle);
                int i = 0;
                foreach (MatchedRulesExtractor.MatchedRule rule in m_MatchedRulesExtractor.selectedElementRules)
                {
                    StringBuilder builder = new StringBuilder();
                    for (int j = 0; j < rule.matchRecord.complexSelector.selectors.Length; j++)
                    {
                        StyleSelector sel = rule.matchRecord.complexSelector.selectors[j];
                        switch (sel.previousRelationship)
                        {
                            case StyleSelectorRelationship.Child:
                                builder.Append(" > ");
                                break;
                            case StyleSelectorRelationship.Descendent:
                                builder.Append(" ");
                                break;
                        }
                        for (int k = 0; k < sel.parts.Length; k++)
                        {
                            StyleSelectorPart part = sel.parts[k];
                            switch (part.type)
                            {
                                case StyleSelectorType.Class:
                                    builder.Append(".");
                                    break;
                                case StyleSelectorType.ID:
                                    builder.Append("#");
                                    break;
                                case StyleSelectorType.PseudoClass:
                                case StyleSelectorType.RecursivePseudoClass:
                                    builder.Append(":");
                                    break;
                                case StyleSelectorType.Wildcard:
                                    break;
                            }
                            builder.Append(part.value);
                        }
                    }

                    StyleProperty[] props = rule.matchRecord.complexSelector.rule.properties;
                    bool expanded = m_CurFoldout.Contains(i);
                    EditorGUILayout.BeginHorizontal();
                    bool foldout = EditorGUILayout.Foldout(m_CurFoldout.Contains(i), new GUIContent(builder.ToString()), true);
                    if (rule.displayPath != null && GUILayout.Button(rule.displayPath, EditorStyles.miniButton, GUILayout.MaxWidth(150)) && CanOpenStyleSheet(rule.fullPath))
                        InternalEditorUtility.OpenFileAtLineExternal(rule.fullPath, rule.lineNumber, -1);
                    EditorGUILayout.EndHorizontal();

                    if (expanded && !foldout)
                        m_CurFoldout.Remove(i);
                    else if (!expanded && foldout)
                        m_CurFoldout.Add(i);

                    if (foldout)
                    {
                        EditorGUI.indentLevel++;
                        for (int j = 0; j < props.Length; j++)
                        {
                            string s = "";
                            for (int k = 0; k < props[j].values.Length; k++)
                            {
                                if (k > 0)
                                    s += " ";

                                s += rule.matchRecord.sheet.ReadAsString(props[j].values[k]);
                            }

                            s = s.ToLower();
                            EditorGUILayout.LabelField(new GUIContent(props[j].name), new GUIContent(s));
                        }

                        EditorGUI.indentLevel--;
                    }
                    i++;
                }
            }
        }

        static bool CanOpenStyleSheet(string path) => File.Exists(path);

        internal static class Styles
        {
            public static GUIStyle KInspectorTitle = "WhiteLargeCenterLabel";

            public static readonly GUIContent elementStylesContent = EditorGUIUtility.TrTextContent("Element styles");
            public static readonly GUIContent uxmlContent = EditorGUIUtility.TrTextContent("UXML Dump");
            public static readonly GUIContent stylesheetsContent = EditorGUIUtility.TrTextContent("Stylesheets");
            public static readonly GUIContent selectorsContent = EditorGUIUtility.TrTextContent("Matching Selectors");
        }
    }

    internal class MatchedRulesExtractor
    {
        internal HashSet<MatchedRule> selectedElementRules = new HashSet<MatchedRule>(MatchedRule.lineNumberFullPathComparer);
        internal HashSet<string> selectedElementStylesheets = new HashSet<string>();

        internal struct MatchedRule
        {
            public readonly SelectorMatchRecord matchRecord;
            public readonly string displayPath;
            public readonly int lineNumber;
            public readonly string fullPath;

            public MatchedRule(SelectorMatchRecord matchRecord)
                : this()
            {
                this.matchRecord = matchRecord;
                fullPath = AssetDatabase.GetAssetPath(matchRecord.sheet);
                lineNumber = matchRecord.complexSelector.rule.line;
                if (string.IsNullOrEmpty(fullPath))
                {
                    displayPath = matchRecord.sheet.name + ":" + lineNumber;
                }
                else
                {
                    if (fullPath == "Library/unity editor resources")
                        displayPath = matchRecord.sheet.name + ":" + lineNumber;
                    else
                        displayPath = Path.GetFileName(fullPath) + ":" + lineNumber;
                }
            }

            private sealed class LineNumberFullPathEqualityComparer : IEqualityComparer<MatchedRule>
            {
                public bool Equals(MatchedRule x, MatchedRule y)
                {
                    return x.lineNumber == y.lineNumber && string.Equals(x.fullPath, y.fullPath);
                }

                public int GetHashCode(MatchedRule obj)
                {
                    unchecked
                    {
                        return (obj.lineNumber * 397) ^ (obj.fullPath != null ? obj.fullPath.GetHashCode() : 0);
                    }
                }
            }

            public static IEqualityComparer<MatchedRule> lineNumberFullPathComparer = new LineNumberFullPathEqualityComparer();
        }

        private void FindStyleSheets(VisualElement cursor, StyleMatchingContext matchingContext)
        {
            if (cursor.hierarchy.parent != null)
                FindStyleSheets(cursor.hierarchy.parent, matchingContext);

            if (cursor.styleSheetList == null)
                return;

            foreach (StyleSheet sheet in cursor.styleSheetList)
            {
                string name = AssetDatabase.GetAssetPath(sheet);
                if (string.IsNullOrEmpty(name) || sheet.isUnityStyleSheet)
                    name = sheet.name;

                selectedElementStylesheets.Add(name);
                matchingContext.styleSheetStack.Add(sheet);
            }
        }

        public void FindMatchingRules(VisualElement target)
        {
            var matchingContext = new StyleMatchingContext((element, info) => {}) { currentElement = target };
            FindStyleSheets(target, matchingContext);

            List<SelectorMatchRecord> matches = new List<SelectorMatchRecord>();
            StyleSelectorHelper.FindMatches(matchingContext, matches);

            matches.Sort(SelectorMatchRecord.Compare);

            foreach (var record in matches)
            {
                selectedElementRules.Add(new MatchedRule(record));
            }
        }
    }
}
