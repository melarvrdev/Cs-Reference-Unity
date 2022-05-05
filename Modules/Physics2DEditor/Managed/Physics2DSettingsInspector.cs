// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    [CustomEditor(typeof(Physics2DSettings))]
    internal class Physics2DSettingsInspector : ProjectSettingsBaseEditor
    {
        private static class Content
        {
            public static readonly GUIContent kMultithreadingLabel = EditorGUIUtility.TrTextContent("Multithreading", "Allows the configuration of multi-threaded physics using the job system.");
            public static readonly GUIContent kGizmosLabel = EditorGUIUtility.TrTextContent("Gizmos", "Allows the configuration of 2D physics gizmos shown in the Editor.");
            public static readonly GUIContent kLayerCollisionMatrixLabel = EditorGUIUtility.TrTextContent("Layer Collision Matrix", "Allows the configuration of the layer-based collision detection.");
            public static readonly GUIContent kReuseCollisionCallbacksLabel = EditorGUIUtility.TrTextContent("This option boosts performance when ON. With it OFF it can result in poor performance due to GC pressure. For this reason, it defaults to being ON.");
            public static readonly GUIContent kAutoSyncTransformsLabel = EditorGUIUtility.TrTextContent("This option is for legacy support only. It can result in extremely poor performance when ON. For this reason, it defaults to being OFF.");

            public static readonly GUIContent kGeneralLabel = EditorGUIUtility.TrTextContent("General Settings", "General Settings");
            public static readonly GUIContent kCollisionLabel = EditorGUIUtility.TrTextContent("Layer Collision Matrix", "Collision Settings");
        }

        class Styles
        {
            public static readonly GUIStyle kSettingsFramebox = new GUIStyle(EditorStyles.frameBox) { padding = new RectOffset(1, 1, 1, 0) };
        }

        // These are to maintain UI selection.
        const string UniqueSettingsKey = "UnityEditor.U2D.Physics/";
        const string GeneralSettingsSelectedKey = UniqueSettingsKey + "GeneralSettingsSelected";


        SerializedProperty m_ReuseCollisionCallbacks;
        SerializedProperty m_AutoSyncTransforms;
        SerializedProperty m_Multithreading;
        SerializedProperty m_GizmoOptions;

        public void OnEnable()
        {
            m_ReuseCollisionCallbacks = serializedObject.FindProperty("m_ReuseCollisionCallbacks");
            m_AutoSyncTransforms = serializedObject.FindProperty("m_AutoSyncTransforms");
            m_Multithreading = serializedObject.FindProperty("m_JobOptions");
            m_GizmoOptions = serializedObject.FindProperty("m_GizmoOptions");
        }

        private bool generalSettingsSelected
        {
            get { return EditorPrefs.GetBool(GeneralSettingsSelectedKey, true); }
            set { EditorPrefs.SetBool(GeneralSettingsSelectedKey, value); }
        }

        static GUIStyle s_TabFirst;
        static GUIStyle s_TabLast;

        static Rect GetTabSelection(Rect rect, int tabIndex, out GUIStyle tabStyle)
        {
            if (s_TabFirst == null)
            {
                s_TabFirst = "Tab first";
                s_TabLast = "Tab last";
            }

            if (tabIndex == 0)
                tabStyle = s_TabFirst;
            else
                tabStyle = s_TabLast;

            var tabWidth = rect.width / 2;
            var left = Mathf.RoundToInt(tabIndex * tabWidth);
            var right = Mathf.RoundToInt((tabIndex + 1) * tabWidth);
            return new Rect(rect.x + left, rect.y, right - left, EditorGUI.kTabButtonHeight);
        }

        public override void OnInspectorGUI()
        {
            // Tabs.
            {
                // Select tabs.
                EditorGUI.BeginChangeCheck();
                var rect = EditorGUILayout.BeginVertical(Styles.kSettingsFramebox);

                // Draw General Settings Tab.
                GUIStyle buttonStyle = null;
                var buttonRect = GetTabSelection(rect, 0, out buttonStyle);
                if (GUI.Toggle(buttonRect, generalSettingsSelected, Content.kGeneralLabel, buttonStyle))
                    generalSettingsSelected = true;

                // Draw Collision Settings Tab.
                buttonRect = GetTabSelection(rect, 1, out buttonStyle);
                if (GUI.Toggle(buttonRect, !generalSettingsSelected, Content.kCollisionLabel, buttonStyle))
                    generalSettingsSelected = false;

                GUILayoutUtility.GetRect(10, EditorGUI.kTabButtonHeight);
                EditorGUI.EndChangeCheck();
            }

            // Draw tab selection.
            if (generalSettingsSelected)
            {
                // Update object.
                serializedObject.Update();

                // Draw standard property settings.
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                DrawPropertiesExcluding(serializedObject, "m_JobOptions", "m_ReuseCollisionCallbacks", "m_AutoSyncTransforms", "m_GizmoOptions");

                // Reuse Collision Callbacks.
                EditorGUILayout.PropertyField(m_ReuseCollisionCallbacks);
                if (!m_ReuseCollisionCallbacks.boolValue)
                {
                    EditorGUILayout.HelpBox(Content.kReuseCollisionCallbacksLabel.ToString(), MessageType.Warning, false);
                    EditorGUILayout.Space();
                }

                // Auto Sync Transforms.
                EditorGUILayout.PropertyField(m_AutoSyncTransforms);
                if (m_AutoSyncTransforms.boolValue)
                {
                    EditorGUILayout.HelpBox(Content.kAutoSyncTransformsLabel.ToString(), MessageType.Warning, false);
                    EditorGUILayout.Space();
                }

                // Draw the Gizmo options.
                Physics2D.GizmoOptions gizmoOptions = (Physics2D.GizmoOptions)m_GizmoOptions.intValue;
                gizmoOptions = (Physics2D.GizmoOptions)EditorGUILayout.EnumFlagsField(Content.kGizmosLabel, gizmoOptions);
                m_GizmoOptions.intValue = (int)gizmoOptions;

                // Multithreading.
                GUILayout.BeginHorizontal();
                GUILayout.Space(0);
                EditorGUILayout.PropertyField(m_Multithreading, Content.kMultithreadingLabel, true);
                GUILayout.EndHorizontal();

                // Padding.
                EditorGUILayout.Space();

                // Apply changes.
                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                // Layer Collision Matrix.
                LayerCollisionMatrixGUI2D.Draw(
                    Content.kLayerCollisionMatrixLabel,
                    (int layerA, int layerB) => { return !Physics2D.GetIgnoreLayerCollision(layerA, layerB); },
                    (int layerA, int layerB, bool val) => { Physics2D.IgnoreLayerCollision(layerA, layerB, !val); }
                    );
            }

            EditorGUILayout.EndVertical();
        }

        [SettingsProvider]
        internal static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = AssetSettingsProvider.CreateProviderFromAssetPath(
                "Project/Physics 2D", "ProjectSettings/Physics2DSettings.asset",
                SettingsProvider.GetSearchKeywordsFromPath("ProjectSettings/Physics2DSettings.asset"));
            return provider;
        }

        static bool GetValue(int layerA, int layerB) { return !Physics2D.GetIgnoreLayerCollision(layerA, layerB); }
        static void SetValue(int layerA, int layerB, bool val) { Physics2D.IgnoreLayerCollision(layerA, layerB, !val); }
    }
}
