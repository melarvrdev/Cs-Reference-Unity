// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor
{
    class ColorModuleUI : ModuleUI
    {
        class Texts
        {
            public GUIContent color = EditorGUIUtility.TextContent("Color|Controls the color of each particle during its lifetime.");
        }
        static Texts s_Texts;
        SerializedMinMaxGradient m_Gradient;
        SerializedProperty m_Scale;

        public ColorModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "ColorModule", displayName)
        {
            m_ToolTip = "Controls the color of each particle during its lifetime.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_Gradient != null)
                return;
            m_Gradient = new SerializedMinMaxGradient(this);
            m_Gradient.m_AllowColor = false;
            m_Gradient.m_AllowRandomBetweenTwoColors = false;
        }

        public override void OnInspectorGUI(InitialModuleUI initial)
        {
            if (s_Texts == null)
                s_Texts = new Texts();

            GUIMinMaxGradient(s_Texts.color, m_Gradient, false);
        }
    }
} // namespace UnityEditor
