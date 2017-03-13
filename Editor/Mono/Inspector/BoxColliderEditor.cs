// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(BoxCollider))]
    [CanEditMultipleObjects]
    internal class BoxColliderEditor : PrimitiveCollider3DEditor
    {
        private static readonly int s_HandleControlIDHint = typeof(BoxColliderEditor).Name.GetHashCode();

        SerializedProperty m_Center;
        SerializedProperty m_Size;
        private readonly BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle(s_HandleControlIDHint);

        public override void OnEnable()
        {
            base.OnEnable();

            m_Center = serializedObject.FindProperty("m_Center");
            m_Size = serializedObject.FindProperty("m_Size");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            InspectorEditButtonGUI();
            EditorGUILayout.PropertyField(m_IsTrigger);
            EditorGUILayout.PropertyField(m_Material);
            EditorGUILayout.PropertyField(m_Center);
            EditorGUILayout.PropertyField(m_Size);

            serializedObject.ApplyModifiedProperties();
        }

        protected override PrimitiveBoundsHandle boundsHandle { get { return m_BoundsHandle; } }

        protected override void CopyColliderPropertiesToHandle()
        {
            BoxCollider collider = (BoxCollider)target;
            m_BoundsHandle.center = TransformColliderCenterToHandleSpace(collider.transform, collider.center);
            m_BoundsHandle.size = Vector3.Scale(collider.size, collider.transform.lossyScale);
        }

        protected override void CopyHandlePropertiesToCollider()
        {
            BoxCollider collider = (BoxCollider)target;
            collider.center = TransformHandleCenterToColliderSpace(collider.transform, m_BoundsHandle.center);
            Vector3 size = Vector3.Scale(m_BoundsHandle.size, InvertScaleVector(collider.transform.lossyScale));
            size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            collider.size = size;
        }
    }
}
