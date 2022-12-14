// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(AudioLowPassFilter))]
    [CanEditMultipleObjects]
    internal class AudioLowPassFilterInspector : Editor
    {
        SerializedProperty m_LowpassResonanceQ;
        SerializedProperty m_LowpassLevelCustomCurve;

        private static class Styles
        {
            public static readonly GUIContent LowpassResonanceQTooltip = EditorGUIUtility.TrTextContent("Lowpass Resonance Q", "Determines how much the filter's self-resonance is dampened");
            public static readonly GUIContent LowpassLevelCustomCurveTooltip = EditorGUIUtility.TrTextContent("Cutoff Frequency", "Lowpass cutoff frequency in Hz");
        }

        void OnEnable()
        {
            m_LowpassResonanceQ = serializedObject.FindProperty("m_LowpassResonanceQ");
            m_LowpassLevelCustomCurve = serializedObject.FindProperty("lowpassLevelCustomCurve");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            AudioSourceInspector.AnimProp(
                Styles.LowpassLevelCustomCurveTooltip,
                m_LowpassLevelCustomCurve,
                10.0f, AudioSourceInspector.kMaxCutoffFrequency, true);

            EditorGUILayout.PropertyField(m_LowpassResonanceQ, Styles.LowpassResonanceQTooltip);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
