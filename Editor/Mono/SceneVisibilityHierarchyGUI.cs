// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Experimental;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    internal static class SceneVisibilityHierarchyGUI
    {
        internal static class Styles
        {
            public class IconState
            {
                public GUIContent visibleAll;
                public GUIContent visibleMixed;
                public GUIContent hiddenAll;
                public GUIContent hiddenMixed;
            }

            public static readonly IconState iconNormal = new IconState
            {
                visibleAll = EditorGUIUtility.TrIconContent("scenevis_visible"),
                visibleMixed = EditorGUIUtility.TrIconContent("scenevis_visible-mixed"),
                hiddenAll = EditorGUIUtility.TrIconContent("scenevis_hidden"),
                hiddenMixed = EditorGUIUtility.TrIconContent("scenevis_hidden-mixed"),
            };

            public static readonly IconState iconHovered = new IconState
            {
                visibleAll = EditorGUIUtility.TrIconContent("scenevis_visible_hover"),
                visibleMixed = EditorGUIUtility.TrIconContent("scenevis_visible-mixed_hover"),
                hiddenAll = EditorGUIUtility.TrIconContent("scenevis_hidden_hover"),
                hiddenMixed = EditorGUIUtility.TrIconContent("scenevis_hidden-mixed_hover"),
            };

            public static readonly Color backgroundColor = EditorResources.GetStyle("game-object-tree-view-scene-visibility")
                .GetColor("background-color");

            public static readonly Color hoveredBackgroundColor = EditorResources.GetStyle("game-object-tree-view-scene-visibility")
                .GetColor("-unity-object-hovered-color");

            public static readonly Color selectedBackgroundColor = EditorResources.GetStyle("game-object-tree-view-scene-visibility")
                .GetColor("-unity-object-selected-color");

            public static readonly Color selectedNoFocusBackgroundColor = EditorResources.GetStyle("game-object-tree-view-scene-visibility")
                .GetColor("-unity-object-selected-no-focus-color");

            public static readonly GUIContent iconSceneHovered = EditorGUIUtility.TrIconContent("scenevis_scene_hover");

            public static readonly GUIStyle sceneVisibilityStyle = "SceneVisibility";

            public static Color GetItemBackgroundColor(bool isHovered, bool isSelected, bool isFocused)
            {
                if (isSelected)
                {
                    if (isFocused)
                        return selectedBackgroundColor;

                    return selectedNoFocusBackgroundColor;
                }

                if (isHovered)
                    return hoveredBackgroundColor;

                return backgroundColor;
            }
        }

        private const int k_VisibilityIconPadding = 4;
        private const int k_IconWidth = 16;

        public const float utilityBarWidth = k_VisibilityIconPadding * 2 + k_IconWidth;

        public static void DrawBackground(Rect rect)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            rect.width = utilityBarWidth;

            Color oldColor = GUI.color;
            GUI.color = Styles.backgroundColor;
            GUI.Label(rect, GUIContent.none, GameObjectTreeViewGUI.GameObjectStyles.hoveredItemBackgroundStyle);
            GUI.color = oldColor;
        }

        public static void DoItemGUI(Rect rect, GameObjectTreeViewItem goItem, bool isSelected, bool isHovered, bool isFocused)
        {
            Rect iconRect = rect;
            iconRect.xMin += k_VisibilityIconPadding;
            iconRect.width = k_IconWidth;

            bool isIconHovered = iconRect.Contains(Event.current.mousePosition);

            if (isHovered)
            {
                GUIView.current.MarkHotRegion(GUIClip.UnclipToWindow(iconRect));
            }

            DrawItemBackground(rect, isSelected, isHovered, isFocused);

            GameObject gameObject = goItem.objectPPTR as GameObject;
            if (gameObject)
            {
                DrawGameObjectItem(iconRect, gameObject, isHovered, isIconHovered);
            }
            else
            {
                Scene scene = goItem.scene;
                if (scene.IsValid())
                {
                    DrawSceneItem(iconRect, scene, isHovered, isIconHovered);
                }
            }
        }

        private static void DrawItemBackground(Rect rect, bool isSelected, bool isHovered, bool isFocused)
        {
            if (Event.current.type == EventType.Repaint)
            {
                rect.width = utilityBarWidth;

                if (isSelected || isHovered)
                {
                    using (new GUI.ColorScope(Styles.GetItemBackgroundColor(isHovered, isSelected, isFocused)))
                    {
                        GUI.Label(rect, GUIContent.none, GameObjectTreeViewGUI.GameObjectStyles.hoveredItemBackgroundStyle);
                    }
                }
            }
        }

        private static void DrawGameObjectItem(Rect rect, GameObject gameObject, bool isItemHovered, bool isIconHovered)
        {
            var isHidden = SceneVisibilityManager.IsGameObjectHidden(gameObject);
            bool hasHiddenChildren = !SceneVisibilityManager.AreAllChildrenVisible(gameObject);
            bool shouldDisplayIcon = isItemHovered || isHidden || hasHiddenChildren;
            Styles.IconState iconState = isIconHovered ? Styles.iconHovered : Styles.iconNormal;

            GUIContent icon;
            if (isHidden)
            {
                icon = SceneVisibilityManager.AreAllChildrenHidden(gameObject) ? iconState.hiddenAll : iconState.hiddenMixed;
            }
            else if (hasHiddenChildren)
            {
                icon = iconState.visibleMixed;
            }
            else
            {
                icon = iconState.visibleAll;
            }

            if (shouldDisplayIcon && GUI.Button(rect, icon, Styles.sceneVisibilityStyle))
            {
                if (Event.current.alt)
                    SceneVisibilityManager.ToggleHierarchyVisibility(gameObject);
                else
                    SceneVisibilityManager.ToggleGameObjectVisibility(gameObject);
            }
        }

        private static void DrawSceneItem(Rect rect, Scene scene, bool isItemHovered, bool isIconHovered)
        {
            var isHidden = SceneVisibilityManager.IsEntireSceneHidden(scene);
            bool shouldDisplayIcon = true;

            GUIContent icon;
            if (!isIconHovered)
            {
                if (isHidden)
                {
                    icon = Styles.iconNormal.hiddenAll;
                }
                else if (SceneVisibilityManager.HasHiddenGameObjects(scene))
                {
                    icon = Styles.iconNormal.hiddenMixed;
                }
                else
                {
                    icon = Styles.iconNormal.visibleAll;
                    shouldDisplayIcon = isItemHovered;
                }
            }
            else
            {
                icon = Styles.iconSceneHovered;
            }

            if (shouldDisplayIcon && GUI.Button(rect, icon, Styles.sceneVisibilityStyle))
            {
                if (isHidden)
                {
                    SceneVisibilityManager.ShowScene(scene);
                }
                else
                {
                    SceneVisibilityManager.HideScene(scene);
                }
            }
        }
    }
}
