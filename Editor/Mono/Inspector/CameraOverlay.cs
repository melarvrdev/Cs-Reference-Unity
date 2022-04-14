// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    [Overlay(typeof(SceneView), k_OverlayID, k_DisplayName)]
    [Icon("Icons/Overlays/CameraPreview.png")]
    class SceneViewCameraOverlay : TransientSceneViewOverlay
    {
        internal static bool forceDisable = false;

        // should match color in GizmosDrawers.cpp
        const float kPreviewNormalizedSize = 0.2f;
        const string k_OverlayID = "Scene View/Camera";
        const string k_DisplayName = "Camera Preview";

        Camera m_SelectedCamera;
        Camera selectedCamera => m_SelectedCamera;

        Camera m_PreviewCamera;
        Camera previewCamera => m_PreviewCamera;

        RenderTexture m_PreviewTexture;
        int m_QualitySettingsAntiAliasing = -1;

        public SceneViewCameraOverlay()
        {
            minSize = new Vector2(40, 40);
            maxSize = new Vector2(4000, 4000);

            sizeOverridenChanged += UpdateSize;

            OnSelectionChanged();
        }

        public override bool visible => !forceDisable && selectedCamera != null;

        void OnSelectionChanged()
        {
            m_SelectedCamera = null;

            if (Selection.activeGameObject == null)
                return;

            if (!Selection.activeGameObject.TryGetComponent(out m_SelectedCamera))
                m_SelectedCamera = Selection.GetFiltered<Camera>(SelectionMode.TopLevel).FirstOrDefault();

            displayName = selectedCamera == null || string.IsNullOrEmpty(selectedCamera.name)
                ? "Camera Preview"
                : selectedCamera.name;
        }

        void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            OnSelectionChanged();
        }

        public override void OnCreated()
        {
            Selection.selectionChanged += OnSelectionChanged;

            if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            m_PreviewCamera = EditorUtility.CreateGameObjectWithHideFlags("Preview Camera",
                HideFlags.HideAndDontSave,
                typeof(Camera),
                typeof(Skybox)).GetComponent<Camera>();
            m_PreviewCamera.enabled = false;
            OnSelectionChanged();
        }

        public override void OnWillBeDestroyed()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            if (m_PreviewCamera != null)
                UnityObject.DestroyImmediate(m_PreviewCamera.gameObject, true);
        }

        RenderTexture GetPreviewTextureWithSizeAndAA(int width, int height)
        {
            int antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing);
            if (m_PreviewTexture == null || m_PreviewTexture.width != width || m_PreviewTexture.height != height || m_PreviewTexture.antiAliasing != antiAliasing)
            {
                if (m_PreviewTexture != null)
                    m_PreviewTexture.Release();

                m_PreviewTexture = new RenderTexture(width, height, 24, SystemInfo.GetGraphicsFormat(DefaultFormat.LDR));
                m_QualitySettingsAntiAliasing = QualitySettings.antiAliasing;
                m_PreviewTexture.antiAliasing = antiAliasing;
            }
            return m_PreviewTexture;
        }

        void UpdateSize()
        {
            if (!sizeOverridden)
                size = new Vector2(240, 135);
        }

        public override void OnGUI()
        {
            UpdateSize();

            imguiContainer.style.minWidth = collapsed ? new StyleLength(240) : new StyleLength(StyleKeyword.Auto);
            imguiContainer.style.minHeight = collapsed ? new StyleLength(135) : new StyleLength(StyleKeyword.Auto);
            imguiContainer.parent.style.flexGrow = 1;
            imguiContainer.style.flexGrow = 1;

            if (selectedCamera == null)
            {
                GUILayout.Label("No camera selected", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            if (!CameraEditorUtils.IsViewportRectValidToRender(selectedCamera.rect))
                return;

            var sceneView = SceneView.lastActiveSceneView;

            // Do not render the Camera Preview overlay if the target camera GameObject is not part of the objects the
            // SceneView is rendering
            if (!sceneView.IsGameObjectInThisSceneView(selectedCamera.gameObject))
                return;

            var cameraRect = imguiContainer.rect;
            cameraRect.width = Mathf.Floor(cameraRect.width);

            if (cameraRect.width < 1 || cameraRect.height < 1)
                return;

            if (Event.current.type == EventType.Repaint)
            {
                Graphics.DrawTexture(cameraRect, Texture2D.whiteTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, Color.black);

                // setup camera and render
                previewCamera.CopyFrom(selectedCamera);

                // make sure the preview camera is rendering the same stage as the SceneView is
                if (sceneView.overrideSceneCullingMask != 0)
                    previewCamera.overrideSceneCullingMask = sceneView.overrideSceneCullingMask;
                else
                    previewCamera.scene = sceneView.customScene;

                // also make sure to sync any Skybox component on the preview camera
                var dstSkybox = previewCamera.GetComponent<Skybox>();
                if (dstSkybox)
                {
                    var srcSkybox = selectedCamera.GetComponent<Skybox>();
                    if (srcSkybox && srcSkybox.enabled)
                    {
                        dstSkybox.enabled = true;
                        dstSkybox.material = srcSkybox.material;
                    }
                    else
                    {
                        dstSkybox.enabled = false;
                    }
                }

                Vector2 previewSize = selectedCamera.targetTexture
                    ? new Vector2(selectedCamera.targetTexture.width, selectedCamera.targetTexture.height)
                    : PlayModeView.GetMainPlayModeViewTargetSize();

                if (previewSize.x < 0f)
                {
                    // Fallback to Scene View of not a valid game view size
                    previewSize.x = sceneView.position.width;
                    previewSize.y = sceneView.position.height;
                }

                float rectAspect = cameraRect.width / cameraRect.height;
                float previewAspect = previewSize.x / previewSize.y;
                Rect previewRect = cameraRect;
                if (rectAspect > previewAspect)
                {
                    float stretch = previewAspect / rectAspect;
                    previewRect = new Rect(cameraRect.xMin + cameraRect.width * (1.0f - stretch) * .5f, cameraRect.yMin, stretch * cameraRect.width, cameraRect.height);
                }
                else
                {
                    float stretch = rectAspect / previewAspect;
                    previewRect = new Rect(cameraRect.xMin, cameraRect.yMin + cameraRect.height * (1.0f - stretch) * .5f, cameraRect.width, stretch * cameraRect.height);
                }

                var previewTexture = GetPreviewTextureWithSizeAndAA((int)previewRect.width, (int)previewRect.height);
                previewCamera.targetTexture = previewTexture;
                previewCamera.pixelRect = new Rect(0, 0, previewRect.width, previewRect.height);

                Handles.EmitGUIGeometryForCamera(selectedCamera, previewCamera);

                if (selectedCamera.usePhysicalProperties)
                {
                    // when sensor size is reduced, the previous frame is still visible behing so we need to clear the texture before rendering.
                    RenderTexture rt = RenderTexture.active;
                    RenderTexture.active = previewTexture;
                    GL.Clear(false, true, Color.clear);
                    RenderTexture.active = rt;
                }

                previewCamera.Render();

                Graphics.DrawTexture(previewRect, previewTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, EditorGUIUtility.GUITextureBlit2SRGBMaterial);
            }
        }
    }
}
