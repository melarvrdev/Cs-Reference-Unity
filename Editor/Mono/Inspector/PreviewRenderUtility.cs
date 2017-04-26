// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    class PreviewScene : IDisposable
    {
        private readonly Scene m_Scene;
        private readonly List<GameObject> m_GameObjects = new List<GameObject>();
        private readonly Camera m_Camera;

        public PreviewScene(string sceneName)
        {
            m_Scene = EditorSceneManager.NewPreviewScene();
            m_Scene.name = sceneName;

            var camGO = EditorUtility.CreateGameObjectWithHideFlags("Preview Scene Camera", HideFlags.HideAndDontSave, typeof(Camera));
            AddGameObject(camGO);
            m_Camera = camGO.GetComponent<Camera>();
            camera.cameraType = CameraType.Preview;
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.Depth;
            camera.fieldOfView = 15;
            camera.farClipPlane = 10.0f;
            camera.nearClipPlane = 2.0f;
            camera.backgroundColor = new Color(49.0f / 255.0f, 49.0f / 255.0f, 49.0f / 255.0f, 1.0f);

            // Explicitly use forward rendering for all previews
            // (deferred fails when generating some static previews at editor launch; and we never want
            // vertex lit previews if that is chosen in the player settings)
            camera.renderingPath = RenderingPath.Forward;
            camera.useOcclusionCulling = false;
            camera.scene = m_Scene;
        }

        public Camera camera
        {
            get { return m_Camera; }
        }

        public Scene scene
        {
            get { return m_Scene; }
        }

        public void AddGameObject(GameObject go)
        {
            SceneManager.MoveGameObjectToScene(go, m_Scene);
            m_GameObjects.Add(go);
        }

        public void DestroyGameObject(GameObject go)
        {
            m_GameObjects.Remove(go);
            Object.DestroyImmediate(go);
        }

        public void Dispose()
        {
            EditorSceneManager.ClosePreviewScene(m_Scene);

            foreach (var go in m_GameObjects)
                Object.DestroyImmediate(go);

            m_GameObjects.Clear();
        }
    }

    public class PreviewRenderUtility
    {
        private readonly PreviewScene m_PreviewScene;

        private RenderTexture m_RenderTexture;
        private Rect m_TargetRect;
        private SavedRenderTargetState m_SavedState;
        private readonly List<GameObject> m_TempGameObjects = new List<GameObject>();

        public PreviewRenderUtility(bool renderFullScene) : this()
        {}

        public PreviewRenderUtility()
        {
            m_PreviewScene = new PreviewScene("Preview Scene");

            var l0 = CreateLight();
            m_PreviewScene.AddGameObject(l0);
            Light0 = l0.GetComponent<Light>();

            var l1 = CreateLight();
            m_PreviewScene.AddGameObject(l1);
            Light1 = l1.GetComponent<Light>();

            Light0.color = SceneView.kSceneViewFrontLight;
            Light1.transform.rotation = Quaternion.Euler(340, 218, 177);
            Light1.color = new Color(.4f, .4f, .45f, 0f) * .7f;
        }

        [Obsolete("Use the property camera instead (UnityUpgradable) -> camera", false)]
        public Camera m_Camera;

        public Camera camera
        {
            get { return m_PreviewScene.camera; }
        }

        [Obsolete("Use the property cameraFieldOfView (UnityUpgradable) -> cameraFieldOfView", false)]
        public float m_CameraFieldOfView;

        public float cameraFieldOfView
        {
            get { return camera.fieldOfView; }
            set { camera.fieldOfView = value; }
        }

        public Color ambientColor { get; set; }

        [Obsolete("Use the property lights (UnityUpgradable) -> lights", false)]
        public Light[] m_Light;

        public Light[] lights
        {
            get
            {
                return new[] {Light0, Light1};
            }
        }

        private Light Light0 { get; set; }

        private Light Light1 { get; set; }

        internal RenderTexture renderTexture
        {
            get { return m_RenderTexture; }
        }

        public void Cleanup()
        {
            if (m_RenderTexture)
            {
                Object.DestroyImmediate(m_RenderTexture);
                m_RenderTexture = null;
            }
            m_PreviewScene.Dispose();
        }

        private void BeginPreview(Rect r, GUIStyle previewBackground, bool hdr)
        {
            InitPreview(r, hdr);

            if (previewBackground == null || previewBackground == GUIStyle.none)
                return;

            Graphics.DrawTexture(
                previewBackground.overflow.Add(new Rect(0, 0, m_RenderTexture.width, m_RenderTexture.height)),
                previewBackground.normal.background,
                new Rect(0, 0, 1, 1),
                previewBackground.border.left, previewBackground.border.right, previewBackground.border.top,
                previewBackground.border.bottom,
                new Color(.5f, .5f, .5f, 1),
                null
                );
        }

        private void BeginStaticPreview(Rect r, bool hdr)
        {
            InitPreview(r, hdr);
            var color = new Color(82 / 255f, 82 / 255f, 82 / 255f, 1.0f);
            var darkGreyBackground = new Texture2D(1, 1, TextureFormat.RGBA32, true, true);
            darkGreyBackground.SetPixel(0, 0, color);
            darkGreyBackground.Apply();
            Graphics.DrawTexture(new Rect(0, 0, m_RenderTexture.width, m_RenderTexture.height), darkGreyBackground);
            Object.DestroyImmediate(darkGreyBackground);
        }

        private void InitPreview(Rect r, bool hdr)
        {
            m_TargetRect = r;
            float scaleFac = GetScaleFactor(r.width, r.height);

            int rtWidth = (int)(r.width * scaleFac);
            int rtHeight = (int)(r.height * scaleFac);
            if (!m_RenderTexture || m_RenderTexture.width != rtWidth || m_RenderTexture.height != rtHeight)
            {
                if (m_RenderTexture)
                {
                    Object.DestroyImmediate(m_RenderTexture);
                    m_RenderTexture = null;
                }

                // Do not use GetTemporary to manage render textures. Temporary RTs are only
                // garbage collected each N frames, and in the editor we might be wildly resizing
                // the inspector, thus using up tons of memory.
                m_RenderTexture = new RenderTexture(rtWidth, rtHeight, 16, hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                m_RenderTexture.hideFlags = HideFlags.HideAndDontSave;

                camera.targetTexture = m_RenderTexture;
            }

            m_SavedState = new SavedRenderTargetState();
            EditorGUIUtility.SetRenderTextureNoViewport(m_RenderTexture);
            GL.LoadOrtho();
            GL.LoadPixelMatrix(0, m_RenderTexture.width, m_RenderTexture.height, 0);
            ShaderUtil.rawViewportRect = new Rect(0, 0, m_RenderTexture.width, m_RenderTexture.height);
            ShaderUtil.rawScissorRect = new Rect(0, 0, m_RenderTexture.width, m_RenderTexture.height);
            GL.Clear(true, true, new Color(0, 0, 0, 0));
        }

        public float GetScaleFactor(float width, float height)
        {
            float scaleFacX = Mathf.Max(Mathf.Min(width * 2, 1024), width) / width;
            float scaleFacY = Mathf.Max(Mathf.Min(height * 2, 1024), height) / height;
            return Mathf.Min(scaleFacX, scaleFacY) * EditorGUIUtility.pixelsPerPoint;
        }

        public void BeginStaticPreview(Rect r)
        {
            BeginStaticPreview(r, false);
        }

        public void BeginStaticPreviewHDR(Rect r)
        {
            BeginStaticPreview(r, true);
        }

        public void BeginPreview(Rect r, GUIStyle previewBackground)
        {
            BeginPreview(r, previewBackground, false);
        }

        public void BeginPreviewHDR(Rect r, GUIStyle previewBackground)
        {
            BeginPreview(r, previewBackground, true);
        }

        public Texture EndPreview()
        {
            m_SavedState.Restore();
            FinishFrame();
            return m_RenderTexture;
        }

        private void FinishFrame()
        {
            foreach (var go in m_TempGameObjects)
                m_PreviewScene.DestroyGameObject(go);

            m_TempGameObjects.Clear();
        }

        public void EndAndDrawPreview(Rect r)
        {
            Texture t = EndPreview();
            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            GUI.DrawTexture(r, t, ScaleMode.StretchToFill, false);
            GL.sRGBWrite = false;
        }

        public Texture2D EndStaticPreview()
        {
            var tmp = RenderTexture.GetTemporary((int)m_TargetRect.width, (int)m_TargetRect.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);

            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            Graphics.Blit(m_RenderTexture, tmp);
            GL.sRGBWrite = false;

            RenderTexture.active = tmp;
            var copy = new Texture2D((int)m_TargetRect.width, (int)m_TargetRect.height, TextureFormat.RGB24, false, true);
            copy.ReadPixels(new Rect(0, 0, m_TargetRect.width, m_TargetRect.height), 0, 0);
            copy.Apply();
            RenderTexture.ReleaseTemporary(tmp);
            m_SavedState.Restore();
            FinishFrame();
            return copy;
        }

        public void AddSingleGO(GameObject go, bool instantiateAtZero = false)
        {
            var copy = instantiateAtZero ? Object.Instantiate(go, Vector3.zero, Quaternion.identity) : Object.Instantiate(go);
            m_PreviewScene.AddGameObject(copy);
            m_TempGameObjects.Add(copy);
        }

        public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material mat, int subMeshIndex)
        {
            DrawMesh(mesh, matrix, mat, subMeshIndex, null, null, false);
        }

        public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material mat, int subMeshIndex, MaterialPropertyBlock customProperties)
        {
            DrawMesh(mesh, matrix, mat, subMeshIndex, customProperties, null, false);
        }

        public void DrawMesh(Mesh mesh, Matrix4x4 m, Material mat, int subMeshIndex, MaterialPropertyBlock customProperties, Transform probeAnchor, bool useLightProbe)
        {
            var quat = Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
            var pos = m.GetColumn(3);

            DrawMesh(mesh, pos, quat, mat, subMeshIndex, customProperties, probeAnchor, useLightProbe);
        }

        public void DrawMesh(Mesh mesh, Vector3 pos, Quaternion rot, Material mat, int subMeshIndex)
        {
            DrawMesh(mesh, pos, rot, mat, subMeshIndex, null, null, false);
        }

        public void DrawMesh(Mesh mesh, Vector3 pos, Quaternion rot, Material mat, int subMeshIndex, MaterialPropertyBlock customProperties)
        {
            DrawMesh(mesh, pos, rot, mat, subMeshIndex, customProperties, null, false);
        }

        public void DrawMesh(Mesh mesh, Vector3 pos, Quaternion rot, Material mat, int subMeshIndex, MaterialPropertyBlock customProperties, Transform probeAnchor)
        {
            DrawMesh(mesh, pos, rot, mat, subMeshIndex, customProperties, probeAnchor, false);
        }

        public void DrawMesh(Mesh mesh, Vector3 pos, Quaternion rot, Material mat, int subMeshIndex, MaterialPropertyBlock customProperties, Transform probeAnchor, bool useLightProbe)
        {
            var meshGo = EditorUtility.CreateGameObjectWithHideFlags("Mesh", HideFlags.HideAndDontSave, typeof(MeshFilter), typeof(MeshRenderer));
            meshGo.transform.position = pos;
            meshGo.transform.rotation = rot;

            var filter = meshGo.GetComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var renderer = meshGo.GetComponent<MeshRenderer>();

            var materials = renderer.sharedMaterials;
            if (subMeshIndex < materials.Length)
                materials[subMeshIndex] = mat;
            renderer.sharedMaterials = materials;

            renderer.SetPropertyBlock(customProperties);
            renderer.probeAnchor = probeAnchor;
            renderer.lightProbeUsage = useLightProbe ? LightProbeUsage.BlendProbes : LightProbeUsage.Off;

            m_PreviewScene.AddGameObject(meshGo);
            m_TempGameObjects.Add(meshGo);

            if (probeAnchor != null)
            {
                var probe = Object.Instantiate(probeAnchor.gameObject);
                ReflectionProbe previewProbe = probe.GetComponent<ReflectionProbe>();
                previewProbe.mode = ReflectionProbeMode.Custom;
                previewProbe.customBakedTexture = probeAnchor.GetComponent<ReflectionProbe>().texture;
                previewProbe.transform.position = Vector3.zero;

                m_PreviewScene.AddGameObject(probe);
                m_TempGameObjects.Add(probe);
            }
        }

        internal static Mesh GetPreviewSphere()
        {
            var handleGo = (GameObject)EditorGUIUtility.LoadRequired("Previews/PreviewMaterials.fbx");
            // Temp workaround to make it not render in the scene
            handleGo.SetActive(false);
            foreach (Transform t in handleGo.transform)
            {
                if (t.name == "sphere")
                    return t.GetComponent<MeshFilter>().sharedMesh;
            }
            return null;
        }

        protected static GameObject CreateLight()
        {
            GameObject lightGO = EditorUtility.CreateGameObjectWithHideFlags("PreRenderLight", HideFlags.HideAndDontSave, typeof(Light));
            var light = lightGO.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            return lightGO;
        }

        public void Render(bool allowScriptableRenderPipeline = false)
        {
            Unsupported.SetOverrideRenderSettings(m_PreviewScene.scene);
            RenderSettings.ambientLight = ambientColor;
            var oldAllowPipes = Unsupported.useScriptableRenderPipeline;
            Unsupported.useScriptableRenderPipeline = allowScriptableRenderPipeline;

            float saveFieldOfView = camera.fieldOfView;

            // Calculate a view multiplier to avoid clipping when the preview width is smaller than the height.
            float viewMultiplier = (m_RenderTexture.width <= 0 ? 1.0f : Mathf.Max(1.0f, (float)m_RenderTexture.height / m_RenderTexture.width));
            // Multiply the viewing area by the viewMultiplier - it requires some conversions since the camera view is expressed as an angle.
            camera.fieldOfView = Mathf.Atan(viewMultiplier * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad)) * Mathf.Rad2Deg * 2.0f;

            camera.Render();

            camera.fieldOfView = saveFieldOfView;
            Unsupported.useScriptableRenderPipeline = oldAllowPipes;
            Unsupported.RestoreOverrideRenderSettings();
        }
    }

    internal class SavedRenderTargetState
    {
        RenderTexture renderTexture;
        Rect viewport;
        Rect scissor;

        internal SavedRenderTargetState()
        {
            GL.PushMatrix();
            if (ShaderUtil.hardwareSupportsRectRenderTexture)
                renderTexture = RenderTexture.active;
            viewport = ShaderUtil.rawViewportRect;
            scissor = ShaderUtil.rawScissorRect;
        }

        internal void Restore()
        {
            if (ShaderUtil.hardwareSupportsRectRenderTexture)
                EditorGUIUtility.SetRenderTextureNoViewport(renderTexture);
            ShaderUtil.rawViewportRect = viewport;
            ShaderUtil.rawScissorRect = scissor;
            GL.PopMatrix();
        }
    }
}
