// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.SceneManagement
{
    [Serializable]
    [MovedFrom("UnityEditor.Experimental.SceneManagement")]
    public sealed partial class PrefabStage : PreviewSceneStage
    {
        static class Styles
        {
            public static GUIContent autoSaveGUIContent = EditorGUIUtility.TrTextContent("Auto Save", "When Auto Save is enabled, every change you make is automatically saved to the Prefab Asset. Disable Auto Save if you experience long import times.");
            public static GUIContent saveButtonContent = EditorGUIUtility.TrTextContent("Save");
            public static GUIContent checkoutButtonContent = EditorGUIUtility.TrTextContent("Check Out");
            public static GUIContent autoSavingBadgeContent = EditorGUIUtility.TrTextContent("Auto Saving...");
            public static GUIContent immutablePrefabContent = EditorGUIUtility.TrTextContent("Immutable Prefab");
            public static GUIStyle saveToggle;
            public static GUIStyle button;
            public static GUIStyle savingBadge = "Badge";
            public static GUIStyle exposablePopup = "ExposablePopupMenu";
            public static GUIStyle exposablePopupItem = "ExposablePopupItem";
            public static GUIContent contextLabel = EditorGUIUtility.TrTextContent("Context:");
            public static GUIContent[] contextRenderModeTexts = new[] { EditorGUIUtility.TrTextContent("Normal"), EditorGUIUtility.TrTextContent("Gray"), EditorGUIUtility.TrTextContent("Hidden") };
            public static StageUtility.ContextRenderMode[] contextRenderModeOptions = new[] { StageUtility.ContextRenderMode.Normal, StageUtility.ContextRenderMode.GreyedOut, StageUtility.ContextRenderMode.Hidden };
            public static GUIContent showOverridesLabel = EditorGUIUtility.TrTextContent("Show Overrides", "Visualize overrides from the Prefab instance on the Prefab Asset. Overrides on the root Transform are always visualized.");
            public static GUIContent showOverridesLabelWithTooManyOverridesTooltip = EditorGUIUtility.TrTextContent("Show Overrides", "Show Overrides are disabled because there are too many overrides to visualize. Overrides on the root Transform are always visualized though.");

            static Styles()
            {
                saveToggle = EditorStyles.toggle;

                button = EditorStyles.miniButton;
                button.margin.top = button.margin.bottom = 0;
            }
        }

        public enum Mode
        {
            InIsolation,
            InContext
        }

        public static event Action<PrefabStage> prefabStageOpened;
        public static event Action<PrefabStage> prefabStageClosing;
        public static event Action<PrefabStage> prefabStageDirtied;
        public static event Action<GameObject> prefabSaving;
        public static event Action<GameObject> prefabSaved;

        internal static event Action<PrefabStage> prefabStageSavedAsNewPrefab;
        internal static event Action<PrefabStage> prefabStageReloaded; // Used by tests.

        internal static List<PrefabStage> m_AllPrefabStages = new List<PrefabStage>();
        static StateCache<PrefabStageHierarchyState> s_StateCache = new StateCache<PrefabStageHierarchyState>("Library/StateCache/PrefabStageHierarchy/");

        GameObject m_PrefabContentsRoot; // Prefab asset being edited
        string m_PrefabAssetPath;
        GameObject m_OpenedFromInstanceRoot;
        GameObject m_OpenedFromInstanceObject;
        ulong m_FileIdForOpenedFromInstanceObject;
        Stage m_ContextStage = null;
        Mode m_Mode;
        int m_InitialSceneDirtyID;
        int m_LastSceneDirtyID;
        bool m_IgnoreNextAssetImportedEventForCurrentPrefab;
        bool m_PrefabWasChangedOnDisk;
        bool m_StageDirtiedFired;
        bool m_IsPrefabInImmutableFolder;
        bool m_IsPrefabInValidAssetFolder;
        HideFlagUtility m_HideFlagUtility;
        Texture2D m_PrefabFileIcon;
        bool m_TemporarilyDisableAutoSave;
        float m_LastSavingDuration = 0f;
        Transform m_LastRootTransform;
        const float kDurationBeforeShowingSavingBadge = 1.0f;
        static ExposablePopupMenu s_ContextRenderModeSelector;
        Hash128 m_InitialFileHash;
        byte[] m_InitialFileContent;
        Hash128 m_LastPrefabSourceFileHash;
        bool m_NeedsReloadingWhenReturningToStage;
        bool m_IsAssetMissing;
        bool m_TemporarilyDisablePatchAllOverridenProperties;
        const int k_MaxNumberOfOverridesToVisualize = 2000;

        internal static SavedBool s_PatchAllOverriddenProperties = new SavedBool("InContextEditingPatchOverriddenProperties", false);

        [System.Serializable]
        struct PatchedProperty
        {
            public PropertyModification modification;
            public UnityEngine.Object targetInContent;
        }
        List<PatchedProperty> m_PatchedProperties;

        bool m_AnalyticsDidUserModify;
        bool m_AnalyticsDidUserSave;

        internal static PrefabStage CreatePrefabStage(string prefabAssetPath, GameObject openedFromInstanceObject, PrefabStage.Mode prefabStageMode, Stage contextStage)
        {
            PrefabStage prefabStage = CreateInstance<PrefabStage>();
            try
            {
                prefabStage.OneTimeInitialize(prefabAssetPath, openedFromInstanceObject, prefabStageMode, contextStage);
            }
            catch
            {
                DestroyImmediate(prefabStage);
                throw;
            }
            return prefabStage;
        }

        // Used for tests, if nothing else.
        internal static ReadOnlyCollection<PrefabStage> allPrefabStages { get { return m_AllPrefabStages.AsReadOnly(); } }

        private PrefabStage()
        {
        }

        void OneTimeInitialize(string prefabAssetPath, GameObject openedFromInstanceGameObject, PrefabStage.Mode prefabStageMode, Stage contextStage)
        {
            if (scene.IsValid())
                Debug.LogError("PrefabStage is already initialized. Please report a bug.");

            m_PrefabAssetPath = prefabAssetPath;
            CachePrefabFolderInfo();
            SetOpenedFromInstanceObject(openedFromInstanceGameObject);

            if (prefabStageMode == PrefabStage.Mode.InContext)
                m_ContextStage = contextStage;
            m_Mode = prefabStageMode;
        }

        bool IsOpenedFromInstanceObjectValid()
        {
            return m_OpenedFromInstanceObject != null && PrefabUtility.IsPartOfPrefabInstance(m_OpenedFromInstanceObject);
        }

        static GameObject FindPrefabInstanceRootThatMatchesPrefabAssetPath(GameObject prefabInstanceObject, string prefabAssetPath)
        {
            if (prefabInstanceObject == null)
                throw new ArgumentNullException(nameof(prefabInstanceObject));

            if (string.IsNullOrEmpty(prefabAssetPath))
                throw new ArgumentNullException(nameof(prefabAssetPath));

            if (PrefabUtility.GetCorrespondingObjectFromSourceAtPath(prefabInstanceObject, prefabAssetPath) == null)
            {
                //Variants with broken parents
                if (AssetDatabase.LoadMainAssetAtPath(prefabAssetPath) is BrokenPrefabAsset)
                    return prefabInstanceObject;
                else
                    throw new ArgumentException($"'{nameof(prefabInstanceObject)}' is not related to the Prefab at path: '{prefabAssetPath}'");
            }

            Transform transform = prefabInstanceObject.transform;
            while (transform != null)
            {
                var assetObject = PrefabUtility.GetCorrespondingObjectFromSourceAtPath(transform, prefabAssetPath);
                if (assetObject != null && assetObject.parent == null)
                    return transform.gameObject;

                transform = transform.parent;
            }

            return null;
        }

        void SetOpenedFromInstanceObject(GameObject go)
        {
            if (go != null)
            {
                if (!PrefabUtility.IsPartOfPrefabInstance(go))
                    throw new ArgumentException("Prefab Mode: GameObject must be part of a Prefab instance, or null.", nameof(go));

                m_OpenedFromInstanceObject = go;
                m_OpenedFromInstanceRoot = FindPrefabInstanceRootThatMatchesPrefabAssetPath(go, m_PrefabAssetPath);
                if (m_OpenedFromInstanceRoot == null)
                    throw new ArgumentException($"Prefab Mode: The 'openedFromInstance' GameObject '{go.name}' is unrelated to the Prefab Asset '{m_PrefabAssetPath}'.");

                m_FileIdForOpenedFromInstanceObject = Unsupported.GetOrGenerateFileIDHint(go);
            }
            else
            {
                m_OpenedFromInstanceObject = null;
                m_OpenedFromInstanceRoot = null;
                m_FileIdForOpenedFromInstanceObject = 0;
            }
        }

        void ReconstructInContextStateIfNeeded()
        {
            // The previous PrefabStage can have been reloaded if user chose to discard changes when entering this PrefabStage,
            // which means we need to update our reference to m_OpenedFromInstanceObject to the newly loaded GameObject
            // (the old GameObject was deleted as part of reloading the PrefabStage).
            bool needsReconstruction = m_OpenedFromInstanceObject == null && m_FileIdForOpenedFromInstanceObject != 0;
            if (!needsReconstruction)
                return;

            var history = StageNavigationManager.instance.stageHistory;
            int index = history.IndexOf(this);
            int previousIndex = index - 1;
            var previousStage = history[previousIndex];
            var previousPrefabStage = previousStage as PrefabStage;
            if (previousPrefabStage)
            {
                var go = PrefabStageUtility.FindFirstGameObjectThatMatchesFileID(previousPrefabStage.prefabContentsRoot.transform, m_FileIdForOpenedFromInstanceObject, true);
                if (go != null)
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(go))
                        SetOpenedFromInstanceObject(go);
                }
                else
                {
                    // Could not find GameObject with fileID: m_FileIdForOpenedFromInstanceObject, this can happen if inserting a variant parent in a Variant and then discarding changes when entering in-context of the new base
                }
            }
        }

        internal bool analyticsDidUserModify { get { return m_AnalyticsDidUserModify; } }
        internal bool analyticsDidUserSave { get { return m_AnalyticsDidUserSave; } }

        static class Icons
        {
            public static Texture2D prefabVariantIcon = EditorGUIUtility.LoadIconRequired("PrefabVariant Icon");
            public static Texture2D prefabIcon = EditorGUIUtility.LoadIconRequired("Prefab Icon");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            PrefabUtility.savingPrefab += OnSavingPrefab;
            PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdated;
            AssetEvents.assetsChangedOnHDD += OnAssetsChangedOnHDD;
            Undo.undoRedoEvent += UndoRedoPerformed;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            m_AllPrefabStages.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            PrefabUtility.savingPrefab -= OnSavingPrefab;
            PrefabUtility.prefabInstanceUpdated -= OnPrefabInstanceUpdated;
            AssetEvents.assetsChangedOnHDD -= OnAssetsChangedOnHDD;
            Undo.undoRedoEvent -= UndoRedoPerformed;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            // Also cleanup any potential registered event handlers
            EditorApplication.update -= DelayedFraming;
            EditorApplication.update -= PerformDelayedAutoSave;

            m_AllPrefabStages.Remove(this);
        }

        public GameObject prefabContentsRoot
        {
            get
            {
                if (m_PrefabContentsRoot == null)
                    throw new InvalidOperationException("Requesting 'prefabContentsRoot' from Awake and OnEnable is not supported"); // The Prefab stage's m_PrefabContentsRoot is not yet set when we call Awake and OnEnable on user scripts when loading a Prefab
                return m_PrefabContentsRoot;
            }
        }

        public GameObject openedFromInstanceRoot
        {
            get { return m_OpenedFromInstanceRoot; }
        }

        public GameObject openedFromInstanceObject
        {
            get { return m_OpenedFromInstanceObject; }
        }

        public Mode mode
        {
            get { return m_Mode; }
        }

        bool isCurrentStage
        {
            get { return StageUtility.GetCurrentStage() == this; }
        }

        public override ulong GetCombinedSceneCullingMaskForCamera()
        {
            if (m_Mode == Mode.InIsolation)
            {
                return GetSceneCullingMask();
            }
            else if (m_Mode == Mode.InContext)
            {
                var stageHistory = StageNavigationManager.instance.stageHistory;
                if (this == stageHistory.Last())
                {
                    ulong mask = GetSceneCullingMask();
                    int count = stageHistory.Count;
                    for (int i = count - 2; i >= 0; i--)
                    {
                        var stage = stageHistory[i];
                        if (stage)
                        {
                            mask |= stage.GetSceneCullingMask();

                            var prefabStage = stage as PrefabStage;
                            if (prefabStage)
                            {
                                if (prefabStage.mode != Mode.InContext)
                                    break;
                            }
                        }
                    }

                    // Remove the MainStagePrefabInstanceObjectsOpenInPrefabMode bit from the camera's scenecullingmask. By removing that bit we ensure we hide the MainStage Prefab instance
                    // when editing its Prefab Asset in context. Since the MainStage Prefab instance GameObjects have the MainStagePrefabInstanceObjectsOpenInPrefabMode
                    // set when as override-scenecullingmask when entering Prefab Mode in Context for MainStage instances.
                    mask &= ~SceneCullingMasks.MainStagePrefabInstanceObjectsOpenInPrefabMode;
                    return mask;
                }
                else
                {
                    Debug.LogError("We should only call GetOverrideSceneCullingMask() for the current stage");
                    return 0;
                }
            }
            else
            {
                Debug.LogError("Unhandled PrefabStage.Mode");
                return 0;
            }
        }

        internal override Stage GetContextStage()
        {
            if (m_ContextStage != null)
                return m_ContextStage;
            return this;
        }

        internal override Color GetBackgroundColor()
        {
            // Case 1255995 - Workaround for OSX 10.15 driver issue with Intel 630 cards
            Color opaqueBackground = SceneView.kSceneViewPrefabBackground.Color;
            opaqueBackground.a = 1;
            return opaqueBackground;
        }

        public bool IsPartOfPrefabContents(GameObject gameObject)
        {
            if (gameObject == null)
                return false;
            Transform tr = gameObject.transform;
            Transform instanceRootTransform = prefabContentsRoot.transform;
            while (tr != null)
            {
                if (tr == instanceRootTransform)
                    return true;
                tr = tr.parent;
            }
            return false;
        }

        internal override bool showOptionsButton { get { return true; } }

        public override string assetPath { get { return m_PrefabAssetPath; } }

        internal override bool SupportsSaving() { return true; }
        internal override bool hasUnsavedChanges { get { return scene.dirtyID != m_InitialSceneDirtyID; } }

        internal bool showingSavingLabel
        {
            get;
            private set;
        }

        internal Texture2D prefabFileIcon
        {
            get { return m_PrefabFileIcon; }
        }

        string GetPrefabFileName()
        {
            return Path.GetFileNameWithoutExtension(m_PrefabAssetPath);
        }

        internal bool autoSave
        {
            get
            {
                return EditorSettings.prefabModeAllowAutoSave && !m_TemporarilyDisableAutoSave && StageNavigationManager.instance.autoSave;
            }
            set
            {
                if (!EditorSettings.prefabModeAllowAutoSave)
                    return;
                m_TemporarilyDisableAutoSave = false;
                StageNavigationManager.instance.autoSave = value;
            }
        }

        internal bool temporarilyDisableAutoSave
        {
            get { return m_TemporarilyDisableAutoSave || isAssetMissing; }
        }

        internal override bool isValid
        {
            get { return m_PrefabContentsRoot != null && m_PrefabContentsRoot.scene == scene; }
        }

        internal override bool isAssetMissing
        {
            get { return m_IsAssetMissing; }
        }

        void OnPrefabInstanceUpdated(GameObject instance)
        {
            if (mode == Mode.InContext && m_OpenedFromInstanceRoot != null)
            {
                // We check against the outerMostInstanceRoot since that is what will be updated when
                // we change that or any nested prefabs. E.g having PrefabA instance (that have nested PrefabB) in the Scene and we can enter
                // Prefab Mode in Context for prefabB from the scene. Then adding a child; it will then be instanceA that will need to have
                // its objects visibility updated.
                var outerMostInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(m_OpenedFromInstanceRoot);
                bool canHaveChangedCurrentlyHiddenObjects = outerMostInstanceRoot == instance;
                if (canHaveChangedCurrentlyHiddenObjects)
                {
                    SetPrefabInstanceHiddenForInContextEditing(true); // if new GameObjects was added to the instance this will ensure to mark them hidden as well
                }
            }
        }

        void SetPrefabInstanceHiddenForInContextEditing(bool hide)
        {
            // In Playmode all instances have been unpacked so we need to check if the PrefabInstanceHandle have been deleted
            if (m_OpenedFromInstanceRoot != null && PrefabUtility.GetPrefabInstanceHandle(m_OpenedFromInstanceRoot) != null)
                StageUtility.SetPrefabInstanceHiddenForInContextEditing(m_OpenedFromInstanceRoot, hide);
        }

        static List<Canvas> s_ReusableCanvasList = new List<Canvas>();

        bool LoadStage()
        {
            if (scene.IsValid())
            {
                Debug.LogError("LoadStage: we haven't cleared the previous scene before loading a new scene");
                return false;
            }

            if (!File.Exists(m_PrefabAssetPath))
            {
                Debug.LogError("LoadStage: Prefab file not found " + m_PrefabAssetPath);
                return false;
            }

            ReconstructInContextStateIfNeeded();
            if (m_Mode == Mode.InContext && !IsOpenedFromInstanceObjectValid())
            {
                // The Prefab instance used for opening Prefab Mode In Context has become invalid. Opening in Isolation instead.
                SetOpenedFromInstanceObject(null);
                m_ContextStage = null;
                m_Mode = Mode.InIsolation;
            }

            // Ensure scene is set before calling LoadPrefabIntoPreviewScene() so the user can request the current
            // the PrefabStage in their OnEnable and other callbacks (if they use ExecuteInEditMode or ExecuteAlways)
            bool isUIPrefab = PrefabStageUtility.IsUIPrefab(m_PrefabAssetPath);

            switch (m_Mode)
            {
                case Mode.InIsolation:
                    scene = PrefabStageUtility.GetEnvironmentSceneOrEmptyScene(isUIPrefab);
                    break;
                case Mode.InContext:
                    scene = EditorSceneManager.NewPreviewScene();
                    break;
                default:
                    Debug.LogError("Unhandled enum");
                    break;
            }

            m_PrefabContentsRoot = PrefabStageUtility.LoadPrefabIntoPreviewScene(m_PrefabAssetPath, scene);
            if (m_PrefabContentsRoot != null)
            {
                if (isUIPrefab)
                {
                    m_PrefabContentsRoot.GetComponentsInChildren<Canvas>(true, s_ReusableCanvasList);

                    if (m_Mode == Mode.InIsolation && m_PrefabContentsRoot.transform.parent == null)
                        PrefabStageUtility.HandleUIReparentingIfNeeded(m_PrefabContentsRoot, 0);
                }

                m_PrefabFileIcon = DeterminePrefabFileIconFromInstanceRootGameObject();
                m_LastRootTransform = m_PrefabContentsRoot.transform;
                m_InitialSceneDirtyID = scene.dirtyID;
                m_StageDirtiedFired = false;
                UpdateEnvironmentHideFlags();

                if (m_Mode == Mode.InContext)
                {
                    SetPrefabInstanceHiddenForInContextEditing(true);

                    Transform instanceParent = openedFromInstanceRoot.transform.parent;
                    if (instanceParent != null)
                    {
                        // Insert dummy parent which ensure Prefab contents is aligned same as Prefab instance.
                        GameObject dummyParent = new GameObject(PrefabUtility.kDummyPrefabStageRootObjectName);
                        EditorSceneManager.MoveGameObjectToScene(dummyParent, scene);
                        dummyParent.transform.SetParent(m_PrefabContentsRoot.transform.parent, false);

                        // Make Prefab content parent match alignment of Prefab instance parent.
                        // This code doesn't reliably handle cases where the parent matrix is non-orthogonal.
                        dummyParent.transform.localScale = instanceParent.lossyScale;
                        dummyParent.transform.rotation = instanceParent.rotation;
                        dummyParent.transform.position = instanceParent.position;

                        RectTransform instanceRectParent = instanceParent as RectTransform;
                        if (instanceRectParent != null)
                        {
                            RectTransform rect = dummyParent.AddComponent<RectTransform>();
                            rect.sizeDelta = instanceRectParent.rect.size;
                            rect.pivot = instanceRectParent.pivot;
                            Canvas dummyCanvas = dummyParent.AddComponent<Canvas>();
                            Canvas instanceCanvas = openedFromInstanceRoot.GetComponentInParent<Canvas>();
                            if (instanceCanvas != null)
                            {
                                dummyCanvas.sortingOrder = instanceCanvas.sortingOrder;
                                dummyCanvas.referencePixelsPerUnit = instanceCanvas.referencePixelsPerUnit;
                            }
                        }

                        m_PrefabContentsRoot.transform.SetParent(dummyParent.transform, false);
                    }
                }
            }
            else
            {
                // Invalid setup
                CleanupBeforeClosing();
            }

            return isValid;
        }

        // Returns true if opened successfully (this method should only be called from the StageNavigationManager)
        protected internal override bool OnOpenStage()
        {
            if (!isCurrentStage)
            {
                Debug.LogError("Only opening the current PrefabStage is supported. Please report a bug");
                return false;
            }

            bool success = OpenStage();
            if (success)
            {
                var guid = AssetDatabase.AssetPathToGUID(m_PrefabAssetPath);
                m_InitialFileHash = AssetDatabase.GetSourceAssetFileHash(guid);
                if (!FileUtil.ReadFileContentBinary(assetPath, out m_InitialFileContent, out string errorMessage))
                    Debug.LogError($"No undo will be registered for {m_PrefabContentsRoot.name}. \nError: {errorMessage}");
            }

            return success;
        }

        bool OpenStage()
        {
            bool reloading = scene.IsValid();
            if (reloading)
            {
                CleanupBeforeReloading();
            }

            if (LoadStage())
            {
                if (mode == Mode.InContext)
                {
                    RecordPatchedPropertiesForContent();
                    ApplyPatchedPropertiesToContent();
                }
                prefabStageOpened?.Invoke(this);

                // Update environment scene objects after the 'prefabStageOpened' user callback so we can ensure: correct hideflags and
                // that our prefab root is not under a prefab instance (which would mark it as an added object).
                // Note: The user can have reparented and created new GameObjects in the environment scene during this callback.
                EnsureParentOfPrefabRootIsUnpacked();
                UpdateEnvironmentHideFlags();
                UpdateLastPrefabSourceFileHashIfNeeded();

                var sceneHierarchyWindows = SceneHierarchyWindow.GetAllSceneHierarchyWindows();
                foreach (SceneHierarchyWindow sceneHierarchyWindow in sceneHierarchyWindows)
                    sceneHierarchyWindow.FrameObject(prefabContentsRoot.GetInstanceID(), false);

                return true;
            }
            return false;
        }

        protected override void OnCloseStage()
        {
            if (isValid)
                prefabStageClosing?.Invoke(this);

            var initialFileContent = m_InitialFileContent;
            var path = m_PrefabAssetPath;
            var guid = AssetDatabase.AssetPathToGUID(m_PrefabAssetPath);
            var currentPrefabSourceFileHash = AssetDatabase.GetSourceAssetFileHash(guid);

            CleanupBeforeClosing();

            if (initialFileContent?.Length > 0 && currentPrefabSourceFileHash != m_InitialFileHash)
            {
                if (FileUtil.ReadFileContentBinary(path, out byte[] newFileContent, out string errorMessage))
                    Undo.RegisterFileChangeUndo(AssetDatabase.GUIDFromAssetPath(path), initialFileContent, newFileContent);
                else
                    Debug.LogError($"No undo will be registered for {m_PrefabContentsRoot.name}. \nError: {errorMessage}");

            }
        }

        protected internal override void OnReturnToStage()
        {
            if (m_NeedsReloadingWhenReturningToStage)
            {
                m_NeedsReloadingWhenReturningToStage = false;

                if (m_Mode == Mode.InContext && m_OpenedFromInstanceObject == null)
                {
                    // By clearing the contents root this stage becomes invalid which
                    // will be handled the StageNavigationManager by returning to the
                    // main stage
                    m_PrefabContentsRoot = null;
                    return;
                }

                ReloadStage();
            }
        }

        bool HasPatchedPropertyModificationsFor(UnityEngine.Object obj, string partialPropertyName)
        {
            if (m_PatchedProperties == null)
                return false;
            foreach (var patchedProperty in m_PatchedProperties)
            {
                if (patchedProperty.targetInContent == obj && patchedProperty.modification.propertyPath.Contains(partialPropertyName))
                    return true;
            }
            return false;
        }

        internal bool ContainsTransformPrefabPropertyPatchingFor(GameObject[] gameObjects, string partialPropertyName)
        {
            if (gameObjects == null || gameObjects.Length == 0 || m_PatchedProperties == null)
                return false;

            for (int i = 0; i < gameObjects.Length; i++)
                if (HasPatchedPropertyModificationsFor(gameObjects[i].transform, partialPropertyName))
                    return true;

            return false;
        }

        void RecordPatchedPropertiesForContent()
        {
            m_PatchedProperties = new List<PatchedProperty>();

            if (openedFromInstanceRoot == null)
                return;

            if (PrefabUtility.GetPrefabInstanceStatus(openedFromInstanceRoot) != PrefabInstanceStatus.Connected)
                return;

            Dictionary<ulong, UnityEngine.Object> contentObjectsFromFileID = new Dictionary<ulong, UnityEngine.Object>();
            Dictionary<ulong, Transform> instanceTransformsFromFileID = new Dictionary<ulong, Transform>();

            TransformVisitor visitor = new TransformVisitor();

            visitor.VisitAll(prefabContentsRoot.transform, (transform, dict) => {
                contentObjectsFromFileID[Unsupported.GetOrGenerateFileIDHint(transform.gameObject)] = transform.gameObject;
                Component[] components = transform.GetComponents<Component>();
                for (int i = components.Length - 1; i >= 0; i--)
                {
                    if (components[i] == null)
                        continue;
                    contentObjectsFromFileID[Unsupported.GetOrGenerateFileIDHint(components[i])] = components[i];
                }
            }, null);

            visitor.VisitAndAllowEarlyOut(openedFromInstanceRoot.transform, (transform, dict) => {
                // If the GameObject is not hidden in the scene, it's not part of the Prefab instance
                // that we're hiding because we're opening its Asset. That means it's not an object we
                // should use as source for the patching of properties on the Prefab Asset content.
                if (!StageUtility.IsPrefabInstanceHiddenForInContextEditing(transform.gameObject))
                    return false;

                Transform assetTransform = PrefabUtility.GetCorrespondingObjectFromSourceAtPath(transform, assetPath);
                // Added GameObjects have no corresponding asset object
                if (assetTransform != null)
                    instanceTransformsFromFileID[Unsupported.GetLocalIdentifierInFileForPersistentObject(assetTransform)] = transform;
                return true;
            }, null);

            // Note: openedFromInstance is not necessarily a Prefab root, as it might be a nested Prefab.
            var instanceAndCorrespondingObjectChain = new List<(GameObject prefabObject, PropertyModification[] mods)>();
            GameObject prefabObject = openedFromInstanceRoot;
            int numOverrides = 0;
            while (AssetDatabase.GetAssetPath(prefabObject) != assetPath)
            {
                Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(prefabObject));
                PropertyModification[] mods = PrefabUtility.GetPropertyModifications(prefabObject);
                instanceAndCorrespondingObjectChain.Add((prefabObject, mods));
                prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(prefabObject);
                numOverrides += mods.Length;
            }

            // Patching properties is expensive therefore we disable patching when there is an excessive
            // amount of overrides (case 1370904)
            m_TemporarilyDisablePatchAllOverridenProperties = numOverrides >= k_MaxNumberOfOverridesToVisualize;

            bool onlyPatchRootTransform = !showOverrides;

            // Run through same objects, but from innermost out so outer overrides are applied last.
            for (int i = instanceAndCorrespondingObjectChain.Count - 1; i >= 0; i--)
            {
                prefabObject = instanceAndCorrespondingObjectChain[i].prefabObject;
                var mods = instanceAndCorrespondingObjectChain[i].mods;

                UnityEngine.Object lastTarget = null;
                UnityEngine.Object lastTargetInContent = null;
                SerializedObject lastInstanceTransformSO = null;
                foreach (PropertyModification mod in mods)
                {
                    if (mod.target == null)
                        continue;

                    if (onlyPatchRootTransform && !(mod.target is Transform))
                        continue;

                    UnityEngine.Object targetInContent = null;
                    SerializedObject instanceTransformSO = null;

                    // Optimization to avoid doing work to find matching object in Prefab contents
                    // for a target we already saw. Performs best if modifications are sorted by target.
                    if (mod.target == lastTarget)
                    {
                        targetInContent = lastTargetInContent;
                        instanceTransformSO = lastInstanceTransformSO;
                    }
                    else
                    {
                        targetInContent = null;
                        UnityEngine.Object targetInOpenAsset = PrefabUtility.GetCorrespondingObjectFromSourceAtPath(mod.target, assetPath);
                        if (targetInOpenAsset != null)
                        {
                            ulong fileID = Unsupported.GetLocalIdentifierInFileForPersistentObject(targetInOpenAsset);
                            contentObjectsFromFileID.TryGetValue(fileID, out targetInContent);
                            Transform transform;
                            if (instanceTransformsFromFileID.TryGetValue(fileID, out transform))
                            {
                                instanceTransformSO = new SerializedObject(transform);
                            }
                        }
                        lastTarget = mod.target;
                        lastTargetInContent = targetInContent;
                        lastInstanceTransformSO = instanceTransformSO;
                    }

                    if (targetInContent != null)
                    {
                        // Don't patch root name, as Prefab Mode doesn't support changing that.
                        if (targetInContent == prefabContentsRoot && mod.propertyPath == "m_Name")
                            continue;

                        if (onlyPatchRootTransform && targetInContent != prefabContentsRoot.transform)
                            continue;

                        if (instanceTransformSO != null)
                        {
                            // If properties on a Prefab instance are driven, they are serialized as 0,
                            // which means their Prefab override values will be 0 as well.
                            // Patching over values of 0 for Prefab Mode in Context is no good,
                            // so get the actual current value from the instance instead.
                            PropertyModification modFromValue = instanceTransformSO.ExtractPropertyModification(mod.propertyPath);
                            if (modFromValue != null)
                                mod.value = modFromValue.value;
                        }

                        DrivenPropertyManager.TryRegisterProperty(this, targetInContent, mod.propertyPath);
                        m_PatchedProperties.Add(new PatchedProperty() { modification = mod, targetInContent = targetInContent });
                    }
                }
            }

            // Apply patched properties from outer stage where applicable.
            // This is so that changes that were patched when going in Prefab Mode in Context
            // don't suddenly change when digging in deeper into nested Prefab or Variant bases.
            var stageHistory = StageNavigationManager.instance.stageHistory;
            int selfIndex = stageHistory.IndexOf(this);
            if (selfIndex >= 1)
            {
                PrefabStage previous = stageHistory[selfIndex - 1] as PrefabStage;
                if (previous != null && previous.mode == Mode.InContext && previous.m_PatchedProperties != null)
                {
                    List<PatchedProperty> previousPatchedProperties = previous.m_PatchedProperties;
                    UnityEngine.Object lastTarget = null;
                    UnityEngine.Object lastTargetInContent = null;
                    for (int i = previousPatchedProperties.Count - 1; i >= 0; i--)
                    {
                        PatchedProperty patch = previousPatchedProperties[i];
                        PropertyModification mod = patch.modification;
                        if (mod.target == null)
                            continue;

                        UnityEngine.Object targetInContent = null;
                        if (mod.target == lastTarget)
                        {
                            targetInContent = lastTargetInContent;
                        }
                        else
                        {
                            targetInContent = null;
                            UnityEngine.Object targetInOpenAsset = PrefabUtility.GetCorrespondingObjectFromSourceAtPath(mod.target, assetPath);
                            if (targetInOpenAsset != null)
                            {
                                ulong fileID = Unsupported.GetLocalIdentifierInFileForPersistentObject(targetInOpenAsset);
                                contentObjectsFromFileID.TryGetValue(fileID, out targetInContent);
                            }
                            lastTarget = mod.target;
                            lastTargetInContent = targetInContent;
                        }

                        if (targetInContent != null)
                        {
                            // Don't patch root name, as Prefab Mode doesn't support changing that.
                            // Root can be a different object than it was in outer Stage, så check again here.
                            if (targetInContent == prefabContentsRoot && mod.propertyPath == "m_Name")
                                continue;

                            // Root can be a different object than it was in outer Stage, så check again here.
                            if (onlyPatchRootTransform && targetInContent != prefabContentsRoot.transform)
                                continue;

                            DrivenPropertyManager.TryRegisterProperty(this, targetInContent, mod.propertyPath);
                            m_PatchedProperties.Add(new PatchedProperty() { modification = mod, targetInContent = targetInContent });
                        }
                    }
                }
            }
        }

        void ApplyPatchedPropertiesToContent()
        {
            if (m_PatchedProperties.Count == 0)
                return;

            var modificationsPerTargetInContent = new Dictionary<UnityEngine.Object, List<PropertyModification>>();
            UnityEngine.Object lastTargetInContent = null;
            int lastTargetID = 0;
            for (int i = m_PatchedProperties.Count - 1; i >= 0; i--)
            {
                PatchedProperty patchedProperty = m_PatchedProperties[i];
                PropertyModification modification = patchedProperty.modification;
                UnityEngine.Object targetInContent = patchedProperty.targetInContent;

                // If an object was destroyed, but later recreated with undo or redo, we have to recreate the reference to it.
                if (targetInContent == null)
                {
                    // Patched properties are grouped by targetInContent, so we can easily ensure we
                    // only make one attempt per target just by comparing with the previous target.
                    int targetID = targetInContent.GetInstanceID();
                    if (targetID == lastTargetID)
                        targetInContent = lastTargetInContent;
                    else
                        targetInContent = UnityEditorInternal.InternalEditorUtility.GetObjectFromInstanceID(targetID);

                    // Assign the struct back to the array if it worked.
                    if (targetInContent != null)
                    {
                        patchedProperty.targetInContent = targetInContent;
                        m_PatchedProperties[i] = patchedProperty;
                    }
                    lastTargetInContent = targetInContent;
                    lastTargetID = targetID;
                }

                // Group modifications per target for batch apply
                if (targetInContent != null)
                {
                    List<PropertyModification> list;
                    if (!modificationsPerTargetInContent.TryGetValue(targetInContent, out list))
                    {
                        list = new List<PropertyModification>();
                        modificationsPerTargetInContent[targetInContent] = list;
                    }
                    list.Add(modification);
                }


                // If GameObject.active is an override, applying the value via PropertyModification
                // is not sufficient to actually change active state of the object,
                // nor is calling AwakeFromLoad(kAnimationAwakeFromLoad) afterwards,
                // so explicitly set it here.
                GameObject targetGameObject = targetInContent as GameObject;
                if (targetGameObject != null && modification.propertyPath == "m_IsActive")
                    targetGameObject.SetActive(modification.value != "0");
            }

            // Batch apply modifications per object (to optimize for large objects, case 1370904)
            foreach (var kvp in modificationsPerTargetInContent)
            {
                var targetInContent = kvp.Key;
                var mods = kvp.Value;
                PropertyModification.ApplyPropertyModificationsToObject(targetInContent, mods.ToArray());
            }

            StageUtility.CallAwakeFromLoadOnSubHierarchy(m_PrefabContentsRoot);
        }

        void UndoRedoPerformed(in UndoRedoInfo info)
        {
            if (m_Mode == Mode.InContext)
            {
                ApplyPatchedPropertiesToContent();
            }
        }

        void CleanupBeforeReloading()
        {
            // Only clear state that is being reloaded but keep InContext editing state etc
            PrefabStageUtility.DestroyPreviewScene(scene);
            m_HideFlagUtility = null;
        }

        void CleanupBeforeClosing()
        {
            if (m_Mode == Mode.InContext)
            {
                SetPrefabInstanceHiddenForInContextEditing(false);
            }

            if (m_PrefabContentsRoot != null && m_PrefabContentsRoot.scene != scene)
            {
                UnityEngine.Object.DestroyImmediate(m_PrefabContentsRoot);
            }

            if (scene.IsValid())
                PrefabStageUtility.DestroyPreviewScene(scene); // Automatically deletes all GameObjects in scene

            m_PrefabContentsRoot = null;
            m_OpenedFromInstanceRoot = null;
            m_OpenedFromInstanceObject = null;
            m_HideFlagUtility = null;
            m_PrefabAssetPath = null;
            m_InitialFileContent = null;
            m_InitialFileHash = new Hash128();
            m_InitialSceneDirtyID = 0;
            m_StageDirtiedFired = false;
            m_LastSceneDirtyID = 0;
            m_IgnoreNextAssetImportedEventForCurrentPrefab = false;
            m_PrefabWasChangedOnDisk = false;
        }

        void ReloadStage()
        {
            if (SceneHierarchy.s_DebugPrefabStage)
                Debug.Log("RELOADING Prefab at " + m_PrefabAssetPath);

            if (!isCurrentStage)
            {
                Debug.LogError("Only reloading the current PrefabStage is supported. Please report a bug");
                return;
            }

            var sceneHierarchyWindows = SceneHierarchyWindow.GetAllSceneHierarchyWindows();
            foreach (SceneHierarchyWindow sceneHierarchyWindow in sceneHierarchyWindows)
                SaveHierarchyState(sceneHierarchyWindow);

            if (OpenStage())
            {
                foreach (SceneView sceneView in SceneView.sceneViews)
                    SyncSceneViewToStage(sceneView);

                foreach (SceneHierarchyWindow sceneHierarchyWindow in sceneHierarchyWindows)
                {
                    SyncSceneHierarchyToStage(sceneHierarchyWindow);
                    LoadHierarchyState(sceneHierarchyWindow);
                }

                prefabStageReloaded?.Invoke(this);
            }

            if (SceneHierarchy.s_DebugPrefabStage)
                Debug.Log("RELOADING done");
        }

        internal override void SyncSceneViewToStage(SceneView sceneView)
        {
            // The reason we need to set customScene even though we also set overrideSceneCullingMask is
            // because the RenderSettings of the customScene is used to override lighting settings for this SceneView.
            PrefabStage prefabStage = GetContextStage() as PrefabStage;
            sceneView.customScene = prefabStage == null ? default(Scene) : prefabStage.scene;
            sceneView.customParentForNewGameObjects = prefabContentsRoot.transform;
            switch (mode)
            {
                case PrefabStage.Mode.InIsolation:
                    sceneView.overrideSceneCullingMask = 0;
                    break;
                case PrefabStage.Mode.InContext:
                    sceneView.overrideSceneCullingMask = GetCombinedSceneCullingMaskForCamera();
                    break;
                default:
                    Debug.LogError("Unhandled enum");
                    break;
            }
        }

        internal override void SyncSceneHierarchyToStage(SceneHierarchyWindow sceneHierarchyWindow)
        {
            var sceneHierarchy = sceneHierarchyWindow.sceneHierarchy;
            sceneHierarchy.customScenes = new[] { scene };
            sceneHierarchy.customParentForNewGameObjects = prefabContentsRoot.transform;
            sceneHierarchy.SetCustomDragHandler(PrefabModeDraggingHandler);
        }

        protected internal override void OnFirstTimeOpenStageInSceneView(SceneView sceneView)
        {
            // Default to scene view lighting if scene itself does not have any lights
            if (!HasAnyActiveLights(scene))
                sceneView.sceneLighting = false;

            // Default to not showing skybox if user did not specify a custom environment scene.
            if (string.IsNullOrEmpty(scene.path))
                sceneView.sceneViewState.showSkybox = false;

            // For UI to frame properly we need to delay one full Update for the layouting to have been processed
            EditorApplication.update += DelayedFraming;
        }

        internal override void OnFirstTimeOpenStageInSceneHierachyWindow(SceneHierarchyWindow sceneHierarchyWindow)
        {
            var expandedIDs = new List<int>();
            AddParentsBelowButIgnoreNestedPrefabsRecursive(prefabContentsRoot.transform, expandedIDs);
            expandedIDs.Sort();
            sceneHierarchyWindow.sceneHierarchy.treeViewState.expandedIDs = expandedIDs;
        }

        void AddParentsBelowButIgnoreNestedPrefabsRecursive(Transform transform, List<int> gameObjectInstanceIDs)
        {
            gameObjectInstanceIDs.Add(transform.gameObject.GetInstanceID());

            int count = transform.childCount;
            for (int i = 0; i < count; ++i)
            {
                var child = transform.GetChild(i);
                if (child.childCount > 0 && !PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject))
                {
                    AddParentsBelowButIgnoreNestedPrefabsRecursive(child, gameObjectInstanceIDs);
                }
            }
        }

        static DragAndDropVisualMode PrefabModeDraggingHandler(GameObjectTreeViewItem parentItem, GameObjectTreeViewItem targetItem, TreeViewDragging.DropPosition dropPos, bool perform)
        {
            var prefabStage = StageNavigationManager.instance.currentStage as PrefabStage;
            if (prefabStage == null)
                throw new InvalidOperationException("PrefabModeDraggingHandler should only be called in Prefab Mode");

            // Disallow dropping as sibling to the prefab instance root (In Prefab Mode we only want to show one root).
            if (parentItem != null && parentItem.parent == null && dropPos != TreeViewDragging.DropPosition.Upon)
                return DragAndDropVisualMode.Rejected;

            // Disallow dragging scenes into the hierarchy when it is in Prefab Mode (we do not support multi-scenes for prefabs yet)
            foreach (var dragged in DragAndDrop.objectReferences)
            {
                if (dragged is SceneAsset)
                    return DragAndDropVisualMode.Rejected;
            }

            // Check for cyclic nesting (only on perform since it is an expensive operation)
            if (perform)
            {
                var prefabAssetThatIsAddedTo = AssetDatabase.LoadMainAssetAtPath(prefabStage.assetPath);

                if (prefabAssetThatIsAddedTo is BrokenPrefabAsset)
                    return DragAndDropVisualMode.None;

                foreach (var dragged in DragAndDrop.objectReferences)
                {
                    if (dragged is GameObject && EditorUtility.IsPersistent(dragged))
                    {
                        var prefabAssetThatWillBeAdded = dragged;
                        if (PrefabUtility.CheckIfAddingPrefabWouldResultInCyclicNesting(prefabAssetThatIsAddedTo, prefabAssetThatWillBeAdded))
                        {
                            PrefabUtility.ShowCyclicNestingWarningDialog();
                            return DragAndDropVisualMode.Rejected;
                        }
                    }
                }
            }

            return DragAndDropVisualMode.None;
        }

        static bool HasAnyActiveLights(Scene scene)
        {
            foreach (var gameObject in scene.GetRootGameObjects())
            {
                if (gameObject.GetComponentsInChildren<Light>(false).Length > 0)
                    return true;
            }

            return false;
        }

        int m_DelayCounter;
        void DelayedFraming()
        {
            if (m_DelayCounter++ == 1)
            {
                EditorApplication.update -= DelayedFraming;
                m_DelayCounter = 0;

                // Frame the Prefab
                Selection.activeGameObject = prefabContentsRoot;
                foreach (SceneView sceneView in SceneView.sceneViews)
                    sceneView.FrameSelected(false, true);
            }
        }

        internal override string GetErrorMessage()
        {
            if (m_PrefabContentsRoot == null)
                return L10n.Tr("Error: The Prefab contents root has been deleted.\n\nPrefab: ") + m_PrefabAssetPath;

            if (m_PrefabContentsRoot.scene != scene)
                return L10n.Tr("Error: The root GameObject of the opened Prefab has been moved out of the Prefab Stage scene by a script.\n\nPrefab: ") + m_PrefabAssetPath;

            return null;
        }

        protected internal override GUIContent CreateHeaderContent()
        {
            var label = Path.GetFileNameWithoutExtension(m_PrefabAssetPath);
            var icon = AssetDatabase.GetCachedIcon(m_PrefabAssetPath);
            return new GUIContent(label, icon);
        }

        internal override BreadcrumbBar.Item CreateBreadcrumbItem()
        {
            GUIContent content = CreateHeaderContent();

            var history = StageNavigationManager.instance.stageHistory;
            bool isLastCrumb = this == history.Last();
            var style = isLastCrumb ? BreadcrumbBar.DefaultStyles.labelBold : BreadcrumbBar.DefaultStyles.label;
            var separatorstyle = mode == Mode.InIsolation ? BreadcrumbBar.SeparatorStyle.Line : BreadcrumbBar.SeparatorStyle.Arrow;
            if (isAssetMissing)
            {
                style = isLastCrumb ? BreadcrumbBar.DefaultStyles.labelBoldMissing : BreadcrumbBar.DefaultStyles.labelMissing;
                content.tooltip = L10n.Tr("Prefab Asset has been deleted.");
            }

            return new BreadcrumbBar.Item
            {
                content = content,
                guistyle = style,
                userdata = this,
                separatorstyle = separatorstyle
            };
        }

        internal override void Tick()
        {
            if (!isValid)
                return;

            if (hasUnsavedChanges)
            {
                m_AnalyticsDidUserModify = true;

                if (!m_StageDirtiedFired)
                {
                    m_StageDirtiedFired = true;
                    if (prefabStageDirtied != null)
                        prefabStageDirtied(this);
                }
            }

            UpdateEnvironmentHideFlagsIfNeeded();
            HandleAutoSave();
            HandlePrefabChangedOnDisk();
            DetectSceneDirtinessChange();
            DetectPrefabFileIconChange();
            DetectPrefabRootTransformChange();
        }

        void DetectPrefabRootTransformChange()
        {
            var currentTransform = m_PrefabContentsRoot.transform;
            if (currentTransform != m_LastRootTransform)
            {
                foreach (SceneView sceneView in SceneView.sceneViews)
                    SyncSceneViewToStage(sceneView);

                var sceneHierarchyWindows = SceneHierarchyWindow.GetAllSceneHierarchyWindows();
                foreach (SceneHierarchyWindow sceneHierarchyWindow in sceneHierarchyWindows)
                    SyncSceneHierarchyToStage(sceneHierarchyWindow);
            }
            m_LastRootTransform = currentTransform;
        }

        void DetectPrefabFileIconChange()
        {
            var icon = DeterminePrefabFileIconFromInstanceRootGameObject();
            if (icon != m_PrefabFileIcon)
            {
                m_PrefabFileIcon = icon;
                SceneView.RebuildBreadcrumbBarInAll();
                SceneHierarchyWindow.RebuildStageHeaderInAll();
            }
        }

        void DetectSceneDirtinessChange()
        {
            if (scene.dirtyID != m_LastSceneDirtyID)
                SceneView.RepaintAll();
            m_LastSceneDirtyID = scene.dirtyID;
        }

        void UpdateEnvironmentHideFlagsIfNeeded()
        {
            if (m_HideFlagUtility == null)
                m_HideFlagUtility = new HideFlagUtility(scene, m_PrefabContentsRoot);
            m_HideFlagUtility.UpdateEnvironmentHideFlagsIfNeeded();
        }

        void UpdateEnvironmentHideFlags()
        {
            if (m_HideFlagUtility == null)
                m_HideFlagUtility = new HideFlagUtility(scene, m_PrefabContentsRoot);
            m_HideFlagUtility.UpdateEnvironmentHideFlags();
        }

        void EnsureParentOfPrefabRootIsUnpacked()
        {
            var parent = m_PrefabContentsRoot.transform.parent;
            if (parent != null)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(parent))
                {
                    var outerMostPrefabInstance = PrefabUtility.GetOutermostPrefabInstanceRoot(parent);
                    PrefabUtility.UnpackPrefabInstance(outerMostPrefabInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
            }
        }

        bool isTextFieldCaretShowing
        {
            get { return EditorGUI.IsEditingTextField() && !EditorGUIUtility.textFieldHasSelection; }
        }

        bool readyToAutoSave
        {
            get { return m_PrefabContentsRoot != null && hasUnsavedChanges && GUIUtility.hotControl == 0 && !isTextFieldCaretShowing && !EditorApplication.isCompiling; }
        }

        void HandleAutoSave()
        {
            if (IsPrefabInImmutableFolder())
                return;

            if (autoSave && readyToAutoSave)
            {
                AutoSave();
            }
        }

        void HandlePrefabChangedOnDisk()
        {
            if (m_PrefabWasChangedOnDisk)
            {
                m_PrefabWasChangedOnDisk = false;

                if (!isCurrentStage)
                {
                    return;
                }

                if (!File.Exists(m_PrefabAssetPath))
                    return;

                if (hasUnsavedChanges)
                {
                    var title = L10n.Tr("Prefab Has Been Changed on Disk");
                    var message = string.Format(L10n.Tr("You have modifications to the Prefab '{0}' that was changed on disk while in Prefab Mode. Do you want to keep your changes or reload the Prefab and discard your changes?"), m_PrefabContentsRoot.name);
                    bool keepChanges = EditorUtility.DisplayDialog(title, message, L10n.Tr("Keep Changes"), L10n.Tr("Discard Changes"));
                    if (!keepChanges)
                        ReloadStage();
                }
                else
                {
                    ReloadStage();
                }
            }
        }

        public void ClearDirtiness()
        {
            EditorSceneManager.ClearSceneDirtiness(scene);
            m_InitialSceneDirtyID = scene.dirtyID;
            m_StageDirtiedFired = false;
        }

        bool PromptIfMissingVariantParentForVariant()
        {
            if (PrefabUtility.IsPrefabAssetMissing(m_PrefabContentsRoot))
            {
                string title = L10n.Tr("Saving Variant Failed");
                string message = L10n.Tr("Can't save the Prefab Variant when its parent Prefab is missing. You have to unpack the root GameObject or recover the missing parent Prefab in order to save the Prefab Variant");
                if (autoSave)
                    message += L10n.Tr("\n\nAuto Save has been temporarily disabled.");
                EditorUtility.DisplayDialog(title, message, L10n.Tr("OK"));
                m_TemporarilyDisableAutoSave = true;
                return true;
            }
            return false;
        }

        // Returns true if we can continue. Either the user saved changes or user discarded changes. Returns false if the user cancelled switching stage.
        internal override bool AskUserToSaveModifiedStageBeforeSwitchingStage()
        {
            // Allow user to save any unsaved changes only after recompiling have finished so any new scripts can be
            // saved properly to the Prefab file (but only if the stage is valid)
            if (isValid)
            {
                if (EditorApplication.isCompiling && hasUnsavedChanges)
                {
                    SceneView.ShowNotification("Compiling must finish before you can exit Prefab Mode");
                    SceneView.RepaintAll();
                    return false;
                }

                bool continueDestroyingScene = AskUserToSaveDirtySceneBeforeDestroyingScene();
                if (!continueDestroyingScene)
                    return false;
            }

            return true;
        }

        // Returns true if saved succesfully (internal so we can use it in Tests)
        internal bool SavePrefab()
        {
            if (!isValid)
                return false;

            m_AnalyticsDidUserSave = true;
            m_AnalyticsDidUserModify = true;

            if (SceneHierarchy.s_DebugPrefabStage)
                Debug.Log("SAVE PREFAB");

            if (prefabSaving != null)
                prefabSaving(m_PrefabContentsRoot);

            var startTime = EditorApplication.timeSinceStartup;

            if (PromptIfMissingVariantParentForVariant())
                return false;

            // The user can have deleted required folders
            var folder = Path.GetDirectoryName(m_PrefabAssetPath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            bool savedSuccesfully;
            PrefabUtility.SaveAsPrefabAsset(m_PrefabContentsRoot, m_PrefabAssetPath, out savedSuccesfully);
            m_LastSavingDuration = (float)(EditorApplication.timeSinceStartup - startTime);

            if (savedSuccesfully)
            {
                ClearDirtiness();

                if (prefabSaved != null)
                    prefabSaved(m_PrefabContentsRoot);

                // After saving the Prefab any Prefab instances in the environment scene
                // that are dependent on the saved Prefab will have lost their hideflags
                // so here we set them again.
                UpdateEnvironmentHideFlags();
            }
            else
            {
                string title = L10n.Tr("Saving Failed");
                string message = L10n.Tr("Saving failed. Check the Console window to get more insight into what needs to be fixed.");
                if (autoSave)
                    message += L10n.Tr("\n\nAuto Save has been temporarily disabled.");
                EditorUtility.DisplayDialog(title, message, L10n.Tr("OK"));

                m_TemporarilyDisableAutoSave = true;
                m_IgnoreNextAssetImportedEventForCurrentPrefab = false;
            }

            if (SceneHierarchy.s_DebugPrefabStage)
                Debug.Log("SAVE PREFAB ended");

            showingSavingLabel = false;

            return savedSuccesfully;
        }

        internal bool SaveAsNew(string newPath, bool asCopy)
        {
            if (!isValid)
                return false;

            if (newPath == m_PrefabAssetPath)
            {
                throw new ArgumentException("Cannot save as new prefab using the same path");
            }

            if (asCopy)
            {
                // Keep current open prefab and save copy at newPath
                string oldName = m_PrefabContentsRoot.name;
                m_PrefabContentsRoot.name = Path.GetFileNameWithoutExtension(newPath);
                PrefabUtility.SaveAsPrefabAsset(m_PrefabContentsRoot, newPath);
                m_PrefabContentsRoot.name = oldName;
            }
            else
            {
                // Change the current open prefab and save
                m_PrefabContentsRoot.name = Path.GetFileNameWithoutExtension(newPath);
                m_PrefabAssetPath = newPath;
                CachePrefabFolderInfo();
                ClearDirtiness();
                PrefabUtility.SaveAsPrefabAsset(m_PrefabContentsRoot, newPath);

                if (prefabStageSavedAsNewPrefab != null)
                    prefabStageSavedAsNewPrefab(this);
            }

            return true;
        }

        internal override bool SaveAsNew()
        {
            Assert.IsTrue(m_PrefabContentsRoot != null, "We should have a valid m_PrefabContentsRoot when saving to prefab asset");

            string directoryOfCurrentPrefab = Path.GetDirectoryName(m_PrefabAssetPath);
            string nameOfCurrentPrefab = Path.GetFileNameWithoutExtension(m_PrefabAssetPath);
            string relativePath;
            while (true)
            {
                relativePath = EditorUtility.SaveFilePanelInProject("Save Prefab", nameOfCurrentPrefab + " Copy", "prefab", "", directoryOfCurrentPrefab);

                // Cancel pressed
                if (string.IsNullOrEmpty(relativePath))
                    return false;

                if (relativePath == m_PrefabAssetPath)
                {
                    if (EditorUtility.DisplayDialog(L10n.Tr("Save Prefab has failed"), L10n.Tr("Overwriting the same path as another open prefab is not allowed."), L10n.Tr("Try Again"), L10n.Tr("Cancel")))
                        continue;

                    return false;
                }
                break;
            }

            return SaveAsNew(relativePath, false);
        }

        void PerformDelayedAutoSave()
        {
            EditorApplication.update -= PerformDelayedAutoSave;
            Save();
        }

        void AutoSave()
        {
            showingSavingLabel = m_LastSavingDuration > kDurationBeforeShowingSavingBadge;
            if (showingSavingLabel)
            {
                // Save delayed if we want to show the save badge while saving.
                foreach (SceneView sceneView in SceneView.sceneViews)
                    sceneView.Repaint();

                EditorApplication.update += PerformDelayedAutoSave;
            }
            else
            {
                // Save directly if we don't want to show the saving badge
                Save();
            }
        }

        // Returns true if prefab was saved.
        internal override bool Save()
        {
            if (m_PrefabContentsRoot == null)
            {
                Debug.LogError("We should have a valid m_PrefabContentsRoot when saving to prefab asset");
                return false;
            }

            if (!PrefabUtility.PromptAndCheckoutPrefabIfNeeded(m_PrefabAssetPath, PrefabUtility.SaveVerb.Save))
            {
                // If user doesn't want to check out prefab asset, or it cannot be,
                // it doesn't make sense to keep auto save on.
                m_TemporarilyDisableAutoSave = true;
                return false;
            }

            bool showCancelButton = !autoSave;
            if (!CheckRenamedPrefabRootWhenSaving(showCancelButton))
                return false;

            return SavePrefab();
        }

        internal override void DiscardChanges()
        {
            if (hasUnsavedChanges)
                ReloadStage();
        }

        // Returns true if we should continue saving
        bool CheckRenamedPrefabRootWhenSaving(bool showCancelButton)
        {
            var prefabFilename = GetPrefabFileName();
            if (m_PrefabContentsRoot.name != prefabFilename)
            {
                string folder = Path.GetDirectoryName(m_PrefabAssetPath);
                string extension = Path.GetExtension(m_PrefabAssetPath);
                string newPath = Path.Combine(folder, m_PrefabContentsRoot.name + extension);
                string errorMsg = AssetDatabase.ValidateMoveAsset(m_PrefabAssetPath, newPath);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    var t = L10n.Tr("Rename Prefab File Not Possible");
                    var m = string.Format(L10n.Tr("The Prefab file name must match the Prefab root GameObject name but there is already a Prefab asset with the file name '{0}' in the same folder. The root GameObject name will therefore be changed back to match the Prefab file name when saving."), m_PrefabContentsRoot.name);

                    if (showCancelButton)
                    {
                        if (EditorUtility.DisplayDialog(t, m, L10n.Tr("OK"), L10n.Tr("Cancel Save")))
                        {
                            RenameInstanceRootToMatchPrefabFile();
                            return true;
                        }
                        else
                        {
                            return false; // Cancel saving
                        }
                    }

                    EditorUtility.DisplayDialog(t, m, L10n.Tr("OK"));
                    RenameInstanceRootToMatchPrefabFile();
                    return true;
                }

                var title = L10n.Tr("Rename Prefab File?");
                var message = string.Format(L10n.Tr("The Prefab file name must match the Prefab root GameObject name. Do you want to rename the file to '{0}' or use the old name '{1}' for both?"), m_PrefabContentsRoot.name, prefabFilename);

                if (showCancelButton)
                {
                    int option = EditorUtility.DisplayDialogComplex(title, message, L10n.Tr("Rename File"), L10n.Tr("Use Old Name"), L10n.Tr("Cancel Save"));
                    switch (option)
                    {
                        // Rename prefab file
                        case 0:
                            RenamePrefabFileToMatchPrefabInstanceName();
                            return true;
                        // Rename the root GameObject to file name
                        case 1:
                            RenameInstanceRootToMatchPrefabFile();
                            return true;
                        // Cancel saving
                        case 2:
                            return false;
                    }
                }
                else
                {
                    bool renameFile = EditorUtility.DisplayDialog(title, message, L10n.Tr("Rename File"), L10n.Tr("Use Old Name"));
                    if (renameFile)
                        RenamePrefabFileToMatchPrefabInstanceName();
                    else
                        RenameInstanceRootToMatchPrefabFile();
                    return true;
                }
            }
            return true;
        }

        internal void RenameInstanceRootToMatchPrefabFile()
        {
            m_PrefabContentsRoot.name = GetPrefabFileName();
        }

        internal void RenamePrefabFileToMatchPrefabInstanceName()
        {
            string newPrefabFileName = m_PrefabContentsRoot.name;

            if (SceneHierarchy.s_DebugPrefabStage)
                Debug.LogFormat("RENAME Prefab Asset: '{0} to '{1}'", m_PrefabAssetPath, newPrefabFileName);

            // If we don't ignore the next import event we will reload the current prefab and hereby loosing the current selection (inspector goes blank)
            // Since we have made the change ourselves (rename) we don't need to reload our instances.
            m_IgnoreNextAssetImportedEventForCurrentPrefab = true;

            string errorMsg = AssetDatabase.RenameAsset(m_PrefabAssetPath, newPrefabFileName);
            if (!string.IsNullOrEmpty(errorMsg))
                Debug.LogError(errorMsg);

            if (SceneHierarchy.s_DebugPrefabStage)
                Debug.Log("RENAME done");
        }

        // Returns false if the user clicked Cancel to save otherwise returns true (ok to continue to destroying scene)
        internal bool AskUserToSaveDirtySceneBeforeDestroyingScene()
        {
            if (!hasUnsavedChanges || m_PrefabContentsRoot == null)
                return true; // no changes to save or no root to save; continue

            if (IsPrefabInImmutableFolder())
            {
                var header = L10n.Tr("Immutable Prefab");
                var message = L10n.Tr("The Prefab was changed in Prefab Mode but is in a read-only folder so the changes cannot be saved.");
                var buttonOK = L10n.Tr("OK");
                var buttonCancel = L10n.Tr("Cancel");

                if (EditorUtility.DisplayDialog(header, message, buttonOK, buttonCancel))
                    return true; // OK: continue to close stage
                return false; // Cancel closing stage
            }

            // Rare condition. Prefab should have already been saved if auto-save is enabled,
            // but it's possible it hasn't, so save when exiting just in case.
            if (autoSave)
                return Save();

            int dialogResult = EditorUtility.DisplayDialogComplex("Prefab Has Been Modified", "Do you want to save the changes you made in Prefab Mode? Your changes will be lost if you don't save them.", "Save", "Discard Changes", "Cancel");
            switch (dialogResult)
            {
                case 0:
                    return Save(); // save changes and continue current operation

                case 1:
                    // The user have accepted to discard changes
                    if (hasUnsavedChanges && !m_IsAssetMissing)
                        ReloadStage();
                    return true; // continue current operation

                case 2:
                    return false; // cancel and discontinue current operation

                default:
                    throw new InvalidOperationException("Unhandled dialog result " + dialogResult);
            }
        }

        internal void OnSavingPrefab(GameObject gameObject, string path)
        {
            // We care about 'irrelevant prefab paths' since the 'irrelevant' prefab could be a nested prefab to the
            // current prefab being edited (this would result in our current prefab being reimported due to the dependency).
            // For nested prefabs the prefab merging code will take care of updating the current GameObjects loaded
            // so in this case do not reload the prefab even though the path shows up in AssetsChangedOnHDD event.

            bool savedBySelf = gameObject == m_PrefabContentsRoot;
            bool irrelevantPath = path != m_PrefabAssetPath;
            if (savedBySelf || irrelevantPath)
            {
                m_IgnoreNextAssetImportedEventForCurrentPrefab = true;
            }
        }

        bool UpdateLastPrefabSourceFileHashIfNeeded()
        {
            var guid = AssetDatabase.AssetPathToGUID(m_PrefabAssetPath);
            var prefabSourceFileHash = AssetDatabase.GetSourceAssetFileHash(guid);
            if (m_LastPrefabSourceFileHash != prefabSourceFileHash)
            {
                m_LastPrefabSourceFileHash = prefabSourceFileHash;
                return true;
            }
            return false;
        }

        internal void OnAssetsChangedOnHDD(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (SceneHierarchy.s_DebugPrefabStage)
                Debug.LogFormat("AssetDatabase Event: Current Prefab: {0}, *ImportedAssets: {1}, *DeletedAssets: {2}, *MovedAssets: {3}, *MovedFromAssetPaths: {4}", m_PrefabAssetPath, DebugUtils.ListToString(importedAssets), DebugUtils.ListToString(deletedAssets), DebugUtils.ListToString(movedAssets), DebugUtils.ListToString(movedFromAssetPaths));

            // Prefab was moved (update cached path)
            for (int i = 0; i < movedFromAssetPaths.Length; ++i)
            {
                if (movedFromAssetPaths[i].Equals(m_PrefabAssetPath, StringComparison.OrdinalIgnoreCase))
                {
                    m_PrefabAssetPath = movedAssets[i];
                    break;
                }
            }

            for (int i = 0; i < deletedAssets.Length; ++i)
            {
                if (deletedAssets[i] == m_PrefabAssetPath)
                {
                    m_IsAssetMissing = true;
                    break;
                }
            }

            // Detect if our Prefab was modified on HDD outside Prefab Mode (in that case we should ask the user if he wants to reload it)
            for (int i = 0; i < importedAssets.Length; ++i)
            {
                if (importedAssets[i] == m_PrefabAssetPath)
                {
                    m_IsAssetMissing = false;
                    if (UpdateLastPrefabSourceFileHashIfNeeded() && !m_IgnoreNextAssetImportedEventForCurrentPrefab)
                    {
                        if (isCurrentStage)
                        {
                            m_PrefabWasChangedOnDisk = true;
                        }
                        else
                        {
                            m_NeedsReloadingWhenReturningToStage = true;
                        }
                    }

                    // Reset the ignore flag when we finally have imported the saved prefab (We set this flag when saving the Prefab from Prefab Mode)
                    // Note we can get multiple OnAssetsChangedOnHDD events before the Prefab imported event if e.g folders of the Prefab path needs to be reimported first.
                    m_IgnoreNextAssetImportedEventForCurrentPrefab = false;
                    break;
                }
            }
        }

        // This method is not called from the SceneView if the SceneView does not support stage handling
        internal override void OnPreSceneViewRender(SceneView sceneView)
        {
            StaticOcclusionCullingVisualization.showOcclusionCulling = false;

            if (mode != Mode.InContext)
                return;

            StageUtility.EnableHidingForInContextEditingInSceneView(true);
            StageUtility.SetFocusedScene(scene);
            StageUtility.SetFocusedSceneContextRenderMode(StageNavigationManager.instance.contextRenderMode);
            sceneView.SetSceneViewFilteringForStages(StageNavigationManager.instance.contextRenderMode == StageUtility.ContextRenderMode.GreyedOut);
        }

        // This method is not called from the SceneView if the SceneView does not support stage handling
        internal override void OnPostSceneViewRender(SceneView sceneView)
        {
            StaticOcclusionCullingVisualization.showOcclusionCulling = OcclusionCullingWindow.isVisible;

            if (mode != Mode.InContext)
                return;

            StageUtility.EnableHidingForInContextEditingInSceneView(false);
            StageUtility.SetFocusedScene(default(Scene));
            StageUtility.SetFocusedSceneContextRenderMode(StageUtility.ContextRenderMode.Normal);
            sceneView.SetSceneViewFilteringForStages(false);
        }

        Texture2D DeterminePrefabFileIconFromInstanceRootGameObject()
        {
            bool partOfInstance = PrefabUtility.IsPartOfNonAssetPrefabInstance(prefabContentsRoot);
            if (partOfInstance)
                return Icons.prefabVariantIcon;
            return Icons.prefabIcon;
        }

        internal override void OnControlsGUI(SceneView sceneView)
        {
            GUILayout.Space(15);
            if (mode == Mode.InContext)
            {
                InContextModeSelector(sceneView);
                GUILayout.Space(15);
                VisualizeOverridesToggle();
                GUILayout.Space(15);
            }
            SaveButtons(sceneView);
        }

        internal override void PlaceGameObjectInStage(GameObject rootGameObject)
        {
            if (this != null && isValid)
                rootGameObject.transform.SetParent(prefabContentsRoot.transform, true);
        }

        internal override void SaveHierarchyState(SceneHierarchyWindow hierarchyWindow)
        {
            if (!isValid)
                return;

            Hash128 key = StageUtility.CreateWindowAndStageIdentifier(hierarchyWindow.windowGUID, this);
            var state = s_StateCache.GetState(key);
            if (state == null)
                state = new PrefabStageHierarchyState();
            state.SaveStateFromHierarchy(hierarchyWindow, this);
            s_StateCache.SetState(key, state);
        }

        PrefabStageHierarchyState GetStoredHierarchyState(SceneHierarchyWindow hierarchyWindow)
        {
            Hash128 key = StageUtility.CreateWindowAndStageIdentifier(hierarchyWindow.windowGUID, this);
            return s_StateCache.GetState(key);
        }

        internal override void LoadHierarchyState(SceneHierarchyWindow hierarchy)
        {
            if (!isValid)
                return;

            var state = GetStoredHierarchyState(hierarchy);
            if (state != null)
                state.LoadStateIntoHierarchy(hierarchy, this);
            else
                OnFirstTimeOpenStageInSceneHierachyWindow(hierarchy);
        }

        void InitContextRenderModeSelector()
        {
            if (m_Mode != Mode.InContext)
                return;

            List<ExposablePopupMenu.ItemData> buttonData = new List<ExposablePopupMenu.ItemData>();

            GUIStyle onStyle = Styles.exposablePopupItem;
            GUIStyle offStyle = Styles.exposablePopupItem;
            var currentState = StageNavigationManager.instance.contextRenderMode;

            for (int i = 0; i < Styles.contextRenderModeTexts.Length; ++i)
            {
                bool on = currentState == Styles.contextRenderModeOptions[i];
                buttonData.Add(new ExposablePopupMenu.ItemData(Styles.contextRenderModeTexts[i], on ? onStyle : offStyle, on, true, Styles.contextRenderModeOptions[i]));
            }

            GUIContent popupButtonContent = Styles.contextRenderModeTexts[(int)currentState];

            ExposablePopupMenu.PopupButtonData popListData = new ExposablePopupMenu.PopupButtonData(popupButtonContent, Styles.exposablePopup);
            s_ContextRenderModeSelector.Init(Styles.contextLabel, buttonData, 4f, 50, popListData, ContextRenderModeClickedCallback);
            s_ContextRenderModeSelector.rightAligned = true;
        }

        void ContextRenderModeClickedCallback(ExposablePopupMenu.ItemData itemClicked)
        {
            // Behave like radio buttons: a button that is on cannot be turned off
            if (!itemClicked.m_On)
            {
                var selectedMode = (StageUtility.ContextRenderMode)itemClicked.m_UserData;
                StageNavigationManager.instance.contextRenderMode = selectedMode;

                // Ensure to recalc widths and selected texts
                InitContextRenderModeSelector();
            }
        }

        void InContextModeSelector(SceneView sceneView)
        {
            if (s_ContextRenderModeSelector == null)
            {
                s_ContextRenderModeSelector = new ExposablePopupMenu();
                InitContextRenderModeSelector();
            }

            EditorGUI.BeginChangeCheck();
            var rect = GUILayoutUtility.GetRect(s_ContextRenderModeSelector.widthOfPopupAndLabel, s_ContextRenderModeSelector.widthOfButtonsAndLabel, 0, 22);
            s_ContextRenderModeSelector.OnGUI(rect);
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }

        internal bool showOverrides
        {
            get
            {
                return s_PatchAllOverriddenProperties.value && !m_TemporarilyDisablePatchAllOverridenProperties;
            }
            set
            {
                if (m_TemporarilyDisablePatchAllOverridenProperties)
                    return;

                if (value == s_PatchAllOverriddenProperties.value)
                    return;

                s_PatchAllOverriddenProperties.value = value;

                var stageHistory = StageNavigationManager.instance.stageHistory;
                foreach (var stage in stageHistory)
                {
                    PrefabStage prefabStage = stage as PrefabStage;
                    if (prefabStage != null)
                        prefabStage.RefreshPatchedProperties();
                }
                EditorApplication.RequestRepaintAllViews();
            }
        }

        void RefreshPatchedProperties()
        {
            DrivenPropertyManager.UnregisterProperties(this);
            RecordPatchedPropertiesForContent();
            ApplyPatchedPropertiesToContent();
        }

        void VisualizeOverridesToggle()
        {
            using (new EditorGUI.DisabledScope(!IsOpenedFromInstanceObjectValid() || m_TemporarilyDisablePatchAllOverridenProperties))
            {
                EditorGUI.BeginChangeCheck();
                var content = m_TemporarilyDisablePatchAllOverridenProperties ? Styles.showOverridesLabelWithTooManyOverridesTooltip : Styles.showOverridesLabel;
                bool patchAll = GUILayout.Toggle(showOverrides, content);
                if (EditorGUI.EndChangeCheck())
                    showOverrides = patchAll;
            }
        }

        void CachePrefabFolderInfo()
        {
            bool isRootFolder;
            m_IsPrefabInValidAssetFolder = AssetDatabase.GetAssetFolderInfo(m_PrefabAssetPath, out isRootFolder, out m_IsPrefabInImmutableFolder);
        }

        bool IsPrefabInImmutableFolder()
        {
            return !m_IsPrefabInValidAssetFolder || m_IsPrefabInImmutableFolder;
        }

        void SaveButtons(SceneView sceneView)
        {
            if (IsPrefabInImmutableFolder())
            {
                GUILayout.Label(Styles.immutablePrefabContent, EditorStyles.boldLabel);
                return;
            }

            StatusQueryOptions opts = EditorUserSettings.allowAsyncStatusUpdate ? StatusQueryOptions.UseCachedAsync : StatusQueryOptions.UseCachedIfPossible;
            bool openForEdit = AssetDatabase.IsOpenForEdit(assetPath, opts);

            if (showingSavingLabel)
            {
                GUILayout.Label(Styles.autoSavingBadgeContent, Styles.savingBadge);
                GUILayout.Space(4);
            }

            if (!autoSave)
            {
                using (new EditorGUI.DisabledScope((!openForEdit || !hasUnsavedChanges) && !isAssetMissing))
                {
                    if (GUILayout.Button(Styles.saveButtonContent, Styles.button))
                        Save();
                }
            }

            if (EditorSettings.prefabModeAllowAutoSave)
            {
                bool autoSaveForScene = autoSave;
                EditorGUI.BeginChangeCheck();
                autoSaveForScene = GUILayout.Toggle(autoSaveForScene, Styles.autoSaveGUIContent, Styles.saveToggle);
                if (EditorGUI.EndChangeCheck())
                    autoSave = autoSaveForScene;
            }

            if (!openForEdit)
            {
                if (GUILayout.Button(Styles.checkoutButtonContent, Styles.button))
                    AssetDatabase.MakeEditable(assetPath);
            }
        }

        internal const HideFlags kVisibleEnvironmentObjectHideFlags = HideFlags.DontSave | HideFlags.NotEditable;
        internal const HideFlags kNotVisibleEnvironmentObjectHideFlags = HideFlags.DontSave | HideFlags.NotEditable | HideFlags.HideInHierarchy;

        class HideFlagUtility
        {
            int m_LastSceneDirtyID = -1;
            List<GameObject> m_Roots = new List<GameObject>();
            GameObject m_PrefabInstanceRoot;
            Scene m_Scene;

            public HideFlagUtility(Scene scene, GameObject prefabInstanceRoot)
            {
                m_Scene = scene;
                m_PrefabInstanceRoot = prefabInstanceRoot;
            }

            public void UpdateEnvironmentHideFlagsIfNeeded()
            {
                if (m_LastSceneDirtyID == m_Scene.dirtyID)
                    return;
                m_LastSceneDirtyID = m_Scene.dirtyID;
                UpdateEnvironmentHideFlags();
            }

            public void UpdateEnvironmentHideFlags()
            {
                ValidatePreviewSceneConsistency();

                // We use hideflags to hide all environment objects (and make them non-editable since the user cannot save them)
                GameObject rootOfPrefabInstance = GetRoot(m_PrefabInstanceRoot);

                // Set all environment root hierarchies
                m_Scene.GetRootGameObjects(m_Roots);
                foreach (GameObject go in m_Roots)
                {
                    if (go == rootOfPrefabInstance)
                        continue;

                    SetHideFlagsRecursively(go.transform, kNotVisibleEnvironmentObjectHideFlags);
                }

                if (rootOfPrefabInstance != m_PrefabInstanceRoot)
                {
                    // Our prefab instance root might be a child of an environment object. Here we set those environment objects hidden (leaving the root prefab instance unchanged)
                    SetHideFlagsRecursivelyWithIgnore(rootOfPrefabInstance.transform, m_PrefabInstanceRoot.transform, kNotVisibleEnvironmentObjectHideFlags);

                    // And finally we ensure the ancestors of the prefab root are visible
                    Transform current = m_PrefabInstanceRoot.transform.parent;
                    while (current != null)
                    {
                        if (current.hideFlags != kVisibleEnvironmentObjectHideFlags)
                            current.gameObject.hideFlags = kVisibleEnvironmentObjectHideFlags;
                        current = current.parent;
                    }
                }
            }

            static GameObject GetRoot(GameObject go)
            {
                Transform current = go.transform;
                while (true)
                {
                    if (current.parent == null)
                        return current.gameObject;
                    current = current.parent;
                }
            }

            static void SetHideFlagsRecursively(Transform transform, HideFlags hideFlags)
            {
                if (transform.hideFlags != hideFlags)
                    transform.gameObject.hideFlags = hideFlags;

                for (int i = 0; i < transform.childCount; i++)
                    SetHideFlagsRecursively(transform.GetChild(i), hideFlags);
            }

            static void SetHideFlagsRecursivelyWithIgnore(Transform transform, Transform ignoreThisTransform, HideFlags hideFlags)
            {
                if (transform == ignoreThisTransform)
                    return;

                if (transform.gameObject.hideFlags != hideFlags)
                    transform.gameObject.hideFlags = hideFlags;  // GameObject also sets all components so check before setting

                for (int i = 0; i < transform.childCount; i++)
                    SetHideFlagsRecursivelyWithIgnore(transform.GetChild(i), ignoreThisTransform, hideFlags);
            }

            void ValidatePreviewSceneConsistency()
            {
                if (!ValidatePreviewSceneState.ValidatePreviewSceneObjectState(m_PrefabInstanceRoot))
                {
                    ValidatePreviewSceneState.LogErrors();
                }
            }
        }

        static class ValidatePreviewSceneState
        {
            static List<string> m_Errors = new List<string>();

            public static void LogErrors()
            {
                string combinedErrors = string.Join("\n", m_Errors.ToArray());
                Debug.LogError("Inconsistent preview object state:\n" + combinedErrors);
            }

            static public bool ValidatePreviewSceneObjectState(GameObject root)
            {
                m_Errors.Clear();
                TransformVisitor visitor = new TransformVisitor();
                visitor.VisitAll(root.transform, ValidateGameObject, null);
                return m_Errors.Count == 0;
            }

            static void ValidateGameObject(Transform transform, object userData)
            {
                GameObject go = transform.gameObject;
                if (!EditorSceneManager.IsPreviewSceneObject(go))
                {
                    m_Errors.Add("   GameObject not correctly marked as PreviewScene object: " + go.name);
                }

                var components = go.GetComponents<Component>();
                foreach (var c in components)
                {
                    if (c == null)
                    {
                        // This can happen if a monobehaviour has a missing script.
                        // In this case the check can not be made. Could be fixed
                        // by moving the component iteration to native code.
                        continue;
                    }
                    if (!EditorSceneManager.IsPreviewSceneObject(c))
                        m_Errors.Add(string.Format("   Component {0} not correctly marked as PreviewScene object: (GameObject: {1})", c.GetType().Name, go.name));
                }
            }
        }

        void OnPlayModeStateChanged(PlayModeStateChange playmodeState)
        {
            if (playmodeState == PlayModeStateChange.ExitingEditMode)
            {
                bool blockPrefabModeInPlaymode = PrefabStageUtility.CheckIfAnyComponentShouldBlockPrefabModeInPlayMode(assetPath);
                if (blockPrefabModeInPlaymode)
                {
                    StageNavigationManager.instance.GoToMainStage(StageNavigationManager.Analytics.ChangeType.GoToMainViaPlayModeBlocking);
                }
            }

            if (playmodeState == PlayModeStateChange.EnteredEditMode)
            {
                // When exiting play mode we reload scenes and if we are in in Prefab Mode in Context we need to reconstruct the hidden state
                // for the the instance in the previous stage (this is setup when entering Prefab Mode in Context)
                if (mode == Mode.InContext && m_OpenedFromInstanceRoot != null)
                    SetPrefabInstanceHiddenForInContextEditing(true);
            }
        }

        [OnOpenAsset]
        static bool OnOpenAsset(int instanceID, int line)
        {
            string assetPath = AssetDatabase.GetAssetPath(instanceID);

            if (assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                // The 'line' parameter can be used for passing an instanceID of a prefab instance
                GameObject instanceRoot = line == -1 ? null : EditorUtility.InstanceIDToObject(line) as GameObject;
                var prefabStageMode = instanceRoot != null ? PrefabStage.Mode.InContext : PrefabStage.Mode.InIsolation;
                PrefabStageUtility.OpenPrefab(assetPath, instanceRoot, prefabStageMode, StageNavigationManager.Analytics.ChangeType.EnterViaAssetOpened);
                return true;
            }
            return false;
        }
    }
}
