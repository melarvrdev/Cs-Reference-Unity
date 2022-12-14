// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.AnimatedValues;
using System.Collections.Generic;

namespace UnityEditor
{
    [CustomEditor(typeof(Animator))]
    [CanEditMultipleObjects]
    internal class AnimatorInspector : Editor
    {
        SerializedProperty m_Avatar;
        SerializedProperty m_Controller;
        SerializedProperty m_ApplyRootMotion;
        SerializedProperty m_AnimatePhysics;
        SerializedProperty m_CullingMode;
        SerializedProperty m_UpdateMode;
        SerializedProperty m_WarningMessage;

        AnimBool m_ShowWarningMessage = new AnimBool();
        bool m_IsRootPositionOrRotationControlledByCurves;

        private bool IsWarningMessageEmpty { get { return m_WarningMessage != null && m_WarningMessage.stringValue.Length > 0; } }
        private string WarningMessage { get { return m_WarningMessage != null ? m_WarningMessage.stringValue : ""; } }

        class Styles
        {
            public GUIContent applyRootMotion = new GUIContent(EditorGUIUtility.TrTextContent("Apply Root Motion"));
            public GUIContent updateMode = new GUIContent(EditorGUIUtility.TrTextContent("Update Mode"));
            public GUIContent cullingMode = new GUIContent(EditorGUIUtility.TrTextContent("Culling Mode"));
            public GUIContent animatePhysics = new GUIContent(EditorGUIUtility.TrTextContent("Animate Physics"));

            public Styles()
            {
                applyRootMotion.tooltip = "Automatically move the object using the root motion from the animations";
                updateMode.tooltip = "Controls when and how often the Animator is updated";
                cullingMode.tooltip = "Controls what is updated when the object has been culled";
                animatePhysics.tooltip = "Notify physics system of animated transforms.";
            }
        }
        static Styles styles;

        private void Init()
        {
            if (styles == null)
            {
                styles = new Styles();
            }
            InitShowOptions();
        }

        private void InitShowOptions()
        {
            m_ShowWarningMessage.value = IsWarningMessageEmpty;

            m_ShowWarningMessage.valueChanged.AddListener(Repaint);
        }

        private void UpdateShowOptions()
        {
            m_ShowWarningMessage.target = IsWarningMessageEmpty;
        }

        void OnEnable()
        {
            m_Avatar = serializedObject.FindProperty("m_Avatar");
            m_ApplyRootMotion = serializedObject.FindProperty("m_ApplyRootMotion");
            m_AnimatePhysics = serializedObject.FindProperty("m_AnimatePhysics");
            m_Controller = serializedObject.FindProperty("m_Controller");
            m_CullingMode = serializedObject.FindProperty("m_CullingMode");
            m_UpdateMode = serializedObject.FindProperty("m_UpdateMode");
            m_WarningMessage = serializedObject.FindProperty("m_WarningMessage");


            Init();
        }

        public override void OnInspectorGUI()
        {
            bool isEditingMultipleObjects = targets.Length > 1;

            bool cullingModeChanged = false;
            bool updateModeChanged = false;

            Animator firstAnimator = target as Animator;

            serializedObject.UpdateIfRequiredOrScript();

            UpdateShowOptions();

            EditorGUI.BeginChangeCheck();
            //Collect the previous AnimatorControllers

            EditorGUILayout.ObjectField(m_Controller);


            var controller = m_Controller.objectReferenceValue as RuntimeAnimatorController;
            if (EditorGUI.EndChangeCheck())
            {
                var controllers = new List<RuntimeAnimatorController>();
                foreach (Animator animator in targets)
                {
                    controllers.Add(animator.runtimeAnimatorController);
                }
                serializedObject.ApplyModifiedProperties();
                AnimationWindowUtility.ControllerChanged();
            }

            EditorGUILayout.PropertyField(m_Avatar);
            if (firstAnimator.supportsOnAnimatorMove && !isEditingMultipleObjects)
            {
                EditorGUILayout.LabelField("Apply Root Motion", "Handled by Script");
            }
            else
            {
                EditorGUILayout.PropertyField(m_ApplyRootMotion, styles.applyRootMotion);

                // This might change between layout & repaint so we have local cached value to only update on layout
                if (Event.current.type == EventType.Layout)
                    m_IsRootPositionOrRotationControlledByCurves = firstAnimator.isRootPositionOrRotationControlledByCurves;

                if (!m_ApplyRootMotion.boolValue && m_IsRootPositionOrRotationControlledByCurves)
                {
                    EditorGUILayout.HelpBox("Root position or rotation are controlled by curves", MessageType.Info, true);
                }
            }

            EditorGUILayout.PropertyField(m_AnimatePhysics, styles.animatePhysics);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_UpdateMode, styles.updateMode);
            updateModeChanged =  EditorGUI.EndChangeCheck();


            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CullingMode, styles.cullingMode);
            cullingModeChanged =  EditorGUI.EndChangeCheck();


            if (!isEditingMultipleObjects)
                EditorGUILayout.HelpBox(firstAnimator.GetStats(), MessageType.Info, true);

            if (EditorGUILayout.BeginFadeGroup(m_ShowWarningMessage.faded))
            {
                EditorGUILayout.HelpBox(WarningMessage, MessageType.Warning, true);
            }
            EditorGUILayout.EndFadeGroup();


            serializedObject.ApplyModifiedProperties();

            foreach (Animator animator in targets)
            {
                if (cullingModeChanged)
                    animator.OnCullingModeChanged();

                if (updateModeChanged)
                    animator.OnUpdateModeChanged();
            }
        }
    }
}
