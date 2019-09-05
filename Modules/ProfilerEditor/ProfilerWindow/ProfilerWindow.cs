// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Accessibility;
using UnityEditorInternal;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditorInternal.Profiling;
using UnityEngine.Scripting;
using UnityEngine.Experimental.Networking.PlayerConnection;
using ConnectionUtility = UnityEditor.Experimental.Networking.PlayerConnection.EditorGUIUtility;
using ConnectionGUILayout = UnityEditor.Experimental.Networking.PlayerConnection.EditorGUILayout;
using UnityEditor.Experimental.Networking.PlayerConnection;
using UnityEditor.StyleSheets;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Profiler", useTypeNameAsIconName = true)]
    internal class ProfilerWindow : EditorWindow, IProfilerWindowController, IHasCustomMenu
    {
        internal static class Styles
        {
            public static readonly GUIContent addArea = EditorGUIUtility.TrTextContent("Profiler Modules", "Add and remove profiler modules");
            public static readonly GUIContent deepProfile = EditorGUIUtility.TrTextContent("Deep Profile", "Instrument all scripting method calls to investigate scripts");
            public static readonly GUIContent deepProfileNotSupported = EditorGUIUtility.TrTextContent("Deep Profile", "Build a Player with Deep Profiling Support to be able to enable instrumentation of all scripting methods in a Player.");
            public static readonly GUIContent noData = EditorGUIUtility.TrTextContent("No frame data available");

            public static readonly string enableDeepProfilingWarningDialogTitle = L10n.Tr("Enable deep script profiling");
            public static readonly string enableDeepProfilingWarningDialogContent = L10n.Tr("Enabling deep profiling requires reloading scripts.");
            public static readonly string disableDeepProfilingWarningDialogTitle = L10n.Tr("Disable deep script profiling");
            public static readonly string disableDeepProfilingWarningDialogContent = L10n.Tr("Disabling deep profiling requires reloading all scripts.");
            public static readonly string domainReloadWarningDialogButton = L10n.Tr("Reload");
            public static readonly string cancelDialogButton = L10n.Tr("Cancel");

            public static readonly GUIContent recordCallstacks = EditorGUIUtility.TrTextContent("Call Stacks", "Record call stacks for special samples such as \"GC.Alloc\". " +
                "To see the call stacks, select a sample in the CPU Usage module, e.g. in Timeline view. " +
                "To also see call stacks in Hierarchy view, switch from \"No Details\" to \"Show Related Objects\", select a \"GC.Alloc\" sample and select \"N/A\" items from the list.");
            public static readonly GUIContent profilerRecordOff = EditorGUIUtility.TrIconContent("Record Off", "Record profiling information");
            public static readonly GUIContent profilerRecordOn = EditorGUIUtility.TrIconContent("Record On", "Record profiling information");
            public static Color recordOnColor = AnimationMode.recordedPropertyColor;
            public static Color recordOffColor = new Color(1f, 1f , 1f, 0.5f);

            public static SVC<Color> borderColor =
                new SVC<Color>("--theme-profiler-border-color-darker", Color.black);
            public static readonly GUIContent prevFrame = EditorGUIUtility.TrIconContent("Animation.PrevKey", "Go back one frame");
            public static readonly GUIContent nextFrame = EditorGUIUtility.TrIconContent("Animation.NextKey", "Go one frame forwards");
            public static readonly GUIContent currentFrame = EditorGUIUtility.TrIconContent("Animation.LastKey", "Go to (and stay on) current frame");
            public static readonly GUIContent frame = EditorGUIUtility.TrTextContent("Frame: ");
            public static readonly GUIContent clearOnPlay = EditorGUIUtility.TrTextContent("Clear on Play");
            public static readonly GUIContent clearData = EditorGUIUtility.TrTextContent("Clear", "Clear");
            public static readonly GUIContent saveWindowTitle = EditorGUIUtility.TrTextContent("Save Window");
            public static readonly GUIContent saveProfilingData = EditorGUIUtility.TrIconContent("SaveAs", "Save current profiling information to a binary file");
            public static readonly GUIContent loadWindowTitle = EditorGUIUtility.TrTextContent("Load Window");
            public static readonly GUIContent loadProfilingData = EditorGUIUtility.TrIconContent("Import", "Load binary profiling information from a file. Shift click to append to the existing data");
            public static readonly string[] loadProfilingDataFileFilters = new string[] { L10n.Tr("Profiler files"), "data,raw", L10n.Tr("All files"), "*" };

            public static readonly GUIContent optionsButtonContent = EditorGUIUtility.TrIconContent("_Menu", "Options");
            public static readonly GUIContent helpButtonContent = EditorGUIUtility.TrIconContent("_Help", "Open Manual");
            public const string linkToManual = "https://docs.unity3d.com/Manual/ProfilerWindow.html";
            public static readonly GUIContent preferencesButtonContent = EditorGUIUtility.TrTextContent("Preferences", "Open User Preferences for the Profiler");

            public static readonly GUIContent accessibilityModeLabel = EditorGUIUtility.TrTextContent("Color Blind Mode");

            public static readonly GUIStyle background = "OL box flat";
            public static readonly GUIStyle header = "OL title";
            public static readonly GUIStyle label = "OL label";
            public static readonly GUIStyle entryEven = "OL EntryBackEven";
            public static readonly GUIStyle entryOdd = "OL EntryBackOdd";
            public static readonly GUIStyle profilerGraphBackground = "ProfilerScrollviewBackground";
            public static readonly GUIStyle profilerDetailViewBackground = "ProfilerDetailViewBackground";

            static Styles()
            {
                profilerGraphBackground.overflow.left = -(int)Chart.kSideWidth;
            }
        }

        private const string k_CPUUnstackableSeriesName = "Others";
        private static readonly ProfilerArea[] ms_StackedAreas = { ProfilerArea.CPU, ProfilerArea.GPU, ProfilerArea.UI, ProfilerArea.GlobalIllumination };

        [NonSerialized]
        bool m_Initialized;

        [SerializeField]
        SplitterState m_VertSplit;


        const int k_VertSplitterMinSizes = 100;
        const float k_LineHeight = 16.0f;
        const float k_RightPaneMinSize = 700;

        // For keeping correct "Recording" state on window maximizing
        [SerializeField]
        private bool m_Recording;

        private IConnectionStateInternal m_AttachProfilerState;

        private Vector2 m_GraphPos = Vector2.zero;

        [SerializeField]
        string m_ActiveNativePlatformSupportModule = null;

        static List<ProfilerWindow> m_ProfilerWindows = new List<ProfilerWindow>();

        // used by Tests/PerformanceTests/Profiler ProfilerWindowTests.CPUViewTests through reflection
        [SerializeField]
        ProfilerArea m_CurrentArea = ProfilerArea.CPU;
        const ProfilerArea k_InvalidArea = unchecked((ProfilerArea)Profiler.invalidProfilerArea);

        int m_CurrentFrame = -1;
        int m_LastFrameFromTick = -1;
        int m_PrevLastFrame = -1;

        // Profiler charts
        // used by Tests/PerformanceTests/Profiler ProfilerWindowTests.CPUViewTests through reflection
        ProfilerChart[] m_Charts;

        float[] m_ChartOldMax = new[]
        {
            -1.0f, // Cpu
            -1.0f, // Gpu
            0, // Rendering,
            0, // Memory,
            0, // Audio,
            0, // Video,
            0, // Physics,
            0, // Physics2D,
            0, // NetworkMessages,
            0, // NetworkOperations,
            -1.0f, // UI,
            0, // UIDetails,
            0, // GlobalIllumination,
            0, // AreaCount,
        };
        float m_ChartMaxClamp = 70000.0f;

        // Profiling GUI constants
        const float kRowHeight = 16;
        const float kIndentPx = 16;
        const float kBaseIndent = 8;

        const float kNameColumnSize = 350;

        HierarchyFrameDataView m_FrameDataView;

        // used by Tests/PerformanceTests/Profiler ProfilerWindowTests.CPUViewTests through reflection
        ProfilerModuleBase[] m_ProfilerModules;

        // used by Tests/PerformanceTests/Profiler ProfilerWindowTests.CPUViewTests.SelectAndDisplayDetailsForAFrame_WithSearchFiltering to avoid brittle tests due to reflection
        internal T GetProfilerModule<T>(ProfilerArea area) where T : ProfilerModuleBase
        {
            var index = (int)area;
            if (index >= 0 && index < m_ProfilerModules.Length)
                return m_ProfilerModules[index] as T;
            return null;
        }

        private ProfilerMemoryRecordMode m_SelectedMemRecordMode = ProfilerMemoryRecordMode.None;
        private ProfilerMemoryRecordMode m_LastSelectedMemRecordMode = ProfilerMemoryRecordMode.None;


        public string ConnectedTargetName => m_AttachProfilerState.connectionName;

        public bool ConnectedToEditor => m_AttachProfilerState.connectedToTarget == ConnectionTarget.Editor;

        [SerializeField]
        private bool m_ClearOnPlay;

        const string kProfilerRecentSaveLoadProfilePath = "ProfilerRecentSaveLoadProfilePath";
        const string kProfilerEnabledSessionKey = "ProfilerEnabled";
        const string kProfilerDeepProfilingWarningSessionKey = "ProfilerDeepProfilingWarning";

        internal delegate void SelectionChangedCallback(string selectedPropertyPath);
        public event SelectionChangedCallback selectionChanged = delegate {};
        internal event Action<int, bool> currentFrameChanged = delegate {};
        internal event Action<bool> recordingStateChanged = delegate {};
        internal event Action<bool> deepProfileChanged = delegate {};
        internal event Action<ProfilerMemoryRecordMode> memoryRecordingModeChanged = delegate {};

        public void SetSelectedPropertyPath(string path)
        {
            if (ProfilerDriver.selectedPropertyPath != path)
            {
                ProfilerDriver.selectedPropertyPath = path;
                selectionChanged.Invoke(path);
                UpdateCharts();
            }
        }

        public void ClearSelectedPropertyPath()
        {
            if (ProfilerDriver.selectedPropertyPath != string.Empty)
            {
                ProfilerDriver.selectedPropertyPath = string.Empty;
                selectionChanged.Invoke(string.Empty);
                UpdateCharts();
            }
        }

        public ProfilerProperty CreateProperty()
        {
            return CreateProperty(HierarchyFrameDataView.columnDontSort);
        }

        public ProfilerProperty CreateProperty(int sortType)
        {
            int targetedFrame = GetActiveVisibleFrameIndex();
            if (targetedFrame < ProfilerDriver.lastFrameIndex - ProfilerUserSettings.frameCount)
            {
                return null;
            }

            var property = new ProfilerProperty();
            property.SetRoot(targetedFrame, sortType, (int)ProfilerViewType.Hierarchy);
            property.onlyShowGPUSamples = m_CurrentArea == ProfilerArea.GPU;
            return property;
        }

        public int GetActiveVisibleFrameIndex()
        {
            // Update the current frame only at fixed intervals,
            // otherwise it looks weird when it is rapidly jumping around when we have a lot of repaints
            return m_CurrentFrame == -1 ? m_LastFrameFromTick : m_CurrentFrame;
        }

        public bool IsRecording()
        {
            return m_Recording && ((EditorApplication.isPlaying && !EditorApplication.isPaused) || !ProfilerDriver.IsConnectionEditor());
        }

        void OnEnable()
        {
            InitializeIfNeeded();

            titleContent = GetLocalizedTitleContent();
            m_ProfilerWindows.Add(this);
            EditorApplication.playModeStateChanged += OnPlaymodeStateChanged;
            UserAccessiblitySettings.colorBlindConditionChanged += Initialize;
            ProfilerUserSettings.settingsChanged += OnSettingsChanged;

            foreach (var module in m_ProfilerModules)
            {
                module?.OnEnable(this);
            }
        }

        void InitializeIfNeeded()
        {
            if (m_Initialized)
                return;

            Initialize();
        }

        void Initialize()
        {
            // When reinitializing (e.g. because Colorblind mode or PlatformModule changed) we don't need a new state
            if (m_AttachProfilerState == null)
                m_AttachProfilerState = ConnectionUtility.GetAttachToPlayerState(this, OnTargetedEditorConnectionChanged, IsEditorConnectionTargeted, (player) => ClearFramesCallback()) as IConnectionStateInternal;

            int historySize = ProfilerUserSettings.frameCount;

            m_Charts = new ProfilerChart[Profiler.areaCount];

            Color[] chartAreaColors = ProfilerColors.chartAreaColors;

            for (int i = 0; i < Profiler.areaCount; i++)
            {
                float scale = 1.0f;
                Chart.ChartType chartType = Chart.ChartType.Line;
                string[] statisticsNames = ProfilerDriver.GetGraphStatisticsPropertiesForArea((ProfilerArea)i);
                int length = statisticsNames.Length;
                if (Array.IndexOf(ms_StackedAreas, (ProfilerArea)i) != -1)
                {
                    chartType = Chart.ChartType.StackedFill;
                    scale = 1.0f / 1000.0f;
                }

                ProfilerChart chart = CreateProfilerChart((ProfilerArea)i, chartType, scale, length);

                if (chart.m_Area == ProfilerArea.CPU)
                {
                    chart.SetOnSeriesToggleCallback(OnToggleCPUChartSeries);
                }

                for (int s = 0; s < length; s++)
                {
                    chart.m_Series[s] = new ChartSeriesViewData(statisticsNames[s], historySize, chartAreaColors[s % chartAreaColors.Length]);
                    for (int frameIdx = 0; frameIdx < historySize; ++frameIdx)
                        chart.m_Series[s].xValues[frameIdx] = (float)frameIdx;
                }

                m_Charts[(int)i] = chart;
            }

            if (m_VertSplit == null || m_VertSplit.relativeSizes == null || m_VertSplit.relativeSizes.Length == 0)
                m_VertSplit = new SplitterState(new[] { 50f, 50f }, new[] { k_VertSplitterMinSizes, k_VertSplitterMinSizes }, null);
            // 2 times the min splitter size plus one line height for the toolbar up top
            minSize = new Vector2(Chart.kSideWidth + k_RightPaneMinSize, k_VertSplitterMinSizes * m_VertSplit.minSizes.Length + k_LineHeight);

            // TODO: only create modules for active charts and otherwise lazy initialize them.
            if (m_ProfilerModules == null)
            {
                m_ProfilerModules = new ProfilerModuleBase[]
                {
                    new CPUProfilerModule(), //CPU
                    new GPUProfilerModule(), //GPU
                    new RenderingProfilerModule(), //Rendering
                    new MemoryProfilerModule(), //Memory
                    new AudioProfilerModule(), //Audio
                    new VideoProfilerModule(), //Video
                    new PhysicsProfilerModule(), //Physics
                    new Physics2DProfilerModule(), //Physics2D
                    new NetworkingMessagesProfilerModule(), //NetworkMessages
                    new NetworkingOperationsProfilerModule(), //NetworkOperations
                    new UIProfilerModule(), //UI
                    new UIDetailsProfilerModule(), //UIDetails
                    new GlobalIlluminationProfilerModule(), //GlobalIllumination
                };
            }

            foreach (var module in m_ProfilerModules)
            {
                module?.OnEnable(this);
            }

            UpdateCharts();
            foreach (var chart in m_Charts)
                chart.LoadAndBindSettings();

            m_Initialized = true;
        }

        void OnSettingsChanged()
        {
            Initialize();
        }

        void OnToggleCPUChartSeries(bool wasToggled)
        {
            if (wasToggled)
            {
                int historyLength = ProfilerUserSettings.frameCount;
                int firstEmptyFrame = ProfilerDriver.lastFrameIndex - historyLength;
                int firstFrame = Mathf.Max(ProfilerDriver.firstFrameIndex, firstEmptyFrame);

                ComputeChartScaleValue(ProfilerArea.CPU, historyLength, firstEmptyFrame, firstFrame);
            }
        }

        ProfilerChart CreateProfilerChart(ProfilerArea i, Chart.ChartType chartType, float scale, int length)
        {
            ProfilerChart newChart = (i == ProfilerArea.UIDetails)
                ? new UISystemProfilerChart(chartType, scale, length)
                : new ProfilerChart(i, chartType, scale, length);
            newChart.selected += OnChartSelected;
            newChart.closed += OnChartClosed;
            return newChart;
        }

        void OnChartClosed(Chart sender)
        {
            ProfilerChart profilerChart = (ProfilerChart)sender;
            m_CurrentArea = k_InvalidArea;
            profilerChart.active = false;
            m_ProfilerModules[(int)profilerChart.m_Area].OnDisable();
            m_ProfilerModules[(int)profilerChart.m_Area].OnClosed();
            m_CurrentArea = k_InvalidArea;
        }

        void OnChartSelected(Chart sender)
        {
            ProfilerArea newArea = ((ProfilerChart)sender).m_Area;

            if (m_CurrentArea == newArea)
                return;
            var oldArea = m_CurrentArea;
            m_CurrentArea = newArea;

            // if switched out of CPU area, reset selected property
            if (m_CurrentArea != ProfilerArea.CPU)
            {
                ClearSelectedPropertyPath();
            }

            if (oldArea != k_InvalidArea)
            {
                m_ProfilerModules[(int)oldArea].OnDisable();
            }

            m_ProfilerModules[(int)newArea].OnEnable(this);

            Repaint();
            GUIUtility.keyboardControl = 0;
            GUIUtility.ExitGUI();
        }

        void CheckForPlatformModuleChange()
        {
            if (m_ActiveNativePlatformSupportModule == null)
            {
                m_ActiveNativePlatformSupportModule = EditorUtility.GetActiveNativePlatformSupportModuleName();
                return;
            }


            if (m_ActiveNativePlatformSupportModule != EditorUtility.GetActiveNativePlatformSupportModuleName())
            {
                ProfilerDriver.ClearAllFrames();
                Initialize();
                m_ActiveNativePlatformSupportModule = EditorUtility.GetActiveNativePlatformSupportModuleName();
            }
        }

        void OnDisable()
        {
            m_AttachProfilerState.Dispose();
            m_AttachProfilerState = null;
            m_ProfilerWindows.Remove(this);
            foreach (var module in m_ProfilerModules)
            {
                module.OnDisable();
            }
            EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;
            UserAccessiblitySettings.colorBlindConditionChanged -= Initialize;
            ProfilerUserSettings.settingsChanged -= OnSettingsChanged;
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

            m_SelectedMemRecordMode = ProfilerDriver.memoryRecordMode;
        }

        void OnPlaymodeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.EnteredPlayMode)
            {
                ClearFramesCallback();
            }
        }

        void ClearFramesCallback()
        {
            if (m_ClearOnPlay)
                Clear();
        }

        void OnDestroy()
        {
            // When window is destroyed, we disable profiling
            if (Profiler.supported && !EditorApplication.isPlayingOrWillChangePlaymode)
                ProfilerDriver.enabled = false;
        }

        void OnFocus()
        {
            // set the real state of profiler. OnDestroy is called not only when window is destroyed, but also when maximized state is changed
            if (Profiler.supported)
                ProfilerDriver.enabled = m_Recording;
        }

        void OnLostFocus()
        {
            if (GUIUtility.hotControl != 0)
            {
                // The chart may not have had the chance to release the hot control before we lost focus.
                // This happens when changing the selected frame, which may pause the game and switch the focus to another view.
                for (int c = 0; c < m_Charts.Length; ++c)
                {
                    ProfilerChart chart = m_Charts[c];
                    chart.OnLostFocus();
                }
            }
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(Styles.accessibilityModeLabel, UserAccessiblitySettings.colorBlindCondition != ColorBlindCondition.Default, OnToggleColorBlindMode);
        }

        private void OnToggleColorBlindMode()
        {
            UserAccessiblitySettings.colorBlindCondition = UserAccessiblitySettings.colorBlindCondition == ColorBlindCondition.Default
                ? ColorBlindCondition.Deuteranopia
                : ColorBlindCondition.Default;
        }

        // Used by Native method DoBuildPlayer_PostBuild() in BuildPlayer.cpp
        [MenuItem("Window/Analysis/Profiler %7", false, 0)]
        static void ShowProfilerWindow()
        {
            if (ProfilerUserSettings.useOutOfProcessProfiler && Unity.MPE.ProcessService.level == Unity.MPE.ProcessLevel.UMP_MASTER)
                ProfilerRoleProvider.LaunchProfilerSlave();
            else
                EditorWindow.GetWindow<ProfilerWindow>(false);
        }

        [RequiredByNativeCode]
        static void RepaintAllProfilerWindows()
        {
            foreach (ProfilerWindow window in m_ProfilerWindows)
            {
                // This is useful hack when you need to profile in the editor and dont want it to affect your framerate...
                // NOTE: we should make this an option in the UI somehow...
                //if (ProfilerDriver.lastFrameIndex != window.m_LastFrameFromTick && EditorWindow.focusedWindow == window)

                if (ProfilerDriver.lastFrameIndex != window.m_LastFrameFromTick)
                {
                    window.m_LastFrameFromTick = ProfilerDriver.lastFrameIndex;
                    window.RepaintImmediately();
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
            return ((m_CurrentFrame == -1) ? lastFrame : m_CurrentFrame + 1) + " / " + lastFrame;
        }

        void PrevFrame()
        {
            int previousFrame = ProfilerDriver.GetPreviousFrameIndex(m_CurrentFrame);
            if (previousFrame != -1)
                SetCurrentFrame(previousFrame);
        }

        void NextFrame()
        {
            int nextFrame = ProfilerDriver.GetNextFrameIndex(m_CurrentFrame);
            if (nextFrame != -1)
                SetCurrentFrame(nextFrame);
        }

        static bool CheckFrameData(ProfilerProperty property)
        {
            return property != null && property.frameDataReady;
        }

        public HierarchyFrameDataView GetFrameDataView(string threadName, HierarchyFrameDataView.ViewModes viewMode, int profilerSortColumn, bool sortAscending)
        {
            var frameIndex = GetActiveVisibleFrameIndex();
            var threadIndex = 0;
            using (var frameIterator = new ProfilerFrameDataIterator())
            {
                var threadCount = frameIterator.GetThreadCount(frameIndex);
                for (var i = 0; i < threadCount; ++i)
                {
                    frameIterator.SetRoot(frameIndex, i);
                    var grp = frameIterator.GetGroupName();
                    var thrd = frameIterator.GetThreadName();
                    var name = string.IsNullOrEmpty(grp) ? thrd : grp + "." + thrd;
                    if (threadName == name)
                    {
                        threadIndex = i;
                        break;
                    }
                }
            }

            if (m_FrameDataView != null && m_FrameDataView.valid)
            {
                if (m_FrameDataView.frameIndex == frameIndex && m_FrameDataView.threadIndex == threadIndex && m_FrameDataView.viewMode == viewMode)
                    return m_FrameDataView;
            }

            if (m_FrameDataView != null)
                m_FrameDataView.Dispose();

            m_FrameDataView = new HierarchyFrameDataView(frameIndex, threadIndex, viewMode, profilerSortColumn, sortAscending);
            return m_FrameDataView;
        }

        private static void UpdateChartGrid(float timeMax, ChartViewData data)
        {
            if (timeMax < 1500)
            {
                data.SetGrid(new float[] { 1000, 250, 100 }, new[] { "1ms (1000FPS)", "0.25ms (4000FPS)", "0.1ms (10000FPS)" });
            }
            else if (timeMax < 10000)
            {
                data.SetGrid(new float[] { 8333, 4000, 1000 }, new[] { "8ms (120FPS)", "4ms (250FPS)", "1ms (1000FPS)" });
            }
            else if (timeMax < 30000)
            {
                data.SetGrid(new float[] { 16667, 10000, 5000 }, new[] { "16ms (60FPS)", "10ms (100FPS)", "5ms (200FPS)" });
            }
            else if (timeMax < 100000)
            {
                data.SetGrid(new float[] { 66667, 33333, 16667 }, new[] { "66ms (15FPS)", "33ms (30FPS)", "16ms (60FPS)" });
            }
            else
            {
                data.SetGrid(new float[] { 500000, 200000, 66667 }, new[] { "500ms (2FPS)", "200ms (5FPS)", "66ms (15FPS)" });
            }
        }

        private void UpdateCharts()
        {
            int historyLength = ProfilerUserSettings.frameCount;
            int firstEmptyFrame = ProfilerDriver.lastFrameIndex - historyLength;
            int firstFrame = Mathf.Max(ProfilerDriver.firstFrameIndex, firstEmptyFrame);

            // Collect chart values
            foreach (var chart in m_Charts)
            {
                UpdateSingleChart(chart, firstEmptyFrame, firstFrame);
            }

            // CPU chart overlay values
            string selectedName = ProfilerDriver.selectedPropertyPath;
            bool hasCPUOverlay = (selectedName != string.Empty) && m_CurrentArea == ProfilerArea.CPU;
            ProfilerChart cpuChart = m_Charts[(int)ProfilerArea.CPU];
            if (hasCPUOverlay)
            {
                cpuChart.m_Data.hasOverlay = true;
                int numCharts = cpuChart.m_Data.numSeries;
                for (int i = 0; i < numCharts; ++i)
                {
                    var chart = cpuChart.m_Data.series[i];
                    cpuChart.m_Data.overlays[i] = new ChartSeriesViewData(chart.name, chart.yValues.Length, chart.color);
                    for (int frameIdx = 0; frameIdx < chart.yValues.Length; ++frameIdx)
                        cpuChart.m_Data.overlays[i].xValues[frameIdx] = (float)frameIdx;
                    int identifier = ProfilerDriver.GetStatisticsIdentifierForArea(cpuChart.m_Area, UnityString.Format("Selected{0}", chart.name));
                    float maxValue;
                    ProfilerDriver.GetStatisticsValues(identifier, firstEmptyFrame, 1.0f, cpuChart.m_Data.overlays[i].yValues, out maxValue);
                    cpuChart.m_Data.overlays[i].yScale = cpuChart.m_DataScale;
                }
            }
            else
            {
                cpuChart.m_Data.hasOverlay = false;
            }

            // CPU, GPU & UI chart scale value
            for (int i = 0; i < ms_StackedAreas.Length; i++)
                ComputeChartScaleValue(ms_StackedAreas[i], historyLength, firstEmptyFrame, firstFrame);

            // Is GPU Profiling supported warning
            string warning = null;
            if (!ProfilerDriver.isGPUProfilerSupported)
            {
                warning = "GPU profiling is not supported by the graphics card driver. Please update to a newer version if available.";

                if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    if (!ProfilerDriver.isGPUProfilerSupportedByOS)
                        warning = "GPU profiling requires Mac OS X 10.7 (Lion) and a capable video card. GPU profiling is currently not supported on mobile.";
                    else
                        warning = "GPU profiling is not supported by the graphics card driver (or it was disabled because of driver bugs).";
                }
            }
            m_Charts[(int)ProfilerArea.GPU].m_NotSupportedWarning = warning;
        }

        private void ComputeChartScaleValue(ProfilerArea i, int historyLength, int firstEmptyFrame, int firstFrame)
        {
            ProfilerChart chart = m_Charts[(int)i];
            float timeMax = 0.0f;
            float timeMaxExcludeFirst = 0.0f;
            for (int k = 0; k < historyLength; k++)
            {
                float timeNow = 0.0F;
                for (int j = 0; j < chart.m_Series.Length; j++)
                {
                    var series = chart.m_Data.unstackableSeriesIndex == j && chart.m_Data.hasOverlay ?
                        chart.m_Data.overlays[j] : chart.m_Series[j];

                    if (series.enabled)
                        timeNow += series.yValues[k];
                }
                if (timeNow > timeMax)
                    timeMax = timeNow;
                if (timeNow > timeMaxExcludeFirst && k + firstEmptyFrame >= firstFrame + 1)
                    timeMaxExcludeFirst = timeNow;
            }
            if (timeMaxExcludeFirst != 0.0f)
                timeMax = timeMaxExcludeFirst;

            timeMax = Math.Min(timeMax * chart.m_DataScale, m_ChartMaxClamp);

            // Do not apply the new scale immediately, but gradually go towards it
            if (m_ChartOldMax[(int)i] > 0.0f)
                timeMax = Mathf.Lerp(m_ChartOldMax[(int)i], timeMax, 0.4f);
            m_ChartOldMax[(int)i] = timeMax;

            for (int k = 0; k < chart.m_Data.numSeries; ++k)
                chart.m_Data.series[k].rangeAxis = new Vector2(0f, timeMax);
            UpdateChartGrid(timeMax, chart.m_Data);
        }

        internal static void UpdateSingleChart(ProfilerChart chart, int firstEmptyFrame, int firstFrame)
        {
            float totalMaxValue = 1;
            int unstackableChartIndex = -1;
            var maxValues = new float[chart.m_Series.Length];
            for (int i = 0, count = chart.m_Series.Length; i < count; ++i)
            {
                int identifier = ProfilerDriver.GetStatisticsIdentifierForArea(chart.m_Area, chart.m_Series[i].name);
                float maxValue;
                ProfilerDriver.GetStatisticsValues(identifier, firstEmptyFrame, 1.0f, chart.m_Series[i].yValues, out maxValue);
                chart.m_Series[i].yScale = chart.m_DataScale;
                maxValue *= chart.m_DataScale;

                // Minimum size so we don't generate nans during drawing
                maxValue = Mathf.Max(maxValue, 0.0001F);

                if (maxValue > totalMaxValue)
                    totalMaxValue = maxValue;

                if (chart.m_Type == Chart.ChartType.Line)
                {
                    // Scale line charts so they never hit the top. Scale them slightly differently for each line
                    // so that in "no stuff changing" case they will not end up being exactly the same.
                    maxValues[i] = maxValue * (1.05f + i * 0.05f);
                    chart.m_Series[i].rangeAxis = new Vector2(0f, maxValues[i]);
                }
                else
                {
                    maxValues[i] = maxValue;
                    if (chart.m_Area == ProfilerArea.CPU)
                    {
                        if (chart.m_Series[i].name == k_CPUUnstackableSeriesName)
                        {
                            unstackableChartIndex = i;
                            break;
                        }
                    }
                }
            }
            if (chart.m_Area == ProfilerArea.NetworkMessages || chart.m_Area == ProfilerArea.NetworkOperations)
            {
                for (int i = 0, count = chart.m_Series.Length; i < count; ++i)
                    chart.m_Series[i].rangeAxis = new Vector2(0f, 0.9f * totalMaxValue);
                chart.m_Data.maxValue = totalMaxValue;
            }
            chart.m_Data.Assign(chart.m_Series, unstackableChartIndex, firstEmptyFrame, firstFrame);
            ProfilerDriver.GetStatisticsAvailable(chart.m_Area, firstEmptyFrame, chart.m_Data.dataAvailable);

            if (chart is UISystemProfilerChart)
                ((UISystemProfilerChart)chart).Update(firstFrame, ProfilerUserSettings.frameCount);
        }

        void AddAreaClick(object userData, string[] options, int selected)
        {
            if (m_Charts[selected].active)
                m_Charts[selected].Close();
            else
                m_Charts[selected].active = true;
        }

        void SetRecordMode(ProfilerMemoryRecordMode memRecordMode)
        {
            if (memRecordMode == m_SelectedMemRecordMode)
                return;
            m_SelectedMemRecordMode = memRecordMode;
            if (m_SelectedMemRecordMode != ProfilerMemoryRecordMode.None)
                m_LastSelectedMemRecordMode = m_SelectedMemRecordMode;
            ProfilerDriver.memoryRecordMode = memRecordMode;
            memoryRecordingModeChanged?.Invoke(memRecordMode);
        }

        void MemRecordModeClick(object userData, string[] options, int selected)
        {
            SetRecordMode((ProfilerMemoryRecordMode)selected);
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

        public void SetRecordingEnabled(bool profilerEnabled)
        {
            ProfilerDriver.enabled = profilerEnabled;
            m_Recording = profilerEnabled;
            SessionState.SetBool(kProfilerEnabledSessionKey, profilerEnabled);
            if (ProfilerUserSettings.rememberLastRecordState)
                EditorPrefs.SetBool(kProfilerEnabledSessionKey, profilerEnabled);
            recordingStateChanged?.Invoke(m_Recording);
            Repaint();
        }

        private void DrawMainToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Graph types
            Rect popupRect = GUILayoutUtility.GetRect(Styles.addArea, EditorStyles.toolbarDropDown, GUILayout.Width(Chart.kSideWidth - 1));
            if (EditorGUI.DropdownButton(popupRect, Styles.addArea, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                int length = m_Charts.Length;
                var names = new string[length];
                var enabled = new bool[length];
                var selected = new int[length];
                for (int c = 0; c < length; ++c)
                {
                    names[c] = L10n.Tr(((ProfilerArea)c).ToString());
                    enabled[c] = true;
                    selected[c] = m_Charts[c].active ? c : -1;
                }
                EditorUtility.DisplayCustomMenu(popupRect, names, enabled, selected, AddAreaClick, null);
            }

            // Engine attach
            ConnectionGUILayout.AttachToPlayerDropdown(m_AttachProfilerState, EditorStyles.toolbarDropDown);

            // Record
            var profilerEnabled = GUILayout.Toggle(m_Recording, m_Recording ? Styles.profilerRecordOn : Styles.profilerRecordOff, EditorStyles.toolbarButton);
            if (profilerEnabled != m_Recording)
                SetRecordingEnabled(profilerEnabled);

            FrameNavigationControls();

            using (new EditorGUI.DisabledScope(ProfilerDriver.lastFrameIndex == -1))
            {
                // Clear
                if (GUILayout.Button(Styles.clearData, EditorStyles.toolbarButton))
                {
                    Clear();
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
            using (new EditorGUI.DisabledScope(ProfilerDriver.lastFrameIndex == -1))
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
            var overflowMenuRect = GUILayoutUtility.GetRect(Styles.optionsButtonContent, EditorStyles.toolbarButton);
            if (GUI.Button(overflowMenuRect, Styles.optionsButtonContent, EditorStyles.toolbarButton))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(Styles.accessibilityModeLabel, UserAccessiblitySettings.colorBlindCondition != ColorBlindCondition.Default, OnToggleColorBlindMode);
                menu.AddSeparator("");
                menu.AddItem(Styles.preferencesButtonContent, false, OpenProfilerPreferences);
                menu.DropDown(overflowMenuRect);
            }

            GUILayout.EndHorizontal();
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
            var selectedMemRecordMode = m_SelectedMemRecordMode;
            if (Unsupported.IsDeveloperMode())
            {
                bool toggled = m_SelectedMemRecordMode != ProfilerMemoryRecordMode.None;
                var oldToggleState = toggled;
                if (EditorGUILayout.DropDownToggle(ref toggled, Styles.recordCallstacks, EditorStyles.toolbarDropDownToggle))
                {
                    Rect rect = GUILayoutUtility.topLevel.GetLast();
                    var names = new string[]
                    {
                        L10n.Tr("None"), L10n.Tr("Managed Allocations"), L10n.Tr("All Allocations (fast)"), L10n.Tr("All Allocations (full)")
                    };

                    var enabled = new bool[names.Length];
                    for (int c = 0; c < names.Length; ++c)
                        enabled[c] = true;
                    var selected = new int[] { (int)m_SelectedMemRecordMode };
                    EditorUtility.DisplayCustomMenu(rect, names, enabled, selected, MemRecordModeClick, null);
                    GUIUtility.ExitGUI();
                }
                if (toggled != oldToggleState)
                {
                    selectedMemRecordMode = (m_SelectedMemRecordMode != ProfilerMemoryRecordMode.None) ? ProfilerMemoryRecordMode.None :
                        (m_LastSelectedMemRecordMode == ProfilerMemoryRecordMode.None ? ProfilerMemoryRecordMode.ManagedAllocations : m_LastSelectedMemRecordMode);
                }
            }
            else
            {
                selectedMemRecordMode = GUILayout.Toggle(m_SelectedMemRecordMode == ProfilerMemoryRecordMode.ManagedAllocations, Styles.recordCallstacks, EditorStyles.toolbarButton) ? ProfilerMemoryRecordMode.ManagedAllocations : ProfilerMemoryRecordMode.None;
            }

            if (selectedMemRecordMode != m_SelectedMemRecordMode)
                SetRecordMode(selectedMemRecordMode);
        }

        void Clear()
        {
            m_ProfilerModules[(int)ProfilerArea.CPU].Clear();
            m_ProfilerModules[(int)ProfilerArea.GPU].Clear();

            ProfilerDriver.ClearAllFrames();
            m_LastFrameFromTick = -1;

#pragma warning disable CS0618
            NetworkDetailStats.m_NetworkOperations.Clear();
#pragma warning restore
        }

        private void FrameNavigationControls()
        {
            if (m_CurrentFrame > ProfilerDriver.lastFrameIndex)
            {
                SetCurrentFrameDontPause(ProfilerDriver.lastFrameIndex);
            }

            // Previous/next/current buttons
            using (new EditorGUI.DisabledScope(ProfilerDriver.GetPreviousFrameIndex(m_CurrentFrame) == -1))
            {
                if (GUILayout.Button(Styles.prevFrame, EditorStyles.toolbarButton))
                {
                    if (m_CurrentFrame == -1)
                        PrevFrame();
                    PrevFrame();
                }
            }

            using (new EditorGUI.DisabledScope(ProfilerDriver.GetNextFrameIndex(m_CurrentFrame) == -1))
            {
                if (GUILayout.Button(Styles.nextFrame, EditorStyles.toolbarButton))
                    NextFrame();
            }


            if (GUILayout.Toggle(m_CurrentFrame == -1, Styles.currentFrame, EditorStyles.toolbarButton))
            {
                SetCurrentFrame(-1);
                m_LastFrameFromTick = ProfilerDriver.lastFrameIndex;
            }
            else if (m_CurrentFrame == -1)
            {
                PrevFrame();
            }

            // Frame number
            GUILayout.Label(Styles.frame.text + PickFrameLabel(), EditorStyles.toolbarLabel);
        }

        void SetCurrentFrameDontPause(int frame)
        {
            m_CurrentFrame = frame;
        }

        void SetCurrentFrame(int frame)
        {
            bool shouldPause = frame != -1 && ProfilerDriver.enabled && !ProfilerDriver.profileEditor && m_CurrentFrame != frame;
            if (shouldPause && EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.isPaused = true;

            currentFrameChanged?.Invoke(frame, shouldPause);

            if (ProfilerInstrumentationPopup.InstrumentationEnabled)
                ProfilerInstrumentationPopup.UpdateInstrumentableFunctions();

            SetCurrentFrameDontPause(frame);
        }

        void OnGUI()
        {
            CheckForPlatformModuleChange();
            InitializeIfNeeded();

            if (m_CurrentArea == k_InvalidArea && Event.current.type == EventType.Repaint)
            {
                for (int i = 0; i < m_Charts.Length; i++)
                {
                    if (m_Charts[i].active)
                    {
                        m_Charts[i].ChartSelected();
                        break;
                    }
                }
            }
            // Initialization
            DrawMainToolbar();

            SplitterGUILayout.BeginVerticalSplit(m_VertSplit);

            m_GraphPos = EditorGUILayout.BeginScrollView(m_GraphPos, Styles.profilerGraphBackground);

            if (m_PrevLastFrame != ProfilerDriver.lastFrameIndex)
            {
                UpdateCharts();
                m_PrevLastFrame = ProfilerDriver.lastFrameIndex;
            }

            int newCurrentFrame = m_CurrentFrame;
            for (int c = 0; c < m_Charts.Length; ++c)
            {
                var chart = m_Charts[c];
                if (!chart.active)
                    continue;

                newCurrentFrame = chart.DoChartGUI(newCurrentFrame, m_CurrentArea == chart.m_Area);
            }

            if (newCurrentFrame != m_CurrentFrame)
            {
                SetCurrentFrame(newCurrentFrame);
                Repaint();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndScrollView();

            GUILayout.BeginVertical();
            if (m_CurrentArea != k_InvalidArea)
            {
                var detailViewPosition = new Rect(0, m_VertSplit.realSizes[0] + EditorGUI.kWindowToolbarHeight, position.width, m_VertSplit.realSizes[1]);
                var detailViewToolbar = detailViewPosition;
                detailViewToolbar.height = EditorStyles.contentToolbar.CalcHeight(GUIContent.none, 10.0f);
                m_ProfilerModules[(int)m_CurrentArea].DrawToolbar(detailViewPosition);

                detailViewPosition.yMin += detailViewToolbar.height;
                m_ProfilerModules[(int)m_CurrentArea].DrawView(detailViewPosition);

                // Draw separator
                var lineRect = new Rect(0, m_VertSplit.realSizes[0] + EditorGUI.kWindowToolbarHeight - 1, position.width, 1);
                EditorGUI.DrawRect(lineRect, Styles.borderColor);
            }
            else
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.contentToolbar);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            SplitterGUILayout.EndVerticalSplit();
        }

        public void SetClearOnPlay(bool enabled)
        {
            m_ClearOnPlay = enabled;
        }

        public bool GetClearOnPlay()
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
    }
}
