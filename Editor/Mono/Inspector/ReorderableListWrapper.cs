// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    // Provides a default appearance for a generic reorderable list that is typically used in inspector to draw arrays
    internal class ReorderableListWrapper
    {
        public static class Constants
        {
            public const float kHeaderPadding = 3f;
            public const float kArraySizeWidth = 48f;
            public const float kDefaultFoldoutHeaderHeight = 18;
        }

        internal ReorderableList m_ReorderableList;
        float m_HeaderHeight;
        bool m_Reorderable = false;
        bool m_IsNotInPrefabContextModeWithOverrides = false;

        SerializedProperty m_OriginalProperty;
        SerializedProperty m_ArraySize;

        internal static Rect s_ToolTipRect;

        int m_LastArraySize = -1;
        internal SerializedProperty Property
        {
            get
            {
                return m_OriginalProperty;
            }
            set
            {
                m_OriginalProperty = value;
                m_ArraySize = m_OriginalProperty.FindPropertyRelative("Array.size");

                if (m_ReorderableList != null)
                {
                    bool versionChanged = !SerializedProperty.VersionEquals(m_ReorderableList.serializedProperty, m_OriginalProperty);

                    m_ReorderableList.serializedProperty = m_OriginalProperty;

                    if (versionChanged || m_ArraySize != null && m_LastArraySize != m_ArraySize.intValue)
                    {
                        m_ReorderableList.ClearCacheRecursive();
                        ReorderableList.InvalidateParentCaches(m_ReorderableList.serializedProperty.propertyPath);

                        if (m_ArraySize != null) m_LastArraySize = m_ArraySize.intValue;
                    }
                }
            }
        }

        public static string GetPropertyIdentifier(SerializedProperty serializedProperty)
        {
            return serializedProperty.propertyPath + (GUIView.current?.nativeHandle.ToInt32() ?? -1);
        }

        ReorderableListWrapper() {}

        public ReorderableListWrapper(SerializedProperty property, GUIContent label, bool reorderable = true)
        {
            Property = property;
            m_HeaderHeight = Constants.kDefaultFoldoutHeaderHeight;
            Init(reorderable);
        }

        void Init(bool reorderable)
        {
            m_Reorderable = reorderable;
            SerializedProperty childProperty = Property.Copy();
            childProperty.Next(true);

            m_ReorderableList = new ReorderableList(Property.serializedObject, Property.Copy(), m_Reorderable, false, true, true);
            m_ReorderableList.headerHeight = ReorderableList.Defaults.minHeaderHeight;
            m_ReorderableList.m_IsEditable = true;
            m_ReorderableList.multiSelect = true;
            // Check to see if the list has any elements, and use one to find out if serialized property type has children
            m_ReorderableList.m_HasPropertyDrawer = (childProperty != null) ? childProperty.hasChildren : false;

            m_ReorderableList.onCanAddCallback += (list) =>
            {
                return m_IsNotInPrefabContextModeWithOverrides;
            };

            m_ReorderableList.onCanRemoveCallback += (list) =>
            {
                return m_IsNotInPrefabContextModeWithOverrides;
            };
        }

        internal void ClearCache()
        {
            m_ReorderableList.ClearCache();
        }

        public float GetHeight()
        {
            return m_HeaderHeight + (Property.isExpanded && m_ReorderableList != null ? Constants.kHeaderPadding + m_ReorderableList.GetHeight() : 0.0f);
        }

        public void Draw(GUIContent label, Rect r, Rect visibleArea, string tooltip, bool includeChildren)
        {
            r.xMin += EditorGUI.indent;
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            m_IsNotInPrefabContextModeWithOverrides = prefabStage == null || prefabStage.mode != PrefabStage.Mode.InContext || !PrefabStage.s_PatchAllOverriddenProperties
                || Selection.objects.All(obj => PrefabUtility.IsPartOfAnyPrefab(obj) && !AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj)).Equals(AssetDatabase.AssetPathToGUID(prefabStage.assetPath)));
            m_ReorderableList.draggable = m_Reorderable && m_IsNotInPrefabContextModeWithOverrides;

            Rect headerRect = new Rect(r.x, r.y, r.width, m_HeaderHeight);
            Rect sizeRect = new Rect(headerRect.xMax - Constants.kArraySizeWidth - EditorGUI.indent * EditorGUI.indentLevel, headerRect.y,
                Constants.kArraySizeWidth + EditorGUI.indent * EditorGUI.indentLevel, m_HeaderHeight);

            Event evt = Event.current;
            EventType prevType = evt.type;
            if (!string.IsNullOrEmpty(tooltip) && prevType == EventType.Repaint)
            {
                bool hovered = headerRect.Contains(evt.mousePosition);

                if (hovered && GUIClip.visibleRect.Contains(evt.mousePosition))
                {
                    if (!GUIStyle.IsTooltipActive(tooltip))
                        s_ToolTipRect = new Rect(evt.mousePosition, Vector2.zero);
                    GUIStyle.SetMouseTooltip(tooltip, s_ToolTipRect);
                }
            }
            if (Event.current.type == EventType.MouseUp && sizeRect.Contains(Event.current.mousePosition))
            {
                Event.current.type = EventType.Used;
            }

            EditorGUI.BeginChangeCheck();
            if (!m_OriginalProperty.hasMultipleDifferentValues) EditorGUI.BeginProperty(headerRect, GUIContent.none, m_OriginalProperty);

            bool prevEnabled = GUI.enabled;
            GUI.enabled = true;
            Property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(headerRect, Property.isExpanded, label ?? GUIContent.Temp(Property.displayName));
            EditorGUI.EndFoldoutHeaderGroup();
            GUI.enabled = prevEnabled;

            if (!m_OriginalProperty.hasMultipleDifferentValues) EditorGUI.EndProperty();

            if (EditorGUI.EndChangeCheck())
            {
                if (Event.current.alt)
                {
                    EditorGUI.SetExpandedRecurse(Property, Property.isExpanded);
                }

                m_ReorderableList.ClearCacheRecursive();
            }

            DrawChildren(r, headerRect, sizeRect, visibleArea, prevType);
        }

        void DrawChildren(Rect listRect, Rect headerRect, Rect sizeRect, Rect visibleRect, EventType previousEvent)
        {
            if (Event.current.type == EventType.Used && sizeRect.Contains(Event.current.mousePosition)) Event.current.type = previousEvent;

            EditorGUI.DefaultPropertyField(sizeRect, m_ArraySize, GUIContent.none);
            EditorGUI.LabelField(sizeRect, new GUIContent("", "Array Size"));

            if (headerRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform)
                {
                    Object[] objReferences = DragAndDrop.objectReferences;
                    foreach (var o in objReferences)
                    {
                        Object validatedObject = EditorGUI.ValidateObjectFieldAssignment(new[] { o }, typeof(Object), m_ReorderableList.serializedProperty, EditorGUI.ObjectFieldValidatorOptions.None);
                        if (validatedObject != null)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                        }
                        else continue;

                        if (Event.current.type == EventType.DragPerform) ReorderableList.defaultBehaviours.DoAddButton(m_ReorderableList, validatedObject);
                    }
                    DragAndDrop.AcceptDrag();
                    Event.current.Use();
                }
            }

            if (Event.current.type == EventType.DragExited)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.None;
                Event.current.Use();
            }

            if (Property.isExpanded)
            {
                listRect.y += m_HeaderHeight + Constants.kHeaderPadding;
                listRect.height -= m_HeaderHeight + Constants.kHeaderPadding;

                visibleRect.y -= listRect.y;
                m_ReorderableList.DoList(listRect, visibleRect);
            }
        }
    }
}
