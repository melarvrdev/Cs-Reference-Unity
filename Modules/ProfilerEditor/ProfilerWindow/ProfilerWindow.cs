// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor.Accessibility;
using UnityEditor.Networking.PlayerConnection;
using UnityEditor.Profiling;
using UnityEditor.Profiling.ModuleEditor;
using UnityEditor.StyleSheets;
using UnityEditorInternal;
using UnityEditorInternal.Profiling;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.Profiling;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using UnityEditor.MPE;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Profiler", icon = "UnityEditor.ProfilerWindow")]
    public sealed class ProfilerWindow : EditorWindow, IHasCustomMenu, IProfilerWindowController, ProfilerModulesDropdownWindow.IResponder
    {
        internal static class Styles
        {
            public static readonly GUIContent addArea = EditorGUIUtility.TrTextContent("Profiler Modules", "Add and remove profiler modules");
            public static readonly GUIContent deepProfile = EditorGUIUtility.TrTextContent("Deep Profile", "Instrument all scripting method calls to investigate scripts");
            public static readonly GUIContent deepProfileNotSupported = EditorGUIUtility.TrTextContent("Deep Profile", "Build a Player with Deep Profiling Support to be able to enable instrumentation of all scripting methods in a Player.");
            public static readonly GUIContent noData = EditorGUIUtility.TrTextContent("No frame data available");
            public static readonly GUIContent noActiveModules = EditorGUIUtility.TrTextContent("No Profiler Modules are active. Activate modules from the top left-hand drop-down.");

            public static readonly string enableDeepProfilingWarningDialogTitle = L10n.Tr("Enable deep script profiling");
            public static readonly string enableDeepProfilingWarningDialogContent = L10n.Tr("Enabling deep profiling requires reloading scripts.");
            public static readonly string disableDeepProfilingWarningDialogTitle = L10n.Tr("Disable deep script profiling");
            public static readonly string disableDeepProfilingWarningDialogContent = L10n.Tr("Disabling deep profiling requires reloading all scripts.");
            public static readonly string domainReloadWarningDialogButton = L10n.Tr("Reload");
            public static readonly string cancelDialogButton = L10n.Tr("Cancel");

            public static readonly GUIContent recordCallstacks = EditorGUIUtility.TrTextContent("Call Stacks", "Record call stacks for special samples such as \"GC.Alloc\". " +
                "To see the call stacks, select a sample in the CPU Usage module, e.g. in Timeline view. " +
                "To also see call stacks in Hierarchy view, switch from \"No Details\" to \"Related Data\", select a \"GC.Alloc\" sample and select \"N/A\" items from the list.");
            public static readonly string[] recordCallstacksOptions =
            {
                L10n.Tr("GC.Alloc"), L10n.Tr("UnsafeUtility.Malloc(Persistent)"), L10n.Tr("JobHandle.Complete")
            };
            public static readonly string[] recordCallstacksDevelopmentOptions =
            {
                L10n.Tr("GC.Alloc"), L10n.Tr("UnsafeUtility.Malloc(Persistent)"), L10n.Tr("JobHandle.Complete"), L10n.Tr("Native Allocations (Editor Only)")
            };
            public static readonly ProfilerMemoryRecordMode[] recordCallstacksEnumValues =
            {
                ProfilerMemoryRecordMode.GCAlloc, ProfilerMemoryRecordMode.UnsafeUtilityMalloc, ProfilerMemoryRecordMode.JobHandleComplete, ProfilerMemoryRecordMode.NativeAlloc
            };

            public static readonly GUIContent profilerRecordOff = EditorGUIUtility.TrIconContent("Record Off", "Record profiling information");
            public static readonly GUIContent profilerRecordOn = EditorGUIUtility.TrIconContent("Record On", "Record profiling information");

            public static SVC<Color> borderColor =
                new SVC<Color>("--theme-profiler-border-color-darker", Color.black);
            public static readonly GUIContent prevFrame = EditorGUIUtility.TrIconContent("Animation.PrevKey", "Previous frame");
            public static readonly GUIContent nextFrame = EditorGUIUtility.TrIconContent("Animation.NextKey", "Next frame");
            public static readonly GUIContent currentFrame = EditorGUIUtility.TrIconContent("Animation.LastKey", "Current frame");
            public static readonly GUIContent frame = EditorGUIUtility.TrTextContent("Frame: ", "Selected frame / Total number of frames");
            public static readonly GUIContent clearOnPlay = EditorGUIUtility.TrTextContent("Clear on Play", "Clear the captured data on entering Play Mode, or connecting to a new Player");
            public static readonly GUIContent clearData = EditorGUIUtility.TrTextContent("Clear", "Clear the captured data");
            public static readonly GUIContent saveWindowTitle = EditorGUIUtility.TrTextContent("Save Window");
            public static readonly GUIContent saveProfilingData = EditorGUIUtility.TrIconContent("SaveAs", "Save current profiling information to a binary file");
            public static readonly GUIContent loadWindowTitle = EditorGUIUtility.TrTextContent("Load Window");
            public static readonly GUIContent loadProfilingData = EditorGUIUtility.TrIconContent("Profiler.Open", "Load binary profiling information from a file. Shift click to append to the existing data");
            public static readonly string[] loadProfilingDataFileFilters = new string[] { L10n.Tr("Profiler files"), "data,raw", L10n.Tr("All files"), "*" };

            public static readonly GUIContent optionsButtonContent = EditorGUIUtility.TrIconContent("_Menu", "Additional Options");
            public static readonly GUIContent helpButtonContent = EditorGUIUtility.TrIconContent("_Help", "Open Manual (in a web browser)");
            public const string linkToManual = "https://docs.unity3d.com/Manual/ProfilerWindow.html";
            public static readonly GUIContent preferencesButtonContent = EditorGUIUtility.TrTextContent("Preferences", "Open User Preferences for the Profiler");

            public static readonly GUIContent accessibilityModeLabel = EditorGUIUtility.TrTextContent("Color Blind Mode", "Switch the color scheme to color blind safe colors");
            public static readonly GUIContent showStatsLabelsOnCurrentFrameLabel = EditorGUIUtility.TrTextContent("Show Stats for 'current frame'", "Show stats labels when the 'current frame' toggle is on.");

            public static readonly GUIStyle background = "OL box flat";
            public static readonly GUIStyle header = "OL title";
            public static readonly GUIStyle label = "OL label";
            public static readonly GUIStyle entryEven = "OL EntryBackEven";
            public static readonly GUIStyle entryOdd = "OL EntryBackOdd";
            public static readonly GUIStyle profilerGraphBackground = "ProfilerScrollviewBackground";
            public static readonly GUIStyle profilerDetailViewBackground = "ProfilerDetailViewBackground";

            public static readonly GUILayoutOption chartWidthOption = GUILayout.Width(Chart.kSideWidth - 1);

            static Styles()
            {
                profilerGraphBackground.overflow.left = -(int)Chart.kSideWidth;
            }
        }

        static List<ProfilerWindow> s_ProfilerWindows = new List<ProfilerWindow>();

        const string k_UxmlResourceName = "ProfilerWindow.uxml";
        const string k_UssSelector_ProfilerWindowDark = "profiler-window--dark";
        const string k_UssSelector_ProfilerWindowLight = "profiler-window--light";
        const string k_UssSelector_MainSplitView = "main-split-view";
        const string k_UssSelector_ToolbarAndChartsLegacyIMGUIContainer = "toolbar-and-charts__legacy-imgui-container";
        const string k_UssSelector_ModuleDetailsView_Container = "module-details-view__container";
        const string k_MainSplitViewFixedPaneSizePreferenceKey = "ProfilerWindow.MainSplitView.FixedPaneSize";
        const int k_NoModuleSelected = -1;
        const string k_SelectedModuleIndexPreferenceKey = "ProfilerWindow.SelectedModuleIndex";
        const string k_DynamicModulesPreferenceKey = "ProfilerWindow.DynamicModules";

        static readonly Vector2 k_MinimumWindowSize = new Vector2(720f, 216f);

        [NonSerialized]
        bool m_Initialized;

        [NonSerialized]
        float m_FrameCountLabelMinWidth = 0;

        // For keeping correct "Recording" state on window maximizing
        [SerializeField]
        bool m_Recording;

        IConnectionStateInternal m_AttachProfilerState;

        Vector2 m_GraphPos = Vector2.zero;

        [SerializeField]
        string m_ActiveNativePlatformSupportModuleName;

        [NonSerialized]
        int m_SelectedModuleIndex = k_NoModuleSelected;

        int m_CurrentFrame = FrameDataView.invalidOrCurrentFrameIndex;
        int m_LastFrameFromTick = FrameDataView.invalidOrCurrentFrameIndex;

        bool m_CurrentFrameEnabled = false;

        const int k_MainThreadIndex = 0;

        HierarchyFrameDataView m_FrameDataView;

        public const string cpuModuleName = CPUProfilerModule.k_UnlocalizedName;
        public const string gpuModuleName = GPUProfilerModule.k_UnlocalizedName;

        [SerializeReference]
        List<ProfilerModuleBase> m_Modules;

        internal IEnumerable<ProfilerModuleBase> Modules => m_Modules;
        // This is used by our performance tests to prevent the Profiler window from automatically repainting whilst profiling so we can control the repaints more precisely.
        internal bool IgnoreRepaintAllProfilerWindowsTick { get; set; }

        // Used by ProfilerEditorTests/ProfilerAreaReferenceCounterTests through reflection.
        ProfilerAreaReferenceCounter m_AreaReferenceCounter;
        [SerializeReference]
        ProfilerWindowControllerProxy m_ProfilerWindowControllerProxy = new ProfilerWindowControllerProxy();

        ProfilerMemoryRecordMode m_CurrentCallstackRecordMode = ProfilerMemoryRecordMode.None;
        [SerializeField]
        ProfilerMemoryRecordMode m_CallstackRecordMode = ProfilerMemoryRecordMode.None;

        internal string ConnectedTargetName => m_AttachProfilerState.connectionName;
        internal bool ConnectedToEditor => m_AttachProfilerState.connectedToTarget == ConnectionTarget.Editor;
        internal TwoPaneSplitView MainSplitView { get; private set; }

        [SerializeField]
        bool m_ClearOnPlay;

        // UI references.
        IMGUIContainer m_ToolbarAndChartsIMGUIContainer;
        VisualElement m_DetailsViewContainer;

        internal VisualElement DetailsViewContainer => m_DetailsViewContainer;

        const string kProfilerRecentSaveLoadProfilePath = "ProfilerRecentSaveLoadProfilePath";
        const string kProfilerEnabledSessionKey = "ProfilerEnabled";
        const string kProfilerEditorTargetModeEnabledSessionKey = "ProfilerTargetMode";
        const string kProfilerDeepProfilingWarningSessionKey = "ProfilerDeepProfilingWarning";

        internal event Action<int, bool> currentFrameChanged = delegate {};
        internal event Action frameDataViewAboutToBeDisposed = delegate {};
        // TODO This event must be made public as part of Extensibility.
        internal event Action<long> SelectedFrameIndexChanged;

        internal event Action<bool> recordingStateChanged = delegate {};
        internal event Action<bool> deepProfileChanged = delegate {};
        internal event Action<ProfilerMemoryRecordMode> memoryRecordingModeChanged = delegate {};

        public string selectedModuleName => selectedModule?.name ?? null;

        internal ProfilerModuleBase selectedModule
        {
            get
            {
                return ModuleAtIndex(m_SelectedModuleIndex);
            }
            set
            {
                // As far as I can tell, the only time this check is required is when a new frame is selected in a chart view that is already selected. We can mitigate that when we restructure charts for UIToolkit.
                if (selectedModule == value)
                    return;

                SelectModule(value);
            }
        }

        // IProfilerWindow Interface implementation
        public long selectedFrameIndex
        {
            get => GetActiveVisibleFrameIndex();
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", $"Can't set a value < 0 for the {nameof(selectedFrameIndex)}.");
                if (value < firstAvailableFrameIndex)
                    throw new ArgumentOutOfRangeException("value", $"Can't set a value smaller than {nameof(firstAvailableFrameIndex)} which is currently {firstAvailableFrameIndex}.");
                if (value > lastAvailableFrameIndex)
                    throw new ArgumentOutOfRangeException("value", $"Can't set a value greater than {nameof(lastAvailableFrameIndex)} which is currently {lastAvailableFrameIndex}.");
                SetActiveVisibleFrameIndex((int)value);
            }
        }

        // these properties act as a redirect to ProfilerDriver for now.
        // Once the Profiler Window isn't so tightly coupled to the ProfilerDriver singleton anymore, they will relate just the data stream displayed in this instance.
        public long firstAvailableFrameIndex => ProfilerDriver.firstFrameIndex;
        public long lastAvailableFrameIndex => ProfilerDriver.lastFrameIndex;

        internal ProfilerProperty CreateProperty()
        {
            return CreateProperty(HierarchyFrameDataView.columnDontSort);
        }

        internal ProfilerProperty CreateProperty(int sortType)
        {
            var targetedFrame = GetActiveVisibleFrameIndex();
            if (targetedFrame < 0)
                targetedFrame = ProfilerDriver.lastFrameIndex;
            if (targetedFrame < Math.Max(0, ProfilerDriver.firstFrameIndex))
            {
                return null;
            }

            var property = new ProfilerProperty();
            property.SetRoot(targetedFrame, sortType, (int)ProfilerViewType.Hierarchy);
            property.onlyShowGPUSamples = (selectedModule is GPUProfilerModule);
            return property;
        }

        internal int GetActiveVisibleFrameIndex()
        {
            // Update the current frame only at fixed intervals,
            // otherwise it looks weird when it is rapidly jumping around when we have a lot of repaints
            return m_CurrentFrame == FrameDataView.invalidOrCurrentFrameIndex ? m_LastFrameFromTick : m_CurrentFrame;
        }

        internal bool ProfilerWindowOverheadIsAffectingProfilingRecordingData()
        {
            return ProcessService.level == ProcessLevel.Main && IsSetToRecord() && ProfilerDriver.IsConnectionEditor() && ((EditorApplication.isPlaying && !EditorApplication.isPaused) || ProfilerDriver.profileEditor);
        }

        internal bool IsRecording()
        {
            return IsSetToRecord() && ((EditorApplication.isPlaying && !EditorApplication.isPaused) || ProfilerDriver.profileEditor || !ProfilerDriver.IsConnectionEditor());
        }

        internal bool IsSetToRecord()
        {
            return m_Recording;
        }

        public IProfilerFrameTimeViewSampleSelectionController GetFrameTimeViewSampleSelectionController(string moduleName)
        {
            switch (moduleName)
            {
                case cpuModuleName:
                    var cpuModule = this.GetProfilerModuleByType<CPUProfilerModule>();
                    return cpuModule;
                case gpuModuleName:
                    var gpuModule = this.GetProfilerModuleByType<GPUProfilerModule>();
                    return gpuModule;
                default:
                    throw new ArgumentException($"\"{moduleName}\" is not a valid module name for a module implementing IProfilerFrameTimeViewSampleSelectionController. Try \"{nameof(ProfilerWindow.cpuModuleName)}\" or \"{nameof(ProfilerWindow.gpuModuleName)}\" instead.", $"{nameof(moduleName)}");
            }
        }

        // TODO: Remove this once Profile Analizer version 1.0.5 is verified.
        // Used by Profiler Analyzer via reflection.
        internal T GetProfilerModule<T>(ProfilerArea area) where T : ProfilerModuleBase
        {
            foreach (var module in m_Modules)
            {
                if (module.area == area)
                {
                    return module as T;
                }
            }

            return null;
        }

        // Used by Tests/ProfilerEditorTests/ProfilerModulePreferenceKeyTests.
        internal ProfilerModuleBase GetProfilerModuleByType(Type type)
        {
            ProfilerModuleBase fittingModule = null;
            if (type.IsAbstract)
                throw new ArgumentException($"{nameof(type)} can't be abstract.", $"{nameof(type)}");

            foreach (var module in m_Modules)
            {
                if (type == module.GetType())
                {
                    fittingModule = module;
                }
            }
            if (fittingModule == null)
                throw new ArgumentException($"A {nameof(type)} of {type.Name} is not a type that describes an existing Profiler Module.", $"{nameof(type)}");
            return fittingModule;
        }

        internal void SetProfilerModuleActiveState(ProfilerModuleBase module, bool active)
        {
            if (module == null)
                throw new ArgumentNullException($"{nameof(module)}");
            var moduleIndex = IndexOfModule(module as ProfilerModuleBase);
            if (moduleIndex == k_NoModuleSelected)
                throw new ArgumentException($"The {module.name} module is not registered with the Profiler Window.", $"{nameof(module)}");
            m_Modules[moduleIndex].active = active;
        }

        internal bool GetProfilerModuleActiveState(ProfilerModuleBase module)
        {
            if (module == null)
                throw new ArgumentNullException($"{nameof(module)}");
            var moduleIndex = IndexOfModule(module as ProfilerModuleBase);
            if (moduleIndex == k_NoModuleSelected)
                throw new ArgumentException($"The {module.name} module is not registered with the Profiler Window.", $"{nameof(module)}");
            return m_Modules[moduleIndex].active;
        }

        void OnEnable()
        {
            minSize = k_MinimumWindowSize;
            titleContent = GetLocalizedTitleContent();
            s_ProfilerWindows.Add(this); // TODO Remove until we have a need for this.

            InitializeIfNeeded();
            ConstructVisualTree();
            SubscribeToGlobalEvents();

            // If there is already an open instance of the Module Editor window, resubscribe to the onChangesConfirmed event.
            if (ModuleEditorWindow.TryGetOpenInstance(out var moduleEditorWindow))
            {
                moduleEditorWindow.onChangesConfirmed += OnModuleEditorChangesConfirmed;
            }

            foreach (var module in m_Modules)
            {
                module.OnEnable();
            }

            // Select the last selected module this session. If there wasn't one, try to select the first active module.
            var moduleIndexToSelect = SessionState.GetInt(k_SelectedModuleIndexPreferenceKey, k_NoModuleSelected);
            if (moduleIndexToSelect != k_NoModuleSelected)
                SelectModuleAtIndex(moduleIndexToSelect);
            else
                SelectFirstActiveModule();
        }

        void OnDisable()
        {
            SaveViewSettings();
            m_AttachProfilerState.Dispose();
            m_AttachProfilerState = null;
            s_ProfilerWindows.Remove(this);

            DeselectSelectedModuleIfNecessary();
            foreach (var module in m_Modules)
            {
                module.OnDisable();
            }

            UnsubscribeFromGlobalEvents();
        }

        void SubscribeToGlobalEvents()
        {
            // maximize playmode will call ondisable which will unsibrcibe for clear events.
            // we unsubscribe here to make sure that we dont end up with multiple
            EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;
            EditorApplication.playModeStateChanged += OnPlaymodeStateChanged;
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
            UserAccessiblitySettings.colorBlindConditionChanged += OnSettingsChanged;
            ProfilerUserSettings.settingsChanged += OnSettingsChanged;
            ProfilerDriver.profileLoaded += OnProfileLoaded;
            ProfilerDriver.profileCleared += OnProfileCleared;
            ProfilerDriver.profilerCaptureSaved += ProfilerWindowAnalytics.SendSaveLoadEvent;
            ProfilerDriver.profilerCaptureLoaded += ProfilerWindowAnalytics.SendSaveLoadEvent;
            ProfilerDriver.profilerConnected += ProfilerWindowAnalytics.SendConnectionEvent;
            ProfilerDriver.profilingStateChange += ProfilerWindowAnalytics.ProfilingStateChange;
        }

        void UnsubscribeFromGlobalEvents()
        {
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
            UserAccessiblitySettings.colorBlindConditionChanged -= OnSettingsChanged;
            ProfilerUserSettings.settingsChanged -= OnSettingsChanged;
            ProfilerDriver.profileLoaded -= OnProfileLoaded;
            ProfilerDriver.profileCleared -= OnProfileCleared;
            ProfilerDriver.profilerCaptureSaved -= ProfilerWindowAnalytics.SendSaveLoadEvent;
            ProfilerDriver.profilerCaptureLoaded -= ProfilerWindowAnalytics.SendSaveLoadEvent;
            ProfilerDriver.profilerConnected -= ProfilerWindowAnalytics.SendConnectionEvent;
            ProfilerDriver.profilingStateChange -= ProfilerWindowAnalytics.ProfilingStateChange;
        }

        void InitializeIfNeeded()
        {
            // TODO We no longer need this m_Initialized flag as Initialize() is only called from OnEnable.
            if (m_Initialized)
                return;

            Initialize();
        }

        void Initialize()
        {
            m_ProfilerWindowControllerProxy.SetRealSubject(this);

            // When reinitializing (e.g. because Colorblind mode or PlatformModule changed) we don't need a new state
            if (m_AttachProfilerState == null)
                m_AttachProfilerState = PlayerConnectionGUIUtility.GetConnectionState(this, OnTargetedEditorConnectionChanged, IsEditorConnectionTargeted, OnConnectedToPlayer) as IConnectionStateInternal;

            if (!HasValidModules())
                m_Modules = InstantiateAvailableProfilerModules();

            m_AreaReferenceCounter = new ProfilerAreaReferenceCounter();
            m_ActiveNativePlatformSupportModuleName = EditorUtility.GetActiveNativePlatformSupportModuleName();

            m_Initialized = true;
        }

        bool HasValidModules()
        {
            // Check if we haven't deserialized the modules.
            if (m_Modules == null)
            {
                return false;
            }

            // Check if any modules are null. This can occur if a module's deserialization failed due to a missing type (e.g. user removed a Profiler module type).
            foreach (var module in m_Modules)
            {
                if (module == null)
                    return false;
            }

            // If we have deserialized modules, check they are valid using the name. Module serialization was broken in 2020.2.0a15 until ~2020.2.0a20. For users coming from those versions we need to validate the serialized module state and rebuild the modules if invalid.
            return (m_Modules.Count > 0) && !string.IsNullOrEmpty(m_Modules[0].name);
        }

        List<ProfilerModuleBase> InstantiateAvailableProfilerModules()
        {
            var profilerModules = new List<ProfilerModuleBase>();
            InstantiatePredefinedProfilerModules(profilerModules);
            InstantiateDynamicProfilerModules(profilerModules);

            profilerModules.Sort((a, b) => a.orderIndex.CompareTo(b.orderIndex));

            return profilerModules;
        }

        void InstantiatePredefinedProfilerModules(List<ProfilerModuleBase> outModules)
        {
            var moduleTypes = TypeCache.GetTypesDerivedFrom<ProfilerModuleBase>();
            foreach (var moduleType in moduleTypes)
            {
                // Exclude DynamicProfilerModule as they are defined via the Module Editor, i.e. they are not 'predefined'.
                if (!moduleType.IsAbstract && moduleType != typeof(DynamicProfilerModule))
                {
                    try
                    {
                        var module = Activator.CreateInstance(moduleType, m_ProfilerWindowControllerProxy as IProfilerWindowController) as ProfilerModuleBase;
                        if (module != null)
                        {
                            outModules.Add(module);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Unable to create Profiler module of type {moduleType}. {e.Message}");
                    }
                }
            }
        }

        void InstantiateDynamicProfilerModules(List<ProfilerModuleBase> outModules)
        {
            var json = EditorPrefs.GetString(k_DynamicModulesPreferenceKey);
            var serializedDynamicModules = JsonUtility.FromJson<DynamicProfilerModule.SerializedDataCollection>(json);
            if (serializedDynamicModules != null)
            {
                for (int i = 0; i < serializedDynamicModules.Length; i++)
                {
                    var serializedDynamicModuleData = serializedDynamicModules[i];
                    var dynamicModule = DynamicProfilerModule.CreateFromSerializedData(serializedDynamicModuleData, m_ProfilerWindowControllerProxy);
                    outModules.Add(dynamicModule);
                }
            }
        }

        void ConstructVisualTree()
        {
            var template = EditorGUIUtility.Load(k_UxmlResourceName) as VisualTreeAsset;
            template.CloneTree(rootVisualElement);

            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssSelector_ProfilerWindowDark : k_UssSelector_ProfilerWindowLight;
            rootVisualElement.AddToClassList(themeUssClass);

            m_ToolbarAndChartsIMGUIContainer = rootVisualElement.Q<IMGUIContainer>(k_UssSelector_ToolbarAndChartsLegacyIMGUIContainer);
            m_ToolbarAndChartsIMGUIContainer.onGUIHandler = DoLegacyGUI_ToolbarAndCharts;

            MainSplitView = rootVisualElement.Q<TwoPaneSplitView>(k_UssSelector_MainSplitView);
            // TwoPaneSplitView.viewDataKey is not currently supported so we need to manually persist its state.
            var fixedPaneSize = EditorPrefs.GetFloat(k_MainSplitViewFixedPaneSizePreferenceKey, k_MinimumWindowSize.y * 0.5f);
            MainSplitView.fixedPaneInitialDimension = fixedPaneSize;

            m_DetailsViewContainer = rootVisualElement.Q<VisualElement>(k_UssSelector_ModuleDetailsView_Container);
        }

        void OnSettingsChanged()
        {
            SaveViewSettings();

            foreach (var module in m_Modules)
            {
                module.Rebuild();
            }
            Repaint();
        }

        void Clear()
        {
            // Clear All Frames calls ProfilerDriver.profileCleared which in turn calls OnProfileCleared
            ProfilerDriver.ClearAllFrames();
        }

        void OnProfileCleared()
        {
            ResetForClearedOrLoaded(true);
        }

        void OnProfileLoaded()
        {
            ResetForClearedOrLoaded(false);
        }

        void ResetForClearedOrLoaded(bool cleared)
        {
            // Reset frame state
            m_LastFrameFromTick = FrameDataView.invalidOrCurrentFrameIndex;
            m_FrameCountLabelMinWidth = 0;
            foreach (var module in m_Modules)
            {
                module.Clear();
            }
            // Reset the cached data view
            if (m_FrameDataView != null)
                DisposeFrameDataView();
            m_FrameDataView = null;

            if (cleared)
            {
                m_CurrentFrame = FrameDataView.invalidOrCurrentFrameIndex;
                m_CurrentFrameEnabled = true;
#pragma warning disable CS0618
                NetworkDetailStats.m_NetworkOperations.Clear();
#pragma warning restore
            }

            foreach (var module in m_Modules)
            {
                module.Clear();
                module.Update();
            }

            RepaintImmediately();
        }

        internal ProfilerModuleBase[] GetProfilerModules()
        {
            var copy = new ProfilerModuleBase[m_Modules.Count];
            m_Modules.CopyTo(copy);
            return copy;
        }

        internal void GetProfilerModules(ref List<ProfilerModuleBase> outModules)
        {
            if (outModules == null)
            {
                outModules = new List<ProfilerModuleBase>(m_Modules);
                return;
            }
            outModules.Clear();
            outModules.AddRange(m_Modules);
        }

        void CloseModule(ProfilerModuleBase module)
        {
            if (module == selectedModule)
            {
                SelectFirstActiveModule();
            }

            module.OnClosed();
        }

        void CheckForPlatformModuleChange()
        {
            var activeNativePlatformSupportModuleName = EditorUtility.GetActiveNativePlatformSupportModuleName();
            if (m_ActiveNativePlatformSupportModuleName != activeNativePlatformSupportModuleName)
            {
                OnActiveNativePlatformSupportModuleChanged(activeNativePlatformSupportModuleName);
            }
        }

        void OnActiveNativePlatformSupportModuleChanged(string activeNativePlatformSupportModuleName)
        {
            ProfilerDriver.ClearAllFrames();
            m_ActiveNativePlatformSupportModuleName = activeNativePlatformSupportModuleName;

            foreach (var module in m_Modules)
            {
                module.OnNativePlatformSupportModuleChanged();
            }
            Repaint();
        }

        void SaveViewSettings()
        {
            foreach (var module in m_Modules)
            {
                module.SaveViewSettings();
            }

            EditorPrefs.SetFloat(k_MainSplitViewFixedPaneSizePreferenceKey, MainSplitView.fixedPane.resolvedStyle.height);
            SessionState.SetInt(k_SelectedModuleIndexPreferenceKey, m_SelectedModuleIndex);
        }

        void Awake()
        {
            if (!Profiler.supported)
                return;

            // Track enabled state
            if (ProfilerUserSettings.rememberLastRecordState)
                m_Recording = EditorPrefs.GetBool(kProfilerEnabledSessionKey, ProfilerUserSettings.defaultRecordState);
            else
                m_Recording = SessionState.GetBool(kProfilerEnabledSessionKey, ProfilerUserSettings.defaultRecordState);

            // This event gets called every time when some other window is maximized and then unmaximized
            ProfilerDriver.enabled = m_Recording;

            ProfilerDriver.profileEditor = SessionState.GetBool(kProfilerEditorTargetModeEnabledSessionKey,
                ProfilerUserSettings.defaultTargetMode == ProfilerEditorTargetMode.Editmode || ProfilerDriver.profileEditor);

            // Update the current callstack capture mode.
            m_CurrentCallstackRecordMode = ProfilerDriver.memoryRecordMode;
        }

        void OnPlaymodeStateChanged(PlayModeStateChange stateChange)
        {
            m_CurrentFrameEnabled = false;
            if (stateChange == PlayModeStateChange.EnteredPlayMode)
            {
                ClearFramesOnPlayOrPlayerConnectionChange();
            }
        }

        void OnPauseStateChanged(PauseState stateChange)
        {
            m_CurrentFrameEnabled = false;
        }

        internal void ClearFramesOnPlayOrPlayerConnectionChange()
        {
            if (m_ClearOnPlay)
                Clear();
        }

        void OnDestroy()
        {
            // We're being temporary "hidden" on maximize, do nothing
            if (WindowLayout.GetMaximizedWindow() != null)
                return;

            // When window is destroyed, we disable profiling
            if (Profiler.supported)
                ProfilerDriver.enabled = false;
        }

        void OnFocus()
        {
            // set the real state of profiler. OnDestroy is called not only when window is destroyed, but also when maximized state is changed
            if (Profiler.supported)
            {
                ProfilerDriver.enabled = m_Recording;
            }

            ProfilerWindowAnalytics.OnProfilerWindowFocused();
        }

        void OnLostFocus()
        {
            if (GUIUtility.hotControl != 0)
            {
                // The chart may not have had the chance to release the hot control before we lost focus.
                // This happens when changing the selected frame, which may pause the game and switch the focus to another view.
                for (int i = 0; i < m_Modules.Count; ++i)
                {
                    var module = m_Modules[i];
                    module.OnLostFocus();
                }
            }

            ProfilerWindowAnalytics.OnProfilerWindowLostFocus();
        }

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(Styles.accessibilityModeLabel, UserAccessiblitySettings.colorBlindCondition != ColorBlindCondition.Default, OnToggleColorBlindMode);
        }

        void OnToggleColorBlindMode()
        {
            UserAccessiblitySettings.colorBlindCondition = UserAccessiblitySettings.colorBlindCondition == ColorBlindCondition.Default
                ? ColorBlindCondition.Deuteranopia
                : ColorBlindCondition.Default;
        }

        void OnToggleShowStatsLabelsOnCurrentFrame()
        {
            ProfilerUserSettings.showStatsLabelsOnCurrentFrame = !ProfilerUserSettings.showStatsLabelsOnCurrentFrame;
        }

        // Used by Native method DoBuildPlayer_PostBuild() in BuildPlayer.cpp
        [MenuItem("Window/Analysis/Profiler %7", false, 0)]
        internal static ProfilerWindow ShowProfilerWindow()
        {
            return EditorWindow.GetWindow<ProfilerWindow>(false);
        }

        // The Constructor shouldn't be part of the public API
        internal ProfilerWindow() {}

        [MenuItem("Window/Analysis/Profiler (Standalone Process)", false, 1)]
        static void ShowProfilerOOP()
        {
            if (EditorUtility.DisplayDialog("Profiler (Standalone Process)",
                "The Standalone Profiler launches the Profiler window in a separate process from the Editor. " +
                "This means that the performance of the Editor does not affect profiling data, and the Profiler does not affect the performance of the Editor. " +
                "It takes around 3-4 seconds to launch.", "OK", DialogOptOutDecisionType.ForThisMachine, "UseOutOfProcessProfiler"))
            {
                ProfilerRoleProvider.LaunchProfilerProcess();
            }
        }

        static string GetRecordingStateName(string defaultName)
        {
            if (!String.IsNullOrEmpty(defaultName))
                return $"of {defaultName}";
            if (ProfilerDriver.profileEditor)
                return "editmode";
            return "playmode";
        }

        [ShortcutManagement.Shortcut("Profiling/Profiler/RecordToggle", KeyCode.F9)]
        static void RecordToggle()
        {
            var commandHandled = false;
            if (CommandService.Exists("ProfilerRecordToggle"))
            {
                var result = CommandService.Execute("ProfilerRecordToggle", CommandHint.Shortcut);
                commandHandled = Convert.ToBoolean(result);
            }

            if (!commandHandled)
            {
                if (HasOpenInstances<ProfilerWindow>())
                {
                    var profilerWindow = GetWindow<ProfilerWindow>();
                    profilerWindow.SetRecordingEnabled(!profilerWindow.IsSetToRecord());
                }
                else
                {
                    ProfilerDriver.enabled = !ProfilerDriver.enabled;
                }

                using (var state = PlayerConnectionGUIUtility.GetConnectionState(null, null))
                {
                    var connectionName = "";
                    if (state.connectedToTarget != ConnectionTarget.Editor)
                        connectionName = state.connectionName;
                    EditorGUI.hyperLinkClicked -= EditorGUI_HyperLinkClicked;
                    EditorGUI.hyperLinkClicked += EditorGUI_HyperLinkClicked;
                    if (ProfilerDriver.enabled)
                        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, $"Recording {GetRecordingStateName(connectionName)} has started...");
                    else
                        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "Recording has ended.\r\nClick <a openprofiler=\"true\">here</a> to open the profiler window.");
                }
            }
        }

        static void EditorGUI_HyperLinkClicked(EditorWindow window, HyperLinkClickedEventArgs args)
        {
            if (args.hyperLinkData.ContainsKey("openprofiler"))
                ShowProfilerWindow();
        }

        [RequiredByNativeCode]
        static void RepaintAllProfilerWindows()
        {
            foreach (ProfilerWindow window in s_ProfilerWindows)
            {
                if (!window.IgnoreRepaintAllProfilerWindowsTick)
                {
                    // This is useful hack when you need to profile in the editor and dont want it to affect your framerate...
                    // NOTE: we should make this an option in the UI somehow...
                    //if (ProfilerDriver.lastFrameIndex != window.m_LastFrameFromTick && EditorWindow.focusedWindow == window)

                    if (ProfilerDriver.lastFrameIndex != window.m_LastFrameFromTick)
                    {
                        window.m_LastFrameFromTick = ProfilerDriver.lastFrameIndex;
                        window.m_ToolbarAndChartsIMGUIContainer.MarkDirtyRepaint();
                        window.InvokeSelectedFrameIndexChangedEventIfNecessary(window.m_LastFrameFromTick);
                    }
                }
            }
        }

        void SetProfileDeepScripts(bool deep)
        {
            bool currentDeep = ProfilerDriver.deepProfiling;
            if (currentDeep == deep)
                return;


            if (ProfilerDriver.IsConnectionEditor())
            {
                SetEditorDeepProfiling(deep);
            }
            else
            {
                // When connected to the player, we send deep profiler mode command immediately.
                ProfilerDriver.deepProfiling = deep;
                deepProfileChanged?.Invoke(deep);
            }
        }

        string PickFrameLabel()
        {
            var lastFrame = (ProfilerDriver.lastFrameIndex + 1);
            return ((m_CurrentFrame == FrameDataView.invalidOrCurrentFrameIndex) ? lastFrame : m_CurrentFrame + 1) + " / " + lastFrame;
        }

        void PrevFrame()
        {
            int previousFrame = ProfilerDriver.GetPreviousFrameIndex(m_CurrentFrame);
            if (previousFrame != FrameDataView.invalidOrCurrentFrameIndex)
                SetCurrentFrame(previousFrame);
        }

        void NextFrame()
        {
            int nextFrame = ProfilerDriver.GetNextFrameIndex(m_CurrentFrame);
            if (nextFrame != FrameDataView.invalidOrCurrentFrameIndex)
                SetCurrentFrame(nextFrame);
        }

        static bool CheckFrameData(ProfilerProperty property)
        {
            return property != null && property.frameDataReady;
        }

        internal HierarchyFrameDataView GetFrameDataView(string groupName, string threadName, ulong threadId, HierarchyFrameDataView.ViewModes viewMode, int profilerSortColumn, bool sortAscending)
        {
            var frameIndex = GetActiveVisibleFrameIndex();
            var foundThreadIndex = FrameDataView.invalidThreadIndex;
            using (var frameIterator = new ProfilerFrameDataIterator())
            {
                var threadCount = frameIterator.GetThreadCount(frameIndex);
                for (var i = 0; i < threadCount; ++i)
                {
                    frameIterator.SetRoot(frameIndex, i);
                    var grp = frameIterator.GetGroupName();
                    // only string compare if both names aren't null or empty, i.e. don't compare ` null != "" ` but treat null the same as empty
                    if (!(string.IsNullOrEmpty(grp) && string.IsNullOrEmpty(groupName)) && grp != groupName)
                        continue;
                    var thrd = frameIterator.GetThreadName();
                    if (threadName == thrd)
                    {
                        using (var rawFrameData = new RawFrameDataView(frameIndex, i))
                        {
                            // do we have a valid thread id to check against and a direct match?
                            if (threadId == FrameDataView.invalidThreadId || threadId == rawFrameData.threadId)
                            {
                                foundThreadIndex = i;
                                break;
                            }
                            // else store the first found thread index as a fallback
                            if (foundThreadIndex < 0)
                                foundThreadIndex = i;
                        }
                    }
                }
            }
            return GetFrameDataView(foundThreadIndex, viewMode, profilerSortColumn, sortAscending);
        }

        void DisposeFrameDataView()
        {
            frameDataViewAboutToBeDisposed();
            m_FrameDataView.Dispose();
        }

        internal HierarchyFrameDataView GetFrameDataView(int threadIndex, HierarchyFrameDataView.ViewModes viewMode, int profilerSortColumn, bool sortAscending)
        {
            var frameIndex = GetActiveVisibleFrameIndex();

            if (frameIndex < firstAvailableFrameIndex || frameIndex > lastAvailableFrameIndex)
            {
                // if the frame index is out of range, invalidate the FrameDataView
                if (m_FrameDataView != null && m_FrameDataView.valid)
                    DisposeFrameDataView();
            }
            else if (frameIndex != FrameDataView.invalidOrCurrentFrameIndex)
            {
                // if the frame is valid but the thread index is not, fallback onto main thread
                if (threadIndex < 0)
                    threadIndex = k_MainThreadIndex;
                else
                {
                    using (var iter = new ProfilerFrameDataIterator())
                    {
                        iter.SetRoot(frameIndex, k_MainThreadIndex);
                        if (threadIndex >= iter.GetThreadCount(frameIndex))
                            threadIndex = k_MainThreadIndex;
                    }
                }
            }

            if (m_FrameDataView != null && m_FrameDataView.valid)
            {
                if (m_FrameDataView.frameIndex == frameIndex && m_FrameDataView.threadIndex == threadIndex && m_FrameDataView.viewMode == viewMode)
                    return m_FrameDataView;
            }

            if (m_FrameDataView != null)
                DisposeFrameDataView();
            m_FrameDataView = new HierarchyFrameDataView(frameIndex, threadIndex, viewMode, profilerSortColumn, sortAscending);
            return m_FrameDataView;
        }

        void UpdateModules()
        {
            foreach (var module in m_Modules)
            {
                if (module.active)
                {
                    module.Update();
                }
            }
        }

        void SetCallstackRecordMode(ProfilerMemoryRecordMode memRecordMode)
        {
            if (memRecordMode == m_CurrentCallstackRecordMode)
                return;
            m_CurrentCallstackRecordMode = memRecordMode;
            ProfilerDriver.memoryRecordMode = memRecordMode;
            memoryRecordingModeChanged?.Invoke(memRecordMode);
        }

        void ToggleCallstackRecordModeFlag(object userData, string[] options, int selected)
        {
            m_CallstackRecordMode ^= Styles.recordCallstacksEnumValues[selected];
            if (m_CurrentCallstackRecordMode != ProfilerMemoryRecordMode.None)
                SetCallstackRecordMode(m_CallstackRecordMode);
        }

        internal void SaveProfilingData()
        {
            string recent = EditorPrefs.GetString(kProfilerRecentSaveLoadProfilePath);
            string directory = string.IsNullOrEmpty(recent)
                ? ""
                : System.IO.Path.GetDirectoryName(recent);
            string filename = string.IsNullOrEmpty(recent)
                ? ""
                : System.IO.Path.GetFileName(recent);

            string selected = EditorUtility.SaveFilePanel(Styles.saveWindowTitle.text, directory, filename, "data");
            if (selected.Length != 0)
            {
                EditorPrefs.SetString(kProfilerRecentSaveLoadProfilePath, selected);
                ProfilerDriver.SaveProfile(selected);
            }
        }

        internal void LoadProfilingData(bool keepExistingData)
        {
            string recent = EditorPrefs.GetString(kProfilerRecentSaveLoadProfilePath);
            string selected = EditorUtility.OpenFilePanelWithFilters(Styles.loadWindowTitle.text, recent, Styles.loadProfilingDataFileFilters);

            if (selected.Length != 0)
            {
                EditorPrefs.SetString(kProfilerRecentSaveLoadProfilePath, selected);

                if (ProfilerDriver.LoadProfile(selected, keepExistingData))
                {
                    // Stop current profiling if data was loaded successfully
                    ProfilerDriver.enabled = m_Recording = false;
                    SessionState.SetBool(kProfilerEnabledSessionKey, m_Recording);
                    if (ProfilerUserSettings.rememberLastRecordState)
                        EditorPrefs.SetBool(kProfilerEnabledSessionKey, m_Recording);
#pragma warning disable CS0618
                    NetworkDetailStats.m_NetworkOperations.Clear();
#pragma warning restore
                }
            }
        }

        internal void SetRecordingEnabled(bool profilerEnabled)
        {
            ProfilerDriver.enabled = profilerEnabled;
            m_Recording = profilerEnabled;
            SessionState.SetBool(kProfilerEnabledSessionKey, profilerEnabled);
            if (ProfilerUserSettings.rememberLastRecordState)
                EditorPrefs.SetBool(kProfilerEnabledSessionKey, profilerEnabled);
            recordingStateChanged?.Invoke(m_Recording);
            Repaint();
        }

        float DrawMainToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            DrawModuleSelectionDropdownMenu();

            // Engine attach
            PlayerConnectionGUILayout.ConnectionTargetSelectionDropdown(m_AttachProfilerState, EditorStyles.toolbarDropDown);

            // Record
            var profilerEnabled = GUILayout.Toggle(m_Recording, m_Recording ? Styles.profilerRecordOn : Styles.profilerRecordOff, EditorStyles.toolbarButton);
            if (profilerEnabled != m_Recording)
                SetRecordingEnabled(profilerEnabled);

            FrameNavigationControls();

            using (new EditorGUI.DisabledScope(ProfilerDriver.lastFrameIndex == FrameDataView.invalidOrCurrentFrameIndex))
            {
                // Clear
                if (GUILayout.Button(Styles.clearData, EditorStyles.toolbarButton))
                {
                    Clear();
                    GUIUtility.ExitGUI();
                }
            }

            // Separate File/Stream control elements from toggles
            GUILayout.FlexibleSpace();
            // Clear on Play
            SetClearOnPlay(GUILayout.Toggle(GetClearOnPlay(), Styles.clearOnPlay, EditorStyles.toolbarButton));

            // Deep profiling
            var deepProfilerSupported = m_AttachProfilerState.deepProfilingSupported;
            using (new EditorGUI.DisabledScope(!deepProfilerSupported))
            {
                SetProfileDeepScripts(GUILayout.Toggle(ProfilerDriver.deepProfiling, deepProfilerSupported ? Styles.deepProfile : Styles.deepProfileNotSupported, EditorStyles.toolbarButton));
            }

            // Allocation call stacks
            AllocationCallstacksToolbarItem();

            // keep more space between the toggles and the overflow/help icon buttons on the far right, keep deep profiling closer to the other controls
            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();

            // Load profile
            if (GUILayout.Button(Styles.loadProfilingData, EditorStyles.toolbarButton, GUILayout.MaxWidth(25)))
            {
                LoadProfilingData(Event.current.shift);

                // Opened a load pop-up, MacOS will redraw the window so bail out now
                EditorGUIUtility.ExitGUI();
            }

            // Save profile
            using (new EditorGUI.DisabledScope(ProfilerDriver.lastFrameIndex == FrameDataView.invalidOrCurrentFrameIndex))
            {
                if (GUILayout.Button(Styles.saveProfilingData, EditorStyles.toolbarButton))
                {
                    SaveProfilingData();

                    // Opened a save pop-up, MacOS will redraw the window so bail out now
                    EditorGUIUtility.ExitGUI();
                }
            }

            // Open Manual
            if (GUILayout.Button(Styles.helpButtonContent, EditorStyles.toolbarButton))
            {
                Application.OpenURL(Styles.linkToManual);
            }

            // Overflow Menu
            var overflowMenuRect = GUILayoutUtility.GetRect(Styles.optionsButtonContent, EditorStyles.toolbarButtonRight);
            if (GUI.Button(overflowMenuRect, Styles.optionsButtonContent, EditorStyles.toolbarButtonRight))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(Styles.accessibilityModeLabel, UserAccessiblitySettings.colorBlindCondition != ColorBlindCondition.Default, OnToggleColorBlindMode);
                menu.AddItem(Styles.showStatsLabelsOnCurrentFrameLabel, ProfilerUserSettings.showStatsLabelsOnCurrentFrame, OnToggleShowStatsLabelsOnCurrentFrame);
                menu.AddSeparator("");
                menu.AddItem(Styles.preferencesButtonContent, false, OpenProfilerPreferences);
                menu.DropDown(overflowMenuRect);
            }

            GUILayout.EndHorizontal();

            return EditorStyles.toolbar.fixedHeight;
        }

        void DrawModuleSelectionDropdownMenu()
        {
            Rect popupRect = GUILayoutUtility.GetRect(Styles.addArea, EditorStyles.toolbarDropDown, Styles.chartWidthOption);
            if (EditorGUI.DropdownButton(popupRect, Styles.addArea, FocusType.Passive, EditorStyles.toolbarDropDownLeft))
            {
                var popupScreenRect = GUIUtility.GUIToScreenRect(popupRect);
                if (ProfilerModulesDropdownWindow.TryPresentIfNoOpenInstances(popupScreenRect, m_Modules, out var modulesDropdownWindow))
                {
                    modulesDropdownWindow.responder = m_ProfilerWindowControllerProxy;
                }
            }
        }

        void OpenProfilerPreferences()
        {
            var settings = SettingsWindow.Show(SettingsScope.User, "Preferences/Analysis/Profiler");
            if (settings == null)
            {
                Debug.LogError("Could not find Preferences for 'Analysis/Profiler'");
            }
        }

        void AllocationCallstacksToolbarItem()
        {
            // Whenever we unset all flags, we fallback to the default GC Alloc callstacks.
            if (m_CallstackRecordMode == ProfilerMemoryRecordMode.None)
                m_CallstackRecordMode = ProfilerMemoryRecordMode.GCAlloc;

            var selectedMemRecordMode = m_CurrentCallstackRecordMode;
            var toggled = selectedMemRecordMode != ProfilerMemoryRecordMode.None;
            var oldToggleState = toggled;
            if (EditorGUILayout.DropDownToggle(ref toggled, Styles.recordCallstacks, EditorStyles.toolbarDropDownToggle))
            {
                var rect = GUILayoutUtility.topLevel.GetLast();
                var names = Unsupported.IsDeveloperMode() ? Styles.recordCallstacksDevelopmentOptions : Styles.recordCallstacksOptions;
                var selected = new List<int>();
                for (var i = 0; i < names.Length; ++i)
                {
                    if ((m_CallstackRecordMode & Styles.recordCallstacksEnumValues[i]) != 0)
                        selected.Add(i);
                }
                EditorUtility.DisplayCustomMenu(rect, names, selected.ToArray(), ToggleCallstackRecordModeFlag, null);
                GUIUtility.ExitGUI();
            }
            if (toggled != oldToggleState)
            {
                selectedMemRecordMode = m_CurrentCallstackRecordMode != ProfilerMemoryRecordMode.None ? ProfilerMemoryRecordMode.None : m_CallstackRecordMode;
                SetCallstackRecordMode(selectedMemRecordMode);
            }
        }

        void FrameNavigationControls()
        {
            // TODO We shouldn't be relying on GUI cycles to reset state like this.
            if (m_CurrentFrame > ProfilerDriver.lastFrameIndex)
            {
                SetCurrentFrameDontPause(ProfilerDriver.lastFrameIndex);
            }

            // Previous/next/current buttons
            using (new EditorGUI.DisabledScope(ProfilerDriver.GetPreviousFrameIndex(m_CurrentFrame) == FrameDataView.invalidOrCurrentFrameIndex))
            {
                if (GUILayout.Button(Styles.prevFrame, EditorStyles.toolbarButton))
                {
                    if (m_CurrentFrame == FrameDataView.invalidOrCurrentFrameIndex)
                        PrevFrame();
                    PrevFrame();
                }
            }

            using (new EditorGUI.DisabledScope(ProfilerDriver.GetNextFrameIndex(m_CurrentFrame) == FrameDataView.invalidOrCurrentFrameIndex))
            {
                if (GUILayout.Button(Styles.nextFrame, EditorStyles.toolbarButton))
                    NextFrame();
            }


            using (new EditorGUI.DisabledScope(ProfilerDriver.lastFrameIndex < 0))
            {
                if (GUILayout.Toggle(ProfilerDriver.lastFrameIndex >= 0 && m_CurrentFrame == FrameDataView.invalidOrCurrentFrameIndex, Styles.currentFrame, EditorStyles.toolbarButton))
                {
                    if (!m_CurrentFrameEnabled)
                    {
                        SelectAndStayOnLatestFrame();
                    }
                }
                else if (m_CurrentFrame == FrameDataView.invalidOrCurrentFrameIndex)
                {
                    m_CurrentFrameEnabled = false;
                    PrevFrame();
                }
                else if (m_CurrentFrameEnabled && m_CurrentFrame >= 0)
                {
                    m_CurrentFrameEnabled = false;
                }
            }

            // Frame number
            var frameCountLabel = new GUIContent(Styles.frame.text + PickFrameLabel());
            float maxWidth, minWidth;
            EditorStyles.toolbarLabel.CalcMinMaxWidth(frameCountLabel, out minWidth, out maxWidth);
            if (minWidth > m_FrameCountLabelMinWidth)
                // to avoid increasing the size in too fine graned intervals, add a 10 pixel buffer.
                m_FrameCountLabelMinWidth = minWidth + 10;
            GUILayout.Label(frameCountLabel, EditorStyles.toolbarLabel, GUILayout.MinWidth(m_FrameCountLabelMinWidth));
        }

        public void SelectAndStayOnLatestFrame()
        {
            SetCurrentFrame(FrameDataView.invalidOrCurrentFrameIndex);
            m_LastFrameFromTick = ProfilerDriver.lastFrameIndex;
            m_CurrentFrameEnabled = true;
        }

        void SetCurrentFrameDontPause(int frame)
        {
            m_CurrentFrame = frame;
            InvokeSelectedFrameIndexChangedEventIfNecessary(frame);
        }

        void SetCurrentFrame(int frame)
        {
            bool shouldPause = frame != FrameDataView.invalidOrCurrentFrameIndex && ProfilerDriver.enabled && !ProfilerDriver.profileEditor && m_CurrentFrame != frame;
            if (shouldPause && EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.isPaused = true;

            currentFrameChanged?.Invoke(frame, shouldPause);

            SetCurrentFrameDontPause(frame);
        }

        internal void SetActiveVisibleFrameIndex(int frame)
        {
            if (frame != FrameDataView.invalidOrCurrentFrameIndex && (frame < ProfilerDriver.firstFrameIndex || frame > ProfilerDriver.lastFrameIndex))
                throw new ArgumentOutOfRangeException($"{nameof(frame)}");

            currentFrameChanged?.Invoke(frame, false);
            SetCurrentFrameDontPause(frame);
            Repaint();
        }

        void DoLegacyGUI_ToolbarAndCharts()
        {
            if (Event.current.isMouse)
                ProfilerWindowAnalytics.RecordProfilerSessionMouseEvent();

            if (Event.current.isKey)
                ProfilerWindowAnalytics.RecordProfilerSessionKeyboardEvent();

            CheckForPlatformModuleChange(); // TODO Move this to an update loop or a callback.

            var toolbarHeight = DrawMainToolbar();

            m_GraphPos = EditorGUILayout.BeginScrollView(m_GraphPos, Styles.profilerGraphBackground);

            // MainSplitView.fixedPane will be null on the first pass through here as MainSplitView picks up its children in its PostDisplaySetup.
            var fixedPaneRect = (MainSplitView.fixedPane != null) ?  MainSplitView.fixedPane.layout : Rect.zero;
            var verticalScrollbarStyle = GUI.skin.verticalScrollbar;
            var scrollViewContentWidth = fixedPaneRect.width - verticalScrollbarStyle.fixedWidth - verticalScrollbarStyle.padding.horizontal;
            var scrollViewViewportHeight = fixedPaneRect.height - toolbarHeight;
            int newCurrentFrame = DrawModuleChartViews(new Vector2(scrollViewContentWidth, scrollViewViewportHeight));
            if (newCurrentFrame != m_CurrentFrame)
            {
                SetCurrentFrame(newCurrentFrame);
                Repaint();
                if (Event.current.type != EventType.Repaint)
                    GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndScrollView();
        }

        int DrawModuleChartViews(Vector2 containerSize)
        {
            // Calculate the total minimum chart height of all active modules.
            var totalMinimumChartHeight = 0f;
            var activeModuleCount = 0;
            var lastActiveModuleIndex = -1;
            for (int i = 0; i < m_Modules.Count; ++i)
            {
                var module = m_Modules[i];
                if (module.active)
                {
                    totalMinimumChartHeight += module.GetMinimumChartHeight();
                    activeModuleCount++;
                    lastActiveModuleIndex = i;
                }
            }

            var newCurrentFrame = m_CurrentFrame;
            if (activeModuleCount > 0)
            {
                // If there will be empty space below the charts, calculate how much to expand each chart by to fill this space.
                var additionalChartHeight = 0f;
                var requiresChartHeightExpansion = totalMinimumChartHeight < containerSize.y;
                if (requiresChartHeightExpansion)
                {
                    var verticalSpaceToFill = containerSize.y - totalMinimumChartHeight;
                    additionalChartHeight = GUIUtility.RoundToPixelGrid(verticalSpaceToFill / activeModuleCount);
                }

                var accumulatedExpandedChartHeight = 0f;
                for (int i = 0; i < m_Modules.Count; ++i)
                {
                    var module = m_Modules[i];
                    if (module.active)
                    {
                        // Calculate final chart height.
                        var chartHeight = module.GetMinimumChartHeight();
                        if (requiresChartHeightExpansion)
                        {
                            // Due to rounding additionalChartHeight to the pixel grid, we make the last chart fill the remaining space. This ensures that exactly the whole space is filled whilst maintaining that all expanded charts remain on the pixel grid.
                            if (i == lastActiveModuleIndex)
                            {
                                var remainingHeightToFill = containerSize.y - accumulatedExpandedChartHeight;
                                chartHeight = remainingHeightToFill;
                            }
                            else
                            {
                                chartHeight += additionalChartHeight;
                                accumulatedExpandedChartHeight += chartHeight;
                            }
                        }

                        // Reserve a chart rect with the layout system.
                        var chartRect = GUILayoutUtility.GetRect(containerSize.x, chartHeight);

                        // Don't draw or update any charts during the layout pass, where rects are not computed yet.
                        if (Event.current.type != EventType.Layout)
                        {
                            // Only draw or update modules that will be visible in the scroll view's viewport.
                            if (GUIClip.visibleRect.Overlaps(chartRect))
                            {
                                // DrawChartView also handles interaction so we can't only call it when repainting.
                                bool isSelected = (m_SelectedModuleIndex == i);
                                var lastVisibleFrameIndex = ProfilerDriver.lastFrameIndex;
                                newCurrentFrame = module.DrawChartView(chartRect, newCurrentFrame, isSelected, lastVisibleFrameIndex);
                            }
                        }
                    }
                }
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(Styles.noActiveModules);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }

            return newCurrentFrame;
        }

        void ProfilerModulesDropdownWindow.IResponder.OnModuleActiveStateChanged()
        {
            m_ToolbarAndChartsIMGUIContainer.MarkDirtyRepaint();

            // If we have no module selected, try to select the first one.
            if (m_SelectedModuleIndex == k_NoModuleSelected)
                SelectFirstActiveModule();
        }

        void ProfilerModulesDropdownWindow.IResponder.OnConfigureModules()
        {
            ModuleEditorWindow moduleEditorWindow;
            if (ModuleEditorWindow.TryGetOpenInstance(out moduleEditorWindow))
            {
                moduleEditorWindow.Focus();
            }
            else
            {
                moduleEditorWindow = ModuleEditorWindow.Present(m_Modules);
                moduleEditorWindow.onChangesConfirmed += OnModuleEditorChangesConfirmed;
            }
        }

        void ProfilerModulesDropdownWindow.IResponder.OnRestoreDefaultModules()
        {
            int index = m_Modules.Count - 1;
            while (index >= 0)
            {
                var module = m_Modules[index];
                if (module is DynamicProfilerModule)
                {
                    DeleteProfilerModuleAtIndex(index);
                }

                module.ResetOrderIndexToDefault();

                index--;
            }

            m_Modules.Sort((a, b) => a.orderIndex.CompareTo(b.orderIndex));

            PersistDynamicModulesToEditorPrefs();
            UpdateModules();
            Repaint();

            if (ModuleEditorWindow.TryGetOpenInstance(out var moduleEditorWindow))
            {
                moduleEditorWindow.Close();
                moduleEditorWindow = ModuleEditorWindow.Present(m_Modules);
                moduleEditorWindow.onChangesConfirmed += OnModuleEditorChangesConfirmed;
            }
        }

        void OnModuleEditorChangesConfirmed(ReadOnlyCollection<ModuleData> modules, ReadOnlyCollection<ModuleData> deletedModules)
        {
            var selectedModuleIndexCached = m_SelectedModuleIndex;

            int index = 0;
            foreach (var moduleData in modules)
            {
                switch (moduleData.editedState)
                {
                    case ModuleData.EditedState.Created:
                    {
                        CreateNewProfilerModule(moduleData, index);
                        break;
                    }

                    case ModuleData.EditedState.Updated:
                    {
                        UpdateProfilerModule(moduleData, index, selectedModuleIndexCached);
                        break;
                    }
                }

                index++;
            }

            foreach (var moduleData in deletedModules)
            {
                DeleteProfilerModule(moduleData);
            }

            // If any modules were deleted, all existing modules should update/refresh their order index.
            bool hasDeletedModules = deletedModules.Count > 0;
            if (hasDeletedModules)
            {
                for (int i = 0; i < m_Modules.Count; i++)
                {
                    var profilerModule = m_Modules[i];
                    profilerModule.orderIndex = i;
                }
            }

            m_Modules.Sort((a, b) => a.orderIndex.CompareTo(b.orderIndex));

            PersistDynamicModulesToEditorPrefs();
            UpdateModules();
            Repaint();
        }

        void CreateNewProfilerModule(ModuleData moduleData, int orderIndex)
        {
            var name = moduleData.name;
            var module = new DynamicProfilerModule(m_ProfilerWindowControllerProxy, name);
            var chartCounters = new List<ProfilerCounterData>(moduleData.chartCounters);
            var detailCounters = new List<ProfilerCounterData>(moduleData.detailCounters);
            module.SetCounters(chartCounters, detailCounters);
            module.orderIndex = orderIndex;

            m_Modules.Add(module);
            module.OnEnable();
        }

        void UpdateProfilerModule(ModuleData moduleData, int orderIndex, int selectedModuleIndexCached)
        {
            var currentProfilerModuleName = moduleData.currentProfilerModuleName;
            int updatedModuleIndex = IndexOfModuleWithName(currentProfilerModuleName);
            if (updatedModuleIndex < 0)
            {
                throw new IndexOutOfRangeException($"Unable to update module '{currentProfilerModuleName}' at index '{updatedModuleIndex}'.");
            }

            var module = m_Modules[updatedModuleIndex];
            var isSelectedIndex = (module.orderIndex == selectedModuleIndexCached);

            module.name = moduleData.name;
            var chartCounters = new List<ProfilerCounterData>(moduleData.chartCounters);
            var detailCounters = new List<ProfilerCounterData>(moduleData.detailCounters);
            module.SetCounters(chartCounters, detailCounters);
            module.orderIndex = orderIndex;

            if (isSelectedIndex)
            {
                m_SelectedModuleIndex = orderIndex;
            }
        }

        void DeleteProfilerModule(ModuleData moduleData)
        {
            var currentProfilerModuleName = moduleData.currentProfilerModuleName;
            int deletedModuleIndex = IndexOfModuleWithName(currentProfilerModuleName);
            DeleteProfilerModuleAtIndex(deletedModuleIndex);
        }

        void DeleteProfilerModuleAtIndex(int index)
        {
            if (index < 0 || index >= m_Modules.Count)
            {
                throw new IndexOutOfRangeException($"Unable to delete module at index '{index}'.");
            }

            var moduleToDelete = m_Modules[index];
            // Ensure that active areas in use are decremented. Additionally, if moduleToDelete is currently selected, the SetActive(false) call will invoke its OnDeselected callback prior to the OnDisable callback below (via its chart's onClosed callback, which invokes the IProfilerWindowController's CloseModule method, which invokes the OnDeselected callback).
            moduleToDelete.active = false;
            moduleToDelete.OnDisable();
            moduleToDelete.DeleteAllPreferences();
            m_Modules.RemoveAt(index);
        }

        int IndexOfModuleWithName(string moduleName)
        {
            int moduleIndex = k_NoModuleSelected;
            for (int i = 0; i < m_Modules.Count; i++)
            {
                var module = m_Modules[i];
                if (module.name.Equals(moduleName))
                {
                    moduleIndex = i;
                    break;
                }
            }

            return moduleIndex;
        }

        void PersistDynamicModulesToEditorPrefs()
        {
            var serializableDynamicModules = DynamicProfilerModule.SerializedDataCollection.FromDynamicProfilerModulesInCollection(m_Modules);
            var json = JsonUtility.ToJson(serializableDynamicModules);
            EditorPrefs.SetString(k_DynamicModulesPreferenceKey, json);
        }

        internal void SetClearOnPlay(bool enabled)
        {
            m_ClearOnPlay = enabled;
        }

        internal bool GetClearOnPlay()
        {
            return m_ClearOnPlay;
        }

        void OnTargetedEditorConnectionChanged(EditorConnectionTarget change)
        {
            switch (change)
            {
                case EditorConnectionTarget.None:
                case EditorConnectionTarget.MainEditorProcessPlaymode:
                    ProfilerDriver.profileEditor = false;
                    recordingStateChanged?.Invoke(m_Recording);
                    break;
                case EditorConnectionTarget.MainEditorProcessEditmode:
                    ProfilerDriver.profileEditor = true;
                    recordingStateChanged?.Invoke(m_Recording);
                    break;
                default:
                    ProfilerDriver.profileEditor = false;
                    if (Unsupported.IsDeveloperMode())
                        Debug.LogError($"{change} is not implemented!");
                    break;
            }

            SessionState.SetBool(kProfilerEditorTargetModeEnabledSessionKey, ProfilerDriver.profileEditor);
        }

        bool IsEditorConnectionTargeted(EditorConnectionTarget connection)
        {
            switch (connection)
            {
                case EditorConnectionTarget.None:
                case EditorConnectionTarget.MainEditorProcessPlaymode:
                case EditorConnectionTarget.MainEditorProcessEditmode:
                    return ProfilerDriver.profileEditor;
                default:
                    if (Unsupported.IsDeveloperMode())
                        Debug.LogError($"{connection} is not implemented!");
                    return ProfilerDriver.profileEditor == false;
            }
        }

        void OnConnectedToPlayer(string player, EditorConnectionTarget? editorConnectionTarget)
        {
            if (editorConnectionTarget == null || editorConnectionTarget.Value == EditorConnectionTarget.None)
                ClearFramesOnPlayOrPlayerConnectionChange();
        }

        internal static bool SetEditorDeepProfiling(bool deep)
        {
            var doApply = true;

            // When enabling / disabling deep script profiling we need to reload scripts.
            // In play mode this might be intrusive. So ask the user first.
            if (EditorApplication.isPlaying)
            {
                if (deep)
                    doApply = EditorUtility.DisplayDialog(Styles.enableDeepProfilingWarningDialogTitle, Styles.enableDeepProfilingWarningDialogContent, Styles.domainReloadWarningDialogButton, Styles.cancelDialogButton, DialogOptOutDecisionType.ForThisSession, kProfilerDeepProfilingWarningSessionKey);
                else
                    doApply = EditorUtility.DisplayDialog(Styles.disableDeepProfilingWarningDialogTitle, Styles.disableDeepProfilingWarningDialogContent, Styles.domainReloadWarningDialogButton, Styles.cancelDialogButton, DialogOptOutDecisionType.ForThisSession, kProfilerDeepProfilingWarningSessionKey);
            }

            if (doApply)
            {
                ProfilerDriver.deepProfiling = deep;
                EditorUtility.RequestScriptReload();
            }

            return doApply;
        }

        void SetAreasInUse(IEnumerable<ProfilerArea> areas, bool inUse)
        {
            if (inUse)
            {
                foreach (var area in areas)
                {
                    m_AreaReferenceCounter.IncrementArea(area);
                }
            }
            else
            {
                foreach (var area in areas)
                {
                    m_AreaReferenceCounter.DecrementArea(area);
                }
            }
        }

        /// <summary>
        /// Select the Profiler module at the specified index. The currently selected Profiler module will be deselected.
        /// </summary>
        /// <param name="index"></param>
        void SelectModuleAtIndex(int index)
        {
            var module = ModuleAtIndex(index);
            SelectModuleWithIndexAndDeselectSelectedModuleIfNecessary(module, index);
        }

        /// <summary>
        /// Select the specified Profiler module. The currently selected Profiler module will be deselected.
        /// </summary>
        /// <param name="module"></param>
        void SelectModule(ProfilerModuleBase module)
        {
            var index = IndexOfModule(module);
            SelectModuleWithIndexAndDeselectSelectedModuleIfNecessary(module, index);
        }

        /// <summary>
        /// Select the first active Profiler module. The currently selected Profiler module will be deselected.
        /// </summary>
        void SelectFirstActiveModule()
        {
            var moduleIndexToSelect = k_NoModuleSelected;
            for (int i = 0; i < m_Modules.Count; ++i)
            {
                var module = m_Modules[i];
                if (module.active)
                {
                    moduleIndexToSelect = i;
                    break;
                }
            }

            SelectModuleAtIndex(moduleIndexToSelect);
        }

        void SelectModuleWithIndexAndDeselectSelectedModuleIfNecessary(ProfilerModuleBase moduleToSelect, int moduleIndexToSelect)
        {
            DeselectSelectedModuleIfNecessary();

            if (moduleToSelect != null)
            {
                // Ensure the module being selected is active.
                if (!moduleToSelect.active)
                {
                    moduleToSelect.active = true;
                }

                // Create the module's details view and add it to the hierarchy.
                var detailsView = moduleToSelect.CreateDetailsView();
                if (detailsView == null)
                    throw new ArgumentNullException($"{moduleToSelect.name} did not provide a details view.");
                m_DetailsViewContainer.Add(detailsView);

                m_SelectedModuleIndex = moduleIndexToSelect;
            }
        }

        void DeselectSelectedModuleIfNecessary()
        {
            DeselectModuleAtIndexIfNecessary(m_SelectedModuleIndex);
        }

        void DeselectModuleAtIndexIfNecessary(int index)
        {
            var moduleIndexToDeselect = index;
            var moduleToDeselect = ModuleAtIndex(moduleIndexToDeselect);
            if (moduleToDeselect != null)
            {
                moduleToDeselect.CloseDetailsView();
                m_SelectedModuleIndex = k_NoModuleSelected;
            }
        }

        int IndexOfModule(ProfilerModuleBase module)
        {
            int index = k_NoModuleSelected;
            for (int i = 0; i < m_Modules.Count; i++)
            {
                var m = m_Modules[i];
                if (m.Equals(module))
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        ProfilerModuleBase ModuleAtIndex(int index)
        {
            if ((index != k_NoModuleSelected) && (index >= 0) && (index < m_Modules.Count))
            {
                return m_Modules[index];
            }

            return null;
        }

        // TODO Ideally we wouldn't need this method. However, the current IMGUI tangle means setting the current frame can occur an unpredictable number of times per frame. We want to ensure we only invoke this event once for the selected frame. Fully transitioning to UIToolkit (especially on the toolbar) should simplify this.
        int m_LastReportedSelectedFrameIndex;
        void InvokeSelectedFrameIndexChangedEventIfNecessary(int newFrame)
        {
            if (newFrame != m_LastReportedSelectedFrameIndex)
            {
                SelectedFrameIndexChanged?.Invoke(selectedFrameIndex);
                m_LastReportedSelectedFrameIndex = newFrame;
            }
        }

        long IProfilerWindowController.selectedFrameIndex { get => selectedFrameIndex; set => selectedFrameIndex = value; }
        ProfilerModuleBase IProfilerWindowController.selectedModule { get => selectedModule; set => selectedModule = value; }
        ProfilerModuleBase IProfilerWindowController.GetProfilerModuleByType(Type T) => GetProfilerModuleByType(T);
        void IProfilerWindowController.Repaint() => Repaint();


        event Action<int, bool> IProfilerWindowController.currentFrameChanged
        {
            add { currentFrameChanged += value; }
            remove { currentFrameChanged -= value; }
        }

        event Action IProfilerWindowController.frameDataViewAboutToBeDisposed
        {
            add { frameDataViewAboutToBeDisposed += value; }
            remove { frameDataViewAboutToBeDisposed -= value; }
        }

        void IProfilerWindowController.SetClearOnPlay(bool enabled) => SetClearOnPlay(enabled);
        bool IProfilerWindowController.GetClearOnPlay() => GetClearOnPlay();
        HierarchyFrameDataView IProfilerWindowController.GetFrameDataView(string groupName, string threadName, ulong threadId, HierarchyFrameDataView.ViewModes viewMode, int profilerSortColumn, bool sortAscending)
            => GetFrameDataView(groupName, threadName, threadId, viewMode, profilerSortColumn, sortAscending);
        HierarchyFrameDataView IProfilerWindowController.GetFrameDataView(int threadIndex, HierarchyFrameDataView.ViewModes viewMode, int profilerSortColumn, bool sortAscending)
            => GetFrameDataView(threadIndex, viewMode, profilerSortColumn, sortAscending);
        bool IProfilerWindowController.IsRecording() => IsRecording();
        bool IProfilerWindowController.ProfilerWindowOverheadIsAffectingProfilingRecordingData() => ProfilerWindowOverheadIsAffectingProfilingRecordingData();
        string IProfilerWindowController.ConnectedTargetName { get => ConnectedTargetName; }
        bool IProfilerWindowController.ConnectedToEditor { get => ConnectedToEditor; }
        ProfilerProperty IProfilerWindowController.CreateProperty() => CreateProperty();
        ProfilerProperty IProfilerWindowController.CreateProperty(int sortType) => CreateProperty(sortType);
        void IProfilerWindowController.CloseModule(ProfilerModuleBase module) => CloseModule(module);
        void IProfilerWindowController.SetAreasInUse(IEnumerable<ProfilerArea> areas, bool inUse) => SetAreasInUse(areas, inUse);

        // Using a serializable proxy object that does not inherit from UnityEngine.Object allows the modules to use [SerializeReference] to hold a reference to the window across domain reloads.
        [Serializable]
        internal class ProfilerWindowControllerProxy : IProfilerWindowController, ProfilerModulesDropdownWindow.IResponder
        {
            // It is tempting to change this to ProfilerWindow and it will look like its working. Except for weird issues during domain reload...
            // Because it NEEDS to be an interface or at the very least NOT the ProfilerWindow, because that would then break with [SerializeReference]
            IProfilerWindowController m_ProfilerWindowController;

            public void SetRealSubject(IProfilerWindowController profilerWindowController)
            {
                if (profilerWindowController as ProfilerModulesDropdownWindow.IResponder == null)
                    throw new ArgumentException($"The passed {nameof(profilerWindowController)} did not implement {nameof(ProfilerModulesDropdownWindow.IResponder)}", $"{nameof(profilerWindowController)}");
                m_ProfilerWindowController = profilerWindowController;
            }

            string IProfilerWindowController.ConnectedTargetName => m_ProfilerWindowController.ConnectedTargetName;

            bool IProfilerWindowController.ConnectedToEditor => m_ProfilerWindowController.ConnectedToEditor;


            event Action<int, bool> IProfilerWindowController.currentFrameChanged
            {
                add { m_ProfilerWindowController.currentFrameChanged += value; }
                remove { m_ProfilerWindowController.currentFrameChanged -= value; }
            }

            public event Action frameDataViewAboutToBeDisposed
            {
                add { m_ProfilerWindowController.frameDataViewAboutToBeDisposed += value; }
                remove { m_ProfilerWindowController.frameDataViewAboutToBeDisposed -= value; }
            }

            long IProfilerWindowController.selectedFrameIndex { get => m_ProfilerWindowController.selectedFrameIndex; set => m_ProfilerWindowController.selectedFrameIndex = value; }

            public ProfilerModuleBase selectedModule
            {
                get => m_ProfilerWindowController.selectedModule;
                set => m_ProfilerWindowController.selectedModule = value;
            }

            void IProfilerWindowController.CloseModule(ProfilerModuleBase module)
            {
                m_ProfilerWindowController.CloseModule(module);
            }

            ProfilerProperty IProfilerWindowController.CreateProperty()
            {
                return m_ProfilerWindowController.CreateProperty();
            }

            ProfilerProperty IProfilerWindowController.CreateProperty(int sortType)
            {
                return m_ProfilerWindowController.CreateProperty(sortType);
            }

            bool IProfilerWindowController.GetClearOnPlay()
            {
                return m_ProfilerWindowController.GetClearOnPlay();
            }

            HierarchyFrameDataView IProfilerWindowController.GetFrameDataView(string groupName, string threadName, ulong threadId, HierarchyFrameDataView.ViewModes viewMode, int profilerSortColumn, bool sortAscending)
            {
                return m_ProfilerWindowController.GetFrameDataView(groupName, threadName, threadId, viewMode, profilerSortColumn, sortAscending);
            }

            HierarchyFrameDataView IProfilerWindowController.GetFrameDataView(int threadIndex, HierarchyFrameDataView.ViewModes viewMode, int profilerSortColumn, bool sortAscending)
            {
                return m_ProfilerWindowController.GetFrameDataView(threadIndex, viewMode, profilerSortColumn, sortAscending);
            }

            bool IProfilerWindowController.IsRecording()
            {
                return m_ProfilerWindowController.IsRecording();
            }

            bool IProfilerWindowController.ProfilerWindowOverheadIsAffectingProfilingRecordingData()
            {
                return m_ProfilerWindowController.ProfilerWindowOverheadIsAffectingProfilingRecordingData();
            }

            void IProfilerWindowController.SetAreasInUse(IEnumerable<ProfilerArea> areas, bool inUse)
            {
                m_ProfilerWindowController.SetAreasInUse(areas, inUse);
            }

            void IProfilerWindowController.SetClearOnPlay(bool enabled)
            {
                m_ProfilerWindowController.SetClearOnPlay(enabled);
            }

            ProfilerModuleBase IProfilerWindowController.GetProfilerModuleByType(Type T)
            {
                return m_ProfilerWindowController.GetProfilerModuleByType(T);
            }

            void IProfilerWindowController.Repaint()
            {
                m_ProfilerWindowController.Repaint();
            }

            void ProfilerModulesDropdownWindow.IResponder.OnModuleActiveStateChanged()
                => (m_ProfilerWindowController as ProfilerModulesDropdownWindow.IResponder).OnModuleActiveStateChanged();
            void ProfilerModulesDropdownWindow.IResponder.OnConfigureModules()
                => (m_ProfilerWindowController as ProfilerModulesDropdownWindow.IResponder).OnConfigureModules();
            void ProfilerModulesDropdownWindow.IResponder.OnRestoreDefaultModules()
                => (m_ProfilerWindowController as ProfilerModulesDropdownWindow.IResponder).OnRestoreDefaultModules();
        }
    }
}
