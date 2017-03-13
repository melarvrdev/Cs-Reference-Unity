// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class LayerMatrixGUI
    {
        public delegate bool GetValueFunc(int layerA, int layerB);
        public delegate void SetValueFunc(int layerA, int layerB, bool val);

        public static void DoGUI(string title, ref bool show, ref Vector2 scrollPos, GetValueFunc getValue, SetValueFunc setValue)
        {
            const int kMaxLayers = 32;
            const int checkboxSize = 16;
            const int labelSize = 100;
            const int indent = 30;

            int numLayers = 0;
            for (int i = 0; i < kMaxLayers; i++)
                if (LayerMask.LayerToName(i) != "")
                    numLayers++;

            GUILayout.BeginHorizontal();
            GUILayout.Space(0);
            show = EditorGUILayout.Foldout(show, title, true);
            GUILayout.EndHorizontal();
            if (show)
            {
                scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.MinHeight(labelSize + 20), GUILayout.MaxHeight(labelSize + (numLayers + 1) * checkboxSize));
                Rect topLabelRect = GUILayoutUtility.GetRect(checkboxSize * numLayers + labelSize, labelSize);
                Rect scrollArea = GUIClip.topmostRect;
                Vector2 topLeft = GUIClip.Unclip(new Vector2(topLabelRect.x, topLabelRect.y));
                int y = 0;
                for (int i = 0; i < kMaxLayers; i++)
                {
                    if (LayerMask.LayerToName(i) != "")
                    {
                        // Need to do some shifting around here, so the rotated text will still clip correctly
                        float clipOffset = (labelSize + indent + (numLayers - y) * checkboxSize) - (scrollArea.width + scrollPos.x);
                        if (clipOffset < 0)
                            clipOffset = 0;

                        Vector3 translate = new Vector3(labelSize + indent + checkboxSize * (numLayers - y) + topLeft.y + topLeft.x + scrollPos.y - clipOffset, topLeft.y + scrollPos.y, 0);
                        GUI.matrix = Matrix4x4.TRS(translate, Quaternion.identity, Vector3.one) * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, 90), Vector3.one);

                        if (SystemInfo.graphicsDeviceVersion.StartsWith("Direct3D 9.0"))
                            GUI.matrix *= Matrix4x4.TRS(new Vector3(-0.5f, -0.5f, 0), Quaternion.identity, Vector3.one);

                        GUI.Label(new Rect(2 - topLeft.x - scrollPos.y, scrollPos.y - clipOffset, labelSize, checkboxSize), LayerMask.LayerToName(i), "RightLabel");
                        y++;
                    }
                }
                GUI.matrix = Matrix4x4.identity;
                y = 0;
                for (int i = 0; i < kMaxLayers; i++)
                {
                    if (LayerMask.LayerToName(i) != "")
                    {
                        int x = 0;
                        Rect r = GUILayoutUtility.GetRect(indent + checkboxSize * numLayers + labelSize, checkboxSize);
                        GUI.Label(new Rect(r.x + indent, r.y, labelSize, checkboxSize), LayerMask.LayerToName(i), "RightLabel");
                        for (int j = kMaxLayers - 1; j >= 0; j--)
                        {
                            if (LayerMask.LayerToName(j) != "")
                            {
                                if (x < numLayers - y)
                                {
                                    GUIContent tooltip = new GUIContent("", LayerMask.LayerToName(i) + "/" + LayerMask.LayerToName(j));
                                    bool val = getValue(i, j);
                                    bool toggle = GUI.Toggle(new Rect(labelSize + indent + r.x + x * checkboxSize, r.y, checkboxSize, checkboxSize), val, tooltip);
                                    if (toggle != val)
                                        setValue(i, j, toggle);
                                }
                                x++;
                            }
                        }
                        y++;
                    }
                }
                GUILayout.EndScrollView();
            }
        }
    }

    [CustomEditor(typeof(PhysicsManager))]
    internal class PhysicsManagerInspector : Editor
    {
        Vector2 scrollPos;
        bool show = true;

        bool GetValue(int layerA, int layerB)
        {
            return !Physics.GetIgnoreLayerCollision(layerA, layerB);
        }

        void SetValue(int layerA, int layerB, bool val)
        {
            Physics.IgnoreLayerCollision(layerA, layerB, !val);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            LayerMatrixGUI.DoGUI("Layer Collision Matrix", ref show, ref scrollPos, GetValue, SetValue);
        }
    }
}
