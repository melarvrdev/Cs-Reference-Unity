// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.SceneTemplate
{
    [UsedImplicitly, CustomEditor(typeof(SceneTemplateAsset))]
    internal class SceneTemplateAssetInspectorWindow : Editor
    {
        public delegate void SceneTemplateAssetModified(SceneTemplateAsset asset);
        public static event SceneTemplateAssetModified sceneTemplateAssetModified;

        private const string k_ThumbnailAreaName = "scene-template-asset-inspector-thumbnail-area";
        private const string k_DependencyListView = "scene-template-asset-inspector-list-view";
        private const string k_NoLabelRowName = "scene-template-asset-inspector-no-label-row";

        const string k_SceneTemplateInfo = @"Scene Template Pipeline must be a Mono Script whose main class derives from ISceneTemplatePipeline or SceneTemplatePipelineAdapter. The main class and the script must have the same name.";

        private const int k_ItemSize = 16;

        private SceneTemplatePreviewArea m_PreviewArea;

        private const string k_SnapshotTooltip = "Take a snapshot based on the selected target then assign it as thumbnail.";
        private const string k_SnapshotButtonLabel = "Take Snapshot";
        private const string k_SnapshotTargetPopupName = "snapshot";
        private const string k_CreatePipelineTooltip = "Create a new Scene Template Pipeline.";
        private const string k_CreatePipelineButtonLabel = "Create New Scene Template Pipeline";
        private const string k_PipelineHelpUrl = "https://docs.unity3d.com/2020.2/Documentation/Manual/scene-templates.html";
        private List<SerializedProperty> m_DependenciesProperty = new List<SerializedProperty>();

        private Texture2D m_HelpIcon;

        private DependencyListView m_DependencyListView;
        VisualElement Root { get; set; }

        private class SnapshotTargetInfo
        {
            public string Name { get; set; }
            public Action<SnapshotTargetInfo, Action> OnSnapshotAction { get; set; }
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            var sceneTemplateAsset = target as SceneTemplateAsset;

            // Return null to use the default texture
            if (!sceneTemplateAsset || !sceneTemplateAsset.preview)
                return null;

            // We have to decompress compressed textures since saving the template
            // causes a call to RenderStaticPreview and tries to save the main asset thumbnail but fails when
            // the texture is compressed.
            if (GraphicsFormatUtility.IsCompressedFormat(sceneTemplateAsset.preview.graphicsFormat))
            {
                return Decompress(sceneTemplateAsset.preview);
            }

            var previewTex = new Texture2D(sceneTemplateAsset.preview.width, sceneTemplateAsset.preview.height);
            EditorUtility.CopySerialized(sceneTemplateAsset.preview, previewTex);
            return previewTex;
        }

        public override bool UseDefaultMargins()
        {
            return false;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = Root = new VisualElement();
            root.AddToClassList("scene-template-asset-inspector");
            root.AddToClassList(Styles.unityThemeVariables);
            root.AddToClassList(Styles.sceneTemplateThemeVariables);
            root.AddStyleSheetPath(Styles.k_CommonStyleSheetPath);
            root.AddStyleSheetPath(Styles.variableStyleSheet);
            root.style.flexDirection = FlexDirection.Column;

            var detailElement = new VisualElement();

            // Template scene
            var templateSceneProperty = serializedObject.FindProperty(SceneTemplateUtils.TemplateScenePropertyName);
            var templatePropertyField = new PropertyField(templateSceneProperty, "Template Scene");
            templatePropertyField.tooltip = "Scene to instantiate.";
            templatePropertyField.RegisterCallback<ChangeEvent<Object>>(e =>
            {
                RebuildDependencies(root);
                TriggerSceneTemplateModified();
            });
            detailElement.Add(templatePropertyField);

            // Scene title
            var templateTitleProperty = serializedObject.FindProperty(SceneTemplateUtils.TemplateTitlePropertyName);
            var titlePropertyField = new PropertyField(templateTitleProperty, "Title");
            titlePropertyField.tooltip = "Scene template display name. Shown in New Scene Dialog.";
            titlePropertyField.RegisterCallback<ChangeEvent<string>>(e => TriggerSceneTemplateModified());
            detailElement.Add(titlePropertyField);

            // Scene description
            var templateDescriptionProperty = serializedObject.FindProperty(SceneTemplateUtils.TemplateDescriptionPropertyName);
            var description = new PropertyField(templateDescriptionProperty, "Description");
            description.tooltip = "Scene template description. Shown in New Scene Dialog.";
            description.RegisterCallback<ChangeEvent<string>>(e => TriggerSceneTemplateModified());
            description.RegisterCallback<SerializedPropertyBindEvent>(e =>
            {
                EditorApplication.delayCall += () =>
                {
                    if (!description.Children().Any())
                        return;
                    var descriptionTextField = description.Children().First() as TextField;
                    if (descriptionTextField != null)
                    {
                        descriptionTextField.AddToClassList(Styles.classWrappingText);
                        descriptionTextField.multiline = true;
                    }
                };
            });
            detailElement.Add(description);

            var templateAddToDefaultsProperty = serializedObject.FindProperty(SceneTemplateUtils.TemplateAddToDefaultsPropertyName);
            var defaultTemplateField = new VisualElement();
            defaultTemplateField.style.flexDirection = FlexDirection.Row;
            var addToDefaultsPropertyField = new PropertyField(templateAddToDefaultsProperty, " ");
            addToDefaultsPropertyField.RegisterCallback<ChangeEvent<bool>>(e => TriggerSceneTemplateModified());
            addToDefaultsPropertyField.style.flexShrink = 0;
            defaultTemplateField.Add(addToDefaultsPropertyField);
            var label = new Label("Pin in New Scene Dialog");
            label.tooltip = "Pin in New Scene Dialog. Ensuring this template is shown before unpinned template in the list.";
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            label.style.flexShrink = 1;
            defaultTemplateField.Add(label);
            detailElement.Add(defaultTemplateField);
            root.Add(CreateFoldoutInspector(detailElement, "Details", "SceneTemplateInspectorDetailsFoldout"));

            // Template thumbnail
            var templateThumbnailProperty = serializedObject.FindProperty(SceneTemplateUtils.TemplateThumbnailPropertyName);
            var thumbnailField = MakeThumbnailField(templateThumbnailProperty, "Texture");
            root.Add(CreateFoldoutInspector(thumbnailField, "Thumbnail", "SceneTemplateInspectorThumbnailFoldout"));

            // SceneTemplatePipeline
            var sceneTemplatePipeline = new VisualElement();
            var pipelineProperty = serializedObject.FindProperty(SceneTemplateUtils.TemplatePipelineName);
            var pipelineField = new PropertyField(pipelineProperty, "Scene Template Pipeline");
            pipelineField.tooltip = "Scene Template Pipeline must be a Mono Script whose main class derives from ISceneTemplatePipeline or SceneTemplatePipelineAdapter. The main class and the script must have the same name.";
            pipelineField.RegisterCallback<ChangeEvent<Object>>(e =>
            {
                if (e.newValue != null && !SceneTemplateAsset.IsValidPipeline(e.newValue as MonoScript))
                {
                    Debug.LogWarning(k_SceneTemplateInfo);
                    pipelineProperty.objectReferenceValue = null;
                    serializedObject.ApplyModifiedProperties();
                }
            });
            sceneTemplatePipeline.Add(pipelineField);
            var buttonRow = CreateEmptyLabelRow("Scene Template Pipeline", Styles.classUnityPropertyFieldLabel); // Use a hidden label instead of an empty element for proper alignment
            var createPipelineButton = new Button(OnCreateSceneTemplatePipeline) { text = k_CreatePipelineButtonLabel, tooltip = k_CreatePipelineTooltip };
            createPipelineButton.AddToClassList(Styles.classUnityBaseFieldInput);
            buttonRow.Add(createPipelineButton);
            sceneTemplatePipeline.Add(buttonRow);
            root.Add(CreateFoldoutInspectorWithHelp(sceneTemplatePipeline, "Scene Template Pipeline", "SceneTemplatePipelineFoldout", k_PipelineHelpUrl));

            // Dependencies
            root.Add(CreateFoldoutInspector(BuildDependencyRows(), "Dependencies", "SceneTemplateDependenciesFoldout"));
            return root;
        }

        void OnCreateSceneTemplatePipeline()
        {
            var assetPath = AssetDatabase.GetAssetPath(serializedObject.targetObject.GetInstanceID());
            var fileInfo = new FileInfo(assetPath);
            var folder = fileInfo.DirectoryName;
            var scriptAsset = SceneTemplateService.CreateNewSceneTemplatePipeline(folder) as MonoScript;
            if (scriptAsset == null)
                return;
            var pipelineProperty = serializedObject.FindProperty(SceneTemplateUtils.TemplatePipelineName);
            pipelineProperty.objectReferenceValue = scriptAsset;
            serializedObject.ApplyModifiedProperties();
        }

        [UsedImplicitly]
        private void OnEnable()
        {
            m_HelpIcon = EditorGUIUtility.LoadIcon("Icons/_Help.png");
            UpdateSceneTemplateAsset();
        }

        private static Foldout CreateFoldoutInspector(VisualElement element, string title, string foldoutEditorPref)
        {
            var foldout = new Foldout();
            foldout.value = EditorPrefs.GetBool(foldoutEditorPref, true);
            var toggle = foldout.Q<Toggle>();
            toggle.AddToClassList(Styles.classInspectorFoldoutHeader);
            var titleElement = new Label(title);
            titleElement.AddToClassList(Styles.classInspectorFoldoutHeaderText);
            toggle.Children().First().Add(titleElement);
            foldout.Add(element);

            foldout.RegisterValueChangedCallback(e =>
            {
                EditorPrefs.SetBool(foldoutEditorPref, e.newValue);
            });

            return foldout;
        }

        private Foldout CreateFoldoutInspectorWithHelp(VisualElement element, string title, string foldoutEditorPref, string helpUrl)
        {
            var foldout = CreateFoldoutInspector(element, title, foldoutEditorPref);

            var toggleInput = foldout.Q(className: "unity-toggle__input");

            var label = toggleInput.Q<Label>();
            label.style.flexGrow = 1;

            var helpButton = new Button(() =>
            {
                Help.BrowseURL(helpUrl);
            });
            helpButton.style.backgroundImage = new StyleBackground(m_HelpIcon);
            helpButton.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            helpButton.AddToClassList(Styles.classFoldoutHelpButton);
            toggleInput.Add(helpButton);

            return foldout;
        }

        private VisualElement BuildDependencyRows()
        {
            var dependencies = new VisualElement();
            var dependenciesProperty = serializedObject.FindProperty(SceneTemplateUtils.DependenciesPropertyName);

            m_DependenciesProperty = new List<SerializedProperty>();
            PopulateDependencyProperties(dependenciesProperty, m_DependenciesProperty);

            m_DependencyListView = new DependencyListView(m_DependenciesProperty, k_ItemSize, serializedObject);
            m_DependencyListView.AddToClassList(Styles.classUnityBaseField);
            dependencies.Add(m_DependencyListView);
            m_DependencyListView.name = k_DependencyListView;
            UpdateDependencyListVisibility();
            return dependencies;
        }

        private void RebuildDependencies(VisualElement root)
        {
            var sceneTemplateAsset = (SceneTemplateAsset)serializedObject.targetObject;
            sceneTemplateAsset.BindScene(sceneTemplateAsset.templateScene);
            serializedObject.Update();
            var dependenciesProperty = serializedObject.FindProperty(SceneTemplateUtils.DependenciesPropertyName);
            m_DependenciesProperty.Clear();
            PopulateDependencyProperties(dependenciesProperty, m_DependenciesProperty);
            m_DependencyListView.Refresh();
            UpdateDependencyListVisibility();
        }

        private void UpdateDependencyListVisibility()
        {
            m_DependencyListView.UpdateListViewSize();
            m_DependencyListView.visible = m_DependenciesProperty.Count > 0;
        }

        private static void PopulateDependencyProperties(SerializedProperty rootProperty, List<SerializedProperty> model)
        {
            var numDependency = rootProperty.arraySize;
            for (var i = 0; i < numDependency; ++i)
            {
                var depInfoProperty = rootProperty.GetArrayElementAtIndex(i);
                var depProperty = depInfoProperty.FindPropertyRelative(SceneTemplateUtils.DependencyPropertyName);
                if (!SceneTemplateProjectSettings.Get().GetDependencyInfo(depProperty.objectReferenceValue).ignore)
                {
                    model.Add(depInfoProperty);
                }
            }
        }

        private void UpdateSceneTemplateAsset()
        {
            var sceneTemplateAsset = (SceneTemplateAsset)serializedObject.targetObject;
            sceneTemplateAsset.UpdateDependencies();
            serializedObject.Update();
        }

        private VisualElement MakeThumbnailField(SerializedProperty thumbnailProperty, string label)
        {
            var propertyElement = new VisualElement();
            var thumbnailObjectField = new PropertyField(thumbnailProperty, label);
            thumbnailObjectField.tooltip = "Scene template thumbnail. Shown in New Scene Dialog.";
            propertyElement.Add(thumbnailObjectField);

            m_PreviewArea = new SceneTemplatePreviewArea(k_ThumbnailAreaName, thumbnailProperty.objectReferenceValue as Texture2D, "No preview available");
            var previewAreaElement = m_PreviewArea.Element;
            previewAreaElement.AddToClassList(Styles.classUnityBaseField);
            propertyElement.Add(previewAreaElement);

            thumbnailObjectField.RegisterCallback<ChangeEvent<Object>>(e =>
            {
                m_PreviewArea.UpdatePreview(e.newValue as Texture2D);
                TriggerSceneTemplateModified();
            });

            // Snapshot header row
            var snapshotHeaderRowElement = new VisualElement();
            snapshotHeaderRowElement.AddToClassList(Styles.classUnityBaseField);
            propertyElement.Add(snapshotHeaderRowElement);
            var snapshotHeaderLabel = new Label("Snapshot");
            snapshotHeaderLabel.tooltip = "Generate a Scene template thumbnail from a snapshot in Scene or Game view.";
            snapshotHeaderLabel.AddToClassList(Styles.classUnityLabel);
            snapshotHeaderRowElement.Add(snapshotHeaderLabel);

            // Snapshot button with dropdown
            var cameraNames = Camera.allCameras.Select(c => new SnapshotTargetInfo { Name = c.name, OnSnapshotAction = TakeSnapshotFromCamera }).ToList();
            cameraNames.Add(new SnapshotTargetInfo()); // Separator
            cameraNames.Add(new SnapshotTargetInfo { Name = "Game View", OnSnapshotAction = (info, callback) => TakeSnapshotFromGameView(callback) });
            var snapshotTargetPopup = new PopupField<SnapshotTargetInfo>("View", cameraNames, Camera.allCameras.Length == 0 ? 1 : 0);
            snapshotTargetPopup.tooltip = "View or Camera to use as the source of the snapshot.";
            snapshotTargetPopup.formatListItemCallback = info => info.Name;
            snapshotTargetPopup.formatSelectedValueCallback = info => info.Name;
            snapshotTargetPopup.name = k_SnapshotTargetPopupName;
            propertyElement.Add(snapshotTargetPopup);

            var snapshotSecondRowElement = CreateEmptyLabelRow();
            propertyElement.Add(snapshotSecondRowElement);
            var snapshotButton = new Button(() =>
            {
                var targetInfo = snapshotTargetPopup.value;
                if (targetInfo.OnSnapshotAction == null)
                    return;

                targetInfo.OnSnapshotAction(targetInfo, null);
            });
            snapshotButton.tooltip = k_SnapshotTooltip;
            snapshotButton.text = k_SnapshotButtonLabel;
            snapshotButton.AddToClassList(Styles.classUnityBaseFieldInput);
            snapshotSecondRowElement.Add(snapshotButton);

            return propertyElement;
        }

        // For testing purposes
        internal void TakeSnapshot(string targetName, Action onFinishedCallback)
        {
            var snapshotTargetPopup = Root.Q<PopupField<SnapshotTargetInfo>>(k_SnapshotTargetPopupName);
            var targetInfo = snapshotTargetPopup.choices.FirstOrDefault((info => info.Name == targetName));
            targetInfo?.OnSnapshotAction?.Invoke(targetInfo, onFinishedCallback);
        }

        private void TakeSnapshotFromCamera(SnapshotTargetInfo targetInfo, Action onFinishedCallback)
        {
            var sceneTemplateAsset = serializedObject.targetObject as SceneTemplateAsset;
            if (!sceneTemplateAsset)
                return;

            var camera = Camera.allCameras.FirstOrDefault(c => c.name == targetInfo.Name);

            if (!camera)
            {
                Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, $"There is no camera named {targetInfo.Name} in the current scene.");
                return;
            }

            var snapshotTexture = SnapshotUtils.TakeCameraSnapshot(camera);
            snapshotTexture.name = sceneTemplateAsset.name;

            // Put the snapshot into the scene template asset
            // This needs to be done before we set it into the property
            // and apply modifications.
            sceneTemplateAsset.AddThumbnailToAsset(snapshotTexture);

            // Thumbnail property gets disposed after AssetDatabase.SaveAssets
            var templateThumbnailProperty = serializedObject.FindProperty(SceneTemplateUtils.TemplateThumbnailPropertyName);
            templateThumbnailProperty.objectReferenceValue = snapshotTexture;
            serializedObject.ApplyModifiedProperties();

            onFinishedCallback?.Invoke();
        }

        private void TakeSnapshotFromSceneCamera(SerializedProperty thumbnailProperty)
        {
            var sceneTemplateAsset = serializedObject.targetObject as SceneTemplateAsset;
            if (!sceneTemplateAsset)
                return;

            var sceneView = EditorWindow.GetWindow<SceneView>();
            if (sceneView == null)
            {
                Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, "Unable to retrieve the Scene view.");
                return;
            }

            SnapshotUtils.TakeSceneViewSnapshot(sceneView, snapshotTexture =>
            {
                snapshotTexture.name = sceneTemplateAsset.name;

                // Put the snapshot into the scene template asset
                // This needs to be done before we set it into the property
                // and apply modifications.
                sceneTemplateAsset.AddThumbnailToAsset(snapshotTexture);

                thumbnailProperty.objectReferenceValue = snapshotTexture;
                serializedObject.ApplyModifiedProperties();
            });
        }

        private static Texture2D Decompress(Texture2D source)
        {
            var uncompressedTexture = new Texture2D(source.width, source.height);
            if (source.isReadable)
            {
                // This supposes that the source is readable
                var pixels = source.GetPixels32();
                uncompressedTexture.SetPixels32(pixels);
                uncompressedTexture.Apply();
            }
            else
            {
                var savedRT = RenderTexture.active;
                var savedViewport = ShaderUtil.rawViewportRect;

                var tmp = RenderTexture.GetTemporary(
                    source.width, source.height,
                    0,
                    SystemInfo.GetGraphicsFormat(DefaultFormat.LDR));
                var mat = EditorGUI.GetMaterialForSpecialTexture(source, null, QualitySettings.activeColorSpace == ColorSpace.Linear);
                if (mat != null)
                    Graphics.Blit(source, tmp, mat);
                else Graphics.Blit(source, tmp);

                RenderTexture.active = tmp;
                uncompressedTexture = new Texture2D(source.width, source.height, source.alphaIsTransparency ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
                uncompressedTexture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                uncompressedTexture.Apply();
                RenderTexture.ReleaseTemporary(tmp);

                EditorGUIUtility.SetRenderTextureNoViewport(savedRT);
                ShaderUtil.rawViewportRect = savedViewport;
            }

            return uncompressedTexture;
        }

        private void TakeSnapshotFromGameView(Action onFinishedCallback)
        {
            var sceneTemplateAsset = serializedObject.targetObject as SceneTemplateAsset;
            if (!sceneTemplateAsset)
                return;

            var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            var gameView = EditorWindow.GetWindow(gameViewType);
            if (gameView == null)
            {
                Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, "Unable to retrieve the Game view.");
                return;
            }

            SnapshotUtils.TakeGameViewSnapshot(gameView, textureCopy =>
            {
                textureCopy.name = sceneTemplateAsset.name;

                // Put the snapshot into the scene template asset
                // This needs to be done before we set it into the property
                // and apply modifications.
                sceneTemplateAsset.AddThumbnailToAsset(textureCopy);

                var templateThumbnailProperty = serializedObject.FindProperty(SceneTemplateUtils.TemplateThumbnailPropertyName);
                templateThumbnailProperty.objectReferenceValue = textureCopy;
                serializedObject.ApplyModifiedProperties();

                onFinishedCallback?.Invoke();
            });
        }

        private void TriggerSceneTemplateModified()
        {
            sceneTemplateAssetModified?.Invoke(serializedObject.targetObject as SceneTemplateAsset);
        }

        private static VisualElement CreateEmptyLabelRow(string hiddenLabel = null, params string[] hiddenLabelClasses)
        {
            var emptyLabelRow = new VisualElement { name = k_NoLabelRowName };
            emptyLabelRow.AddToClassList(Styles.classUnityBaseField);
            var emptyLabel = hiddenLabel == null ? new VisualElement() : new Label(hiddenLabel) { visible = false };
            foreach (var hiddenLabelClass in hiddenLabelClasses)
            {
                emptyLabel.AddToClassList(hiddenLabelClass);
            }
            emptyLabel.AddToClassList(Styles.classUnityBaseFieldLabel);
            emptyLabelRow.Add(emptyLabel);
            return emptyLabelRow;
        }
    }
}
