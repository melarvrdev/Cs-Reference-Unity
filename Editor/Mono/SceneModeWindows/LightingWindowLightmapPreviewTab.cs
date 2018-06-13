// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;
using UnityEngineInternal;
using Object = UnityEngine.Object;


namespace UnityEditor
{
    internal class LightingWindowLightmapPreviewTab
    {
        LightmapType m_LightmapType         = LightmapType.NoLightmap;
        Vector2 m_ScrollPosition            = Vector2.zero;
        bool m_ShouldScrollToLightmapIndex  = false;

        int m_ActiveGameObjectLightmapIndex     = -1; // the object the user selects in the scene
        int m_SelectedLightmapIndex             = 0;

        // realtime lightmaps
        Hash128 m_ActiveGameObjectTextureHash   = new Hash128(); // the object the user selects in the scene

        SerializedObject m_LightmapSettings;
        SerializedProperty m_LightingDataAsset;
        SerializedProperty m_TextureCompression;

        class Styles
        {
            public static readonly GUIStyle SelectedLightmapHighlight = "LightmapEditorSelectedHighlight";
            public static readonly GUIContent LightingDataAsset = EditorGUIUtility.TrTextContent("Lighting Data Asset", "A different LightingData.asset can be assigned here. These assets are generated by baking a scene in the OnDemand mode.");
        }

        public LightingWindowLightmapPreviewTab(LightmapType type)
        {
            m_LightmapType = type;

            InitSettings();
        }

        private bool isRealtimeLightmap
        {
            get
            {
                return m_LightmapType == LightmapType.DynamicLightmap;
            }
        }

        private bool showDebugInfo
        {
            get
            {
                return !isRealtimeLightmap && Unsupported.IsDeveloperMode() && LightmapEditorSettings.lightmapper == LightmapEditorSettings.Lightmapper.ProgressiveCPU;
            }
        }

        public void UpdateActiveGameObjectSelection()
        {
            MeshRenderer renderer;
            Terrain terrain = null;

            // if the active object in the selection is a renderer or a terrain, we're interested in it's lightmapIndex
            if (Selection.activeGameObject == null ||
                ((renderer = Selection.activeGameObject.GetComponent<MeshRenderer>()) == null &&
                 (terrain = Selection.activeGameObject.GetComponent<Terrain>()) == null))
            {
                m_ActiveGameObjectLightmapIndex = -1;
                m_ActiveGameObjectTextureHash = new Hash128();
                return;
            }
            if (isRealtimeLightmap)
            {
                Hash128 inputSystemHash;
                if ((renderer != null && LightmapEditorSettings.GetInputSystemHash(renderer.GetInstanceID(), out inputSystemHash))
                    || (terrain != null && LightmapEditorSettings.GetInputSystemHash(terrain.GetInstanceID(), out inputSystemHash)))
                {
                    m_ActiveGameObjectTextureHash = inputSystemHash;
                }
                else
                    m_ActiveGameObjectTextureHash = new Hash128();
            }
            else
                m_ActiveGameObjectLightmapIndex = renderer != null ? renderer.lightmapIndex : terrain.lightmapIndex;

            m_ShouldScrollToLightmapIndex = true;
        }

        public void OnGUI(Rect position)
        {
            InitSettings();

            m_LightmapSettings.Update();

            LightmapData[] lightmaps = LightmapSettings.lightmaps;
            VisualisationGITexture[] realtimeLightmaps = LightmapVisualizationUtility.GetRealtimeGITextures(GITextureType.Irradiance);

            LightmapListGUI(lightmaps, realtimeLightmaps);
        }

        private void InitSettings()
        {
            if (m_LightmapSettings == null || m_LightmapSettings.targetObject == null || m_LightmapSettings.targetObject != LightmapEditorSettings.GetLightmapSettings())
            {
                m_LightmapSettings = new SerializedObject(LightmapEditorSettings.GetLightmapSettings());
                m_LightingDataAsset = m_LightmapSettings.FindProperty("m_LightingDataAsset");
                m_TextureCompression = m_LightmapSettings.FindProperty("m_LightmapEditorSettings.m_TextureCompression");
            }
        }

        private void LightmapListGUI(LightmapData[] lightmaps, VisualisationGITexture[] realtimeLightmaps)
        {
            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);

            if (Lightmapping.giWorkflowMode == Lightmapping.GIWorkflowMode.OnDemand && !isRealtimeLightmap)
            {
                EditorGUILayout.PropertyField(m_LightingDataAsset, Styles.LightingDataAsset);
                m_LightmapSettings.ApplyModifiedProperties();
                GUILayout.Space(10);
            }
            else
            {
                GUILayout.Space(30);
            }

            DebugInfoSection(lightmaps);

            int length = isRealtimeLightmap ? realtimeLightmaps.Length : lightmaps.Length;

            if (m_SelectedLightmapIndex >= length)
                m_SelectedLightmapIndex = 0;

            for (int i = 0; i < length; i++)
            {
                Texture2D texture = isRealtimeLightmap ? realtimeLightmaps[i].texture : lightmaps[i].lightmapColor;

                if (texture != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);

                    Hash128 hash = isRealtimeLightmap ? realtimeLightmaps[i].hash : new Hash128();

                    LightmapField(texture, i, hash);

                    GUILayout.Space(5);
                    GUILayout.BeginVertical();
                    GUILayout.Label("Index: " + i, EditorStyles.miniBoldLabel);
                    GUILayout.Label("Size: " + texture.width + "x" + texture.height, EditorStyles.miniBoldLabel);
                    GUILayout.Label("Format: " + texture.format, EditorStyles.miniBoldLabel);

                    if (!isRealtimeLightmap)
                        GUILayout.Label("Compressed: " + (m_TextureCompression.boolValue ? "Compressed" : "None"), EditorStyles.miniBoldLabel);

                    GUILayout.EndVertical();
                    LightmapDebugInfo(i);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.Space(5);
                }
            }

            GUILayout.EndScrollView();
        }

        private void LightmapField(Texture2D lightmap, int index, Hash128 hash)
        {
            Rect rect = GUILayoutUtility.GetRect(100, 100, EditorStyles.objectField);

            if (EditorGUI.Toggle(rect, index == m_SelectedLightmapIndex, EditorStyles.objectFieldThumb))
            {
                m_SelectedLightmapIndex = index;

                if (rect.Contains(Event.current.mousePosition))
                {
                    Object actualTargetObject = lightmap;
                    Component com = actualTargetObject as Component;
                    if (com)
                        actualTargetObject = com.gameObject;

                    if (Event.current.clickCount == 1)
                        EditorGUI.PingObjectOrShowPreviewOnClick(actualTargetObject, GUILayoutUtility.GetLastRect());

                    if (Event.current.clickCount == 2)
                    {
                        if (isRealtimeLightmap)
                            LightmapPreviewWindow.CreateLightmapPreviewWindow(m_SelectedLightmapIndex, true, true);
                        else
                            LightmapPreviewWindow.CreateLightmapPreviewWindow(m_SelectedLightmapIndex, false, true);
                    }
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                rect = EditorStyles.objectFieldThumb.padding.Remove(rect);
                EditorGUI.DrawPreviewTexture(rect, lightmap);

                if ((!isRealtimeLightmap && index == m_ActiveGameObjectLightmapIndex) || (isRealtimeLightmap && hash == m_ActiveGameObjectTextureHash))
                {
                    Styles.SelectedLightmapHighlight.Draw(rect, false, false, false, false);

                    if (m_ShouldScrollToLightmapIndex)
                    {
                        GUI.ScrollTo(rect);

                        m_ShouldScrollToLightmapIndex = false;
                        GUIView.current.Repaint();
                    }
                }
            }
        }

        //***** DEBUG DATA *****//

        const string kEditorPrefsTransmissionTextures = "LightingWindowGlobalMapsTT";
        const string kEditorPrefsGeometryData = "LightingWindowGlobalMapsGD";
        const string kEditorPrefsInFlight = "LightingWindowGlobalMapsIF";
        const string kEditorPrefsMaterialTextures = "LightingWindowGlobalMapsMT";

        private string SizeString(float size)
        {
            return size.ToString("0.0") + " MB";
        }

        private float SumSizes(float[] sizes)
        {
            float sum = 0.0f;
            foreach (var size in sizes)
                sum += size;

            return sum;
        }

        private System.UInt64 SumCounts(System.UInt64[] counts)
        {
            System.UInt64 sum = 0;
            foreach (var count in counts)
                sum += count;

            return sum;
        }

        enum Precision { Tenths, Hundredths }

        private void DebugInfoSection(LightmapData[] lightmaps)
        {
            if (!showDebugInfo)
                return;

            Lightmapping.ResetExplicitlyShownMemLabels();
            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 400f;

            System.UInt64[] dummyCounts = new System.UInt64[0];
            {
                MemLabels labels = Lightmapping.GetTransmissionTexturesMemLabels();
                ShowObjectNamesSizesAndCounts("Transmission textures", kEditorPrefsTransmissionTextures, labels.labels, labels.sizes, dummyCounts, Precision.Tenths);
            }

            {
                MemLabels labels = Lightmapping.GetMaterialTexturesMemLabels();
                ShowObjectNamesSizesAndCounts("Albedo/emissive textures", kEditorPrefsMaterialTextures, labels.labels, labels.sizes, dummyCounts, Precision.Hundredths);
            }

            {
                GeoMemLabels labels = Lightmapping.GetGeometryMemory();
                ShowObjectNamesSizesAndCounts("Geometry data", kEditorPrefsGeometryData, labels.labels, labels.sizes, labels.triCounts, Precision.Hundredths);
            }

            {
                MemLabels labels = Lightmapping.GetNotShownMemLabels();
                string remainingEntriesFoldoutName = Lightmapping.isProgressiveLightmapperDone ? "Leaks" : "In-flight";
                ShowObjectNamesSizesAndCounts(remainingEntriesFoldoutName, kEditorPrefsInFlight, labels.labels, labels.sizes, dummyCounts, Precision.Tenths);
            }

            {
                Dictionary<Hash128, SortedList<int, int>> gbufferHashToLightmapIndices = new Dictionary<Hash128, SortedList<int, int>>();
                for (int i = 0; i < lightmaps.Length; i++)
                {
                    Hash128 gbufferHash;
                    if (Lightmapping.GetGBufferHash(i, out gbufferHash))
                    {
                        if (!gbufferHashToLightmapIndices.ContainsKey(gbufferHash))
                            gbufferHashToLightmapIndices.Add(gbufferHash, new SortedList<int, int>());

                        gbufferHashToLightmapIndices[gbufferHash].Add(i, i);
                    }
                }

                float totalGBuffersSize = 0.0f;
                float totalLightmapsSize = 0.0f;

                foreach (var entry in gbufferHashToLightmapIndices)
                {
                    Hash128 gbufferHash = entry.Key;
                    float gbufferDataSize = Lightmapping.GetGBufferMemory(ref gbufferHash);
                    totalGBuffersSize += gbufferDataSize;

                    SortedList<int, int> lightmapIndices = entry.Value;
                    foreach (var i in lightmapIndices)
                    {
                        LightmapMemory lightmapMemory = Lightmapping.GetLightmapMemory(i.Value);
                        totalLightmapsSize += lightmapMemory.lightmapDataSize;
                        totalLightmapsSize += lightmapMemory.lightmapTexturesSize;
                    }
                }

                string foldoutNameFull = String.Format(
                        "G-buffers ({0}) | Lightmaps ({1})",
                        SizeString(totalGBuffersSize),
                        SizeString(totalLightmapsSize));

                EditorGUILayout.FoldoutTitlebar(true, new GUIContent(foldoutNameFull), true);

                EditorGUIUtility.labelWidth = oldWidth;
            }
        }

        private void LightmapDebugInfo(int index)
        {
            if (!showDebugInfo)
                return;

            GUILayout.Space(5);
            GUILayout.BeginVertical();

            LightmapConvergence lc = Lightmapping.GetLightmapConvergence(index);
            if (lc.IsValid())
            {
                GUILayout.Label("Occupied: " + InternalEditorUtility.CountToString((ulong)lc.occupiedTexelCount), EditorStyles.miniLabel);

                GUIContent direct = EditorGUIUtility.TrTextContent("Direct: " + lc.minDirectSamples + " / " + lc.maxDirectSamples + " / " + lc.avgDirectSamples + "", "min / max / avg samples per texel");
                GUILayout.Label(direct, EditorStyles.miniLabel);

                GUIContent gi = EditorGUIUtility.TrTextContent("GI: " + lc.minGISamples + " / " + lc.maxGISamples + " / " + lc.avgGISamples + "", "min / max / avg samples per texel");
                GUILayout.Label(gi, EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label("Occupied: N/A", EditorStyles.miniLabel);
                GUILayout.Label("Direct: N/A", EditorStyles.miniLabel);
                GUILayout.Label("GI: N/A", EditorStyles.miniLabel);
            }
            float mraysPerSec = Lightmapping.GetLightmapBakePerformance(index);
            if (mraysPerSec >= 0.0)
                GUILayout.Label(mraysPerSec.ToString("0.00") + " mrays/sec", EditorStyles.miniLabel);
            else
                GUILayout.Label("N/A mrays/sec", EditorStyles.miniLabel);

            LightmapMemory lightmapMemory = Lightmapping.GetLightmapMemory(index);
            GUILayout.Label("Lightmap data: " + lightmapMemory.lightmapDataSize.ToString("0.0") + " MB", EditorStyles.miniLabel);

            GUILayout.EndVertical();
            GUILayout.Space(5);
            GUILayout.BeginVertical();

            GUIContent lightmapTexturesSizeContent = null;
            if (lightmapMemory.lightmapTexturesSize > 0.0f)
                lightmapTexturesSizeContent = EditorGUIUtility.TrTextContent("Lightmap textures: " + SizeString(lightmapMemory.lightmapTexturesSize));
            else
                lightmapTexturesSizeContent = EditorGUIUtility.TrTextContent("Lightmap textures: N/A", "This lightmap has converged and is not owned by the Progressive Lightmapper anymore.");
            GUILayout.Label(lightmapTexturesSizeContent, EditorStyles.miniLabel);

            GUILayout.EndVertical();
        }

        private void ShowObjectNamesSizesAndCounts(string foldoutName, string editorPrefsName, string[] objectNames, float[] sizes, System.UInt64[] counts, Precision precision)
        {
            Debug.Assert(objectNames.Length == sizes.Length);

            if (objectNames.Length == 0)
                return;

            string countString = counts.Length > 0 ? (SumCounts(counts).ToString() + " tris, ") : "";
            string sizeString = SizeString(SumSizes(sizes));
            string foldoutNameFull = foldoutName + " (" + countString + sizeString + ")";
            bool showDetailsOld = EditorPrefs.GetBool(editorPrefsName, false);

            bool showDetails = EditorGUILayout.FoldoutTitlebar(showDetailsOld, new GUIContent(foldoutNameFull), true);

            if (showDetails != showDetailsOld)
                EditorPrefs.SetBool(editorPrefsName, showDetails);

            if (!showDetails)
                return;

            GUILayout.BeginHorizontal();
            {
                string[] stringSeparators = new string[] { " | " };

                GUILayout.Space(20);

                GUILayout.BeginVertical();
                for (int i = 0; i < objectNames.Length; ++i)
                {
                    string fullName = objectNames[i];
                    string[] result = fullName.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                    Debug.Assert(result.Length > 0);
                    string objectName = result[0];
                    string tooltip = "";
                    if (result.Length > 1)
                        tooltip = result[1];

                    GUILayout.Label(new GUIContent(objectName, tooltip), EditorStyles.miniLabel);
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                for (int i = 0; i < counts.Length; ++i)
                {
                    GUILayout.Label(counts[i].ToString(), EditorStyles.miniLabel);
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                string format = (precision == Precision.Tenths) ? "0.0" : "0.00";
                for (int i = 0; i < sizes.Length; ++i)
                {
                    GUILayout.Label(sizes[i].ToString(format) + " MB", EditorStyles.miniLabel);
                }
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }
    }
} // namespace
