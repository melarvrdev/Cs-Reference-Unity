// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Overlays;
using UnityEditorInternal;
using UnityEditor.Snap;
using UnityEngine;
using UnityEngine.UIElements;
using FrameCapture = UnityEngine.Apple.FrameCapture;
using FrameCaptureDestination = UnityEngine.Apple.FrameCaptureDestination;

namespace UnityEditor.Toolbars
{
    static class SceneViewToolbarElements
    {
        const string k_StyleSheet = "StyleSheets/SceneViewToolbarElements/SceneViewToolbarElements.uss";
        const string k_StyleLight = "StyleSheets/SceneViewToolbarElements/SceneViewToolbarElementsLight.uss";
        const string k_StyleDark = "StyleSheets/SceneViewToolbarElements/SceneViewToolbarElementsDark.uss";

        static StyleSheet s_Style;
        static StyleSheet s_Skin;
        internal static  void AddStyleSheets(VisualElement ve)
        {
            if (s_Skin == null)
            {
                if (EditorGUIUtility.isProSkin)
                    s_Skin = EditorGUIUtility.Load(k_StyleDark) as StyleSheet;
                else
                    s_Skin = EditorGUIUtility.Load(k_StyleLight) as StyleSheet;
            }
            if (s_Style == null)
            {
                s_Style = EditorGUIUtility.Load(k_StyleSheet) as StyleSheet;
            }
            ve.styleSheets.Add(s_Style);
            ve.styleSheets.Add(s_Skin);
        }
    }

    [EditorToolbarElement("SceneView/Camera Mode", typeof(SceneView))]
    sealed class CameraModeElement : EditorToolbarDropdown, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        static readonly string s_UssClassName_Shaded = "cameramode-shaded";
        static readonly string s_UssClassName_ShadedWireframe = "cameramode-shadedwireframe";
        static readonly string s_UssClassName_Wireframe = "cameramode-wireframe";
        static readonly string s_UssClassName_Debug = "cameramode-debug";

        string currentUssClassName;

        public CameraModeElement()
        {
            name = "CameraModeDropDown";
            tooltip = L10n.Tr("The Draw Mode used to display the Scene.");

            clicked += () => PopupWindow.Show(worldBound, new SceneRenderModeWindow(sceneView));

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            AddToClassList(s_UssClassName_Debug);
            currentUssClassName = s_UssClassName_Debug;
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            SceneViewOnCameraModeChanged(sceneView.cameraMode);
            //Settings the icon display explicitly as this is set to DisplayStyle.Flex when icon = null
            //Here the icon is set using USS so on the C# side icon = null
            var iconElement = this.Q<Image>();
            iconElement.style.display = DisplayStyle.Flex;
            sceneView.onCameraModeChanged += SceneViewOnCameraModeChanged;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.onCameraModeChanged -= SceneViewOnCameraModeChanged;
        }

        void SceneViewOnCameraModeChanged(SceneView.CameraMode mode)
        {
            RemoveFromClassList(currentUssClassName);
            switch (mode.name)
            {
                case "Shaded":
                    currentUssClassName = s_UssClassName_Shaded;
                    break;

                case "Wireframe":
                    currentUssClassName = s_UssClassName_Wireframe;
                    break;

                case "Shaded Wireframe":
                    currentUssClassName = s_UssClassName_ShadedWireframe;
                    break;

                default:
                    currentUssClassName = s_UssClassName_Debug;
                    break;
            }
            AddToClassList(currentUssClassName);
        }
    }

    [EditorToolbarElement("SceneView/2D", typeof(SceneView))]
    sealed class In2DModeElement : EditorToolbarToggle, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public In2DModeElement()
        {
            name = "SceneView2D";
            tooltip = L10n.Tr("When toggled on, the Scene is in 2D view. When toggled off, the Scene is in 3D view.");
            this.RegisterValueChangedCallback(evt => sceneView.in2DMode = evt.newValue);
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            value = sceneView.in2DMode;
            sceneView.modeChanged2D += OnModeChanged;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.modeChanged2D -= OnModeChanged;
        }

        void OnModeChanged(bool enabled)
        {
            value = enabled;
        }
    }

    [EditorToolbarElement("SceneView/Lighting", typeof(SceneView))]
    sealed class SceneLightingElement : EditorToolbarToggle, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public SceneLightingElement()
        {
            name = "SceneviewLighting";
            tooltip = L10n.Tr("When toggled on, the Scene lighting is used. When toggled off, a light attached to the Scene view camera is used.");

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            this.RegisterValueChangedCallback(evt => sceneView.sceneLighting = evt.newValue);
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            value = sceneView.sceneLighting;
            sceneView.sceneLightingChanged += SceneViewOnsceneLightingChanged;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.sceneLightingChanged -= SceneViewOnsceneLightingChanged;
        }

        void SceneViewOnsceneLightingChanged(bool lightingOn)
        {
            value = lightingOn;
        }
    }

    [EditorToolbarElement("SceneView/Audio", typeof(SceneView))]
    sealed class SceneAudioElement : EditorToolbarToggle, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public SceneAudioElement()
        {
            name = "SceneviewAudio";
            tooltip = "Toggle audio on or off.";

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            this.RegisterValueChangedCallback(evt => sceneView.audioPlay = evt.newValue);
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            value = sceneView.audioPlay;
            sceneView.sceneAudioChanged += SceneViewOnsceneAudioChanged;
            EditorApplication.update += CheckAvailability;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.sceneAudioChanged -= SceneViewOnsceneAudioChanged;
            EditorApplication.update -= CheckAvailability;
        }

        void SceneViewOnsceneAudioChanged(bool audio)
        {
            value = audio;
        }

        void CheckAvailability()
        {
            SetEnabled(!EditorApplication.isPlaying);
        }
    }

    [EditorToolbarElement("SceneView/Fx", typeof(SceneView))]
    sealed class SceneFxElement : EditorToolbarDropdownToggle, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public SceneFxElement()
        {
            name = "SceneviewFx";
            tooltip = L10n.Tr("Toggle skybox, fog, and various other effects.");

            dropdownClicked += () => PopupWindow.Show(worldBound, new SceneFXWindow(sceneView));

            this.RegisterValueChangedCallback(delegate(ChangeEvent<bool> evt)
            {
                sceneView.sceneViewState.fxEnabled = evt.newValue;
            });

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            sceneView.sceneViewState.fxEnableChanged += OnSceneFxChanged;
            OnSceneFxChanged(sceneView.sceneViewState.fxEnabled);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.sceneViewState.fxEnableChanged -= OnSceneFxChanged;
        }

        void OnSceneFxChanged(bool enabled)
        {
            value = enabled;
        }
    }

    [EditorToolbarElement("SceneView/Scene Visibility", typeof(SceneView))]
    sealed class SceneVisElement : EditorToolbarToggle, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public SceneVisElement()
        {
            name = "SceneViewVisibility";
            tooltip = "Number of hidden objects, click to toggle scene visibility";

            this.RegisterValueChangedCallback(evt => sceneView.sceneVisActive = evt.newValue);
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            sceneView.sceneVisActiveChanged += SceneViewOnsceneVisActiveChanged;
            value = sceneView.sceneVisActive;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.sceneVisActiveChanged -= SceneViewOnsceneVisActiveChanged;
        }

        void SceneViewOnsceneVisActiveChanged(bool active)
        {
            value = active;
        }
    }

    [EditorToolbarElement("SceneView/Grids", typeof(SceneView))]
    sealed class SceneViewGridSettingsElement : EditorToolbarDropdownToggle, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public SceneViewGridSettingsElement()
        {
            name = "SceneviewGrids";
            tooltip = L10n.Tr("Toggle the visibility of the grid");

            this.RegisterValueChangedCallback(delegate(ChangeEvent<bool> evt)
            {
                sceneView.sceneViewGrids.showGrid = evt.newValue;
            });
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void OnDropdownClicked()
        {
            if (!(containerWindow is SceneView view))
                return;

            var w = PopupWindowBase.Show<GridSettingsWindow>(this, new Vector2(300, 88));

            if(w != null)
                w.Init(view);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            value = sceneView.sceneViewGrids.showGrid;
            sceneView.gridVisibilityChanged += SceneViewOngridVisibilityChanged;
            sceneView.sceneViewGrids.gridRenderAxisChanged += OnSceneViewOngridRenderAxisChanged;
            OnSceneViewOngridRenderAxisChanged(sceneView.sceneViewGrids.gridAxis);
            dropdownClicked += OnDropdownClicked;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.gridVisibilityChanged -= SceneViewOngridVisibilityChanged;
            sceneView.sceneViewGrids.gridRenderAxisChanged -= OnSceneViewOngridRenderAxisChanged;
            dropdownClicked -= OnDropdownClicked;
        }

        void OnSceneViewOngridRenderAxisChanged(SceneViewGrid.GridRenderAxis axis)
        {
            EnableInClassList("unity-sceneview-grid-axis--x", axis == SceneViewGrid.GridRenderAxis.X);
            EnableInClassList("unity-sceneview-grid-axis--y", axis == SceneViewGrid.GridRenderAxis.Y);
            EnableInClassList("unity-sceneview-grid-axis--z", axis == SceneViewGrid.GridRenderAxis.Z);
        }

        void SceneViewOngridVisibilityChanged(bool visibility)
        {
            value = visibility;
        }
    }

    [EditorToolbarElement("SceneView/Render Doc", typeof(SceneView))]
    sealed class RenderDocElement : EditorToolbarButton, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public RenderDocElement()
        {
            name = "FrameCapture";
            tooltip = L10n.Tr(RenderDocUtil.openInRenderDocLabel);
            icon = EditorGUIUtility.FindTexture("FrameCapture");
            UpdateState();

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void UpdateState()
        {
            style.display = RenderDoc.IsLoaded() && RenderDoc.IsSupported() ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            clicked += OnAction;
            EditorApplication.update += OnUpdate;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            clicked -= OnAction;
            EditorApplication.update -= OnUpdate;
        }

        void OnAction()
        {
            sceneView.m_Parent.CaptureRenderDocScene();
        }

        void OnUpdate()
        {
            UpdateState();
        }
    }

    [EditorToolbarElement("SceneView/Metal Capture", typeof(SceneView))]
    sealed class MetalCaptureElement : EditorToolbarButton, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public MetalCaptureElement()
        {
            name = "MetalCapture";
            tooltip = L10n.Tr("Capture the current view and open in Xcode frame debugger");
            icon = EditorGUIUtility.FindTexture("FrameCapture");
            UpdateState();

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void UpdateState()
        {
            style.display = FrameCapture.IsDestinationSupported(FrameCaptureDestination.DevTools)
                || FrameCapture.IsDestinationSupported(FrameCaptureDestination.GPUTraceDocument) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            clicked += OnAction;
            EditorApplication.update += OnUpdate;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            clicked -= OnAction;
            EditorApplication.update -= OnUpdate;
        }

        void OnAction()
        {
            sceneView.m_Parent.CaptureMetalScene();
        }

        void OnUpdate()
        {
            UpdateState();
        }
    }

    [EditorToolbarElement("SceneView/Scene Camera", typeof(SceneView))]
    sealed class SceneCameraElement : EditorToolbarDropdown, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public SceneCameraElement()
        {
            name = "SceneViewCamera";
            tooltip = "Settings for the Scene view camera.";

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            //Settings the icon display explicitly as this is set to DisplayStyle.Flex when icon = null
            //Here the icon is set using USS so on the C# side icon = null
            var iconElement = this.Q<Image>();
            iconElement.style.display = DisplayStyle.Flex;
            clickable.clickedWithEventInfo += OnClickableOnclickedWithEventInfo;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            clickable.clickedWithEventInfo -= OnClickableOnclickedWithEventInfo;
        }

        void OnClickableOnclickedWithEventInfo(EventBase eventBase)
        {
            if (eventBase.eventTypeId == ContextClickEvent.TypeId())
                SceneViewCameraWindow.ShowContextMenu(sceneView);
            else
                PopupWindow.Show(worldBound, new SceneViewCameraWindow(sceneView));
        }
    }

    [EditorToolbarElement("SceneView/Gizmos", typeof(SceneView))]
    sealed class GizmosElement : EditorToolbarDropdownToggle, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public GizmosElement()
        {
            name = "Gizmos";
            tooltip = L10n.Tr("Toggle visibility of all Gizmos in the Scene view");

            dropdownClicked += () => AnnotationWindow.ShowAtPosition(worldBound, false);

            this.RegisterValueChangedCallback(delegate(ChangeEvent<bool> evt)
            {
                sceneView.drawGizmos = evt.newValue;
            });
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            value = sceneView.drawGizmos;
            sceneView.drawGizmosChanged += SceneViewOndrawGizmosChanged;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.drawGizmosChanged -= SceneViewOndrawGizmosChanged;
        }

        void SceneViewOndrawGizmosChanged(bool enabled)
        {
            value = enabled;
        }
    }

    [EditorToolbarElement("SceneView/Snap Increment", typeof(SceneView))]
    sealed class SnapIncrementSettingsElement : EditorToolbarDropdown
    {
        public SnapIncrementSettingsElement()
        {
            name = "SnapIncrement";
            tooltip = "Snap Increment";
            icon = EditorGUIUtility.FindTexture("Snap/SnapIncrement");

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            clicked += OnDropdownClicked;
        }

        void OnDropdownClicked()
        {
            OverlayPopupWindow.Show<SnapIncrementSettingsWindow>(this, new Vector2(300, 88));
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            clicked -= OnDropdownClicked;
        }
    }

    [EditorToolbarElement("SceneView/Search", typeof(SceneView))]
    sealed class SceneViewSearchElement : VisualElement, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }

        public SceneViewSearchElement()
        {
            name = "Search";
            tooltip = "Search the Hierarchy / Scene View";
            SceneViewToolbarElements.AddStyleSheets(this);
            Add(new IMGUIContainer { onGUIHandler = OnGUI });
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (containerWindow is SceneView sceneView)
                sceneView.ToolbarSearchFieldGUI();
            EditorGUILayout.EndHorizontal();
        }
    }
}
