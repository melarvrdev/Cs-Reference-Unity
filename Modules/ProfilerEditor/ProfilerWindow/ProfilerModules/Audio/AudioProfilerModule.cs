// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    [ProfilerModuleMetadata("Audio", typeof(LocalizationResource), IconPath = "Profiler.Audio")]
    internal class AudioProfilerModule : ProfilerModuleBase
    {
        const int k_DefaultOrderIndex = 4;

        Vector2 m_PaneScroll_AudioChannels = Vector2.zero;
        Vector2 m_PaneScroll_AudioDSPLeft = Vector2.zero;
        Vector2 m_PaneScroll_AudioDSPRight_ScrollPos = Vector2.zero;
        Vector2 m_PaneScroll_AudioDSPRight_Size = new Vector2(10000, 20000);
        Vector2 m_PaneScroll_AudioClips = Vector2.zero;

        [SerializeField]
        bool m_ShowInactiveDSPChains = false;

        [SerializeField]
        bool m_HighlightAudibleDSPChains = true;

        [SerializeField]
        float m_DSPGraphZoomFactor = 1.0f;

        [SerializeField]
        bool m_DSPGraphHorizontalLayout = false;

        [SerializeField]
        private AudioProfilerGroupTreeViewState m_AudioProfilerGroupTreeViewState;
        private AudioProfilerGroupView m_AudioProfilerGroupView = null;
        private AudioProfilerGroupViewBackend m_AudioProfilerGroupViewBackend;

        [SerializeField]
        private AudioProfilerClipTreeViewState m_AudioProfilerClipTreeViewState;
        private AudioProfilerClipView m_AudioProfilerClipView = null;
        private AudioProfilerClipViewBackend m_AudioProfilerClipViewBackend;

        private AudioProfilerDSPView m_AudioProfilerDSPView;
        enum ProfilerAudioPopupItems
        {
            Simple = 0,
            Detailed = 1
        }
        ProfilerAudioView m_ShowDetailedAudioPane;

        int m_LastAudioProfilerFrame = -1;

        const string k_ViewTypeSettingsKey = "Profiler.AudioProfilerModule.ViewType";
        const string k_ShowInactiveDSPChainsSettingsKey = "Profiler.MemoryProfilerModule.ShowInactiveDSPChains";
        const string k_HighlightAudibleDSPChainsSettingsKey = "Profiler.MemoryProfilerModule.HighlightAudibleDSPChains";
        const string k_DSPGraphZoomFactorSettingsKey = "Profiler.MemoryProfilerModule.DSPGraphZoomFactor";
        const string k_DSPGraphHorizontalLayoutSettingsKey = "Profiler.MemoryProfilerModule.DSPGraphHorizontalLayout";
        const string k_AudioProfilerGroupTreeViewStateSettingsKey = "Profiler.MemoryProfilerModule.AudioProfilerGroupTreeViewState";
        const string k_AudioProfilerClipTreeViewStateSettingsKey = "Profiler.MemoryProfilerModule.AudioProfilerClipTreeViewState";

        internal override ProfilerArea area => ProfilerArea.Audio;
        public override bool usesCounters => false;

        private protected override int defaultOrderIndex => k_DefaultOrderIndex;
        private protected override string legacyPreferenceKey => "ProfilerChartAudio";

        internal override void OnEnable()
        {
            base.OnEnable();

            m_ShowDetailedAudioPane = (ProfilerAudioView)EditorPrefs.GetInt(k_ViewTypeSettingsKey, (int)ProfilerAudioView.Channels);
            m_ShowInactiveDSPChains = EditorPrefs.GetBool(k_ShowInactiveDSPChainsSettingsKey, m_ShowInactiveDSPChains);
            m_HighlightAudibleDSPChains = EditorPrefs.GetBool(k_HighlightAudibleDSPChainsSettingsKey, m_HighlightAudibleDSPChains);
            m_DSPGraphZoomFactor = SessionState.GetFloat(k_DSPGraphZoomFactorSettingsKey, m_DSPGraphZoomFactor);
            m_DSPGraphHorizontalLayout = EditorPrefs.GetBool(k_DSPGraphHorizontalLayoutSettingsKey, m_DSPGraphHorizontalLayout);
            var restoredAudioProfilerGroupTreeViewState = SessionState.GetString(k_AudioProfilerGroupTreeViewStateSettingsKey, string.Empty);
            if (!string.IsNullOrEmpty(restoredAudioProfilerGroupTreeViewState))
            {
                try
                {
                    m_AudioProfilerGroupTreeViewState = JsonUtility.FromJson<AudioProfilerGroupTreeViewState>(restoredAudioProfilerGroupTreeViewState);
                }
                catch{} // Never mind, we'll fall back to the default
            }
            var restoredAudioProfilerClipTreeViewState = SessionState.GetString(k_AudioProfilerClipTreeViewStateSettingsKey, string.Empty);
            if (!string.IsNullOrEmpty(restoredAudioProfilerClipTreeViewState))
            {
                try
                {
                    m_AudioProfilerClipTreeViewState = JsonUtility.FromJson<AudioProfilerClipTreeViewState>(restoredAudioProfilerClipTreeViewState);
                }
                catch{} // Never mind, we'll fall back to the default
            }
        }

        internal override void SaveViewSettings()
        {
            base.SaveViewSettings();
            EditorPrefs.SetInt(k_ViewTypeSettingsKey, (int)m_ShowDetailedAudioPane);
            EditorPrefs.SetBool(k_ShowInactiveDSPChainsSettingsKey, m_ShowInactiveDSPChains);
            EditorPrefs.SetBool(k_HighlightAudibleDSPChainsSettingsKey, m_HighlightAudibleDSPChains);
            SessionState.SetFloat(k_DSPGraphZoomFactorSettingsKey, m_DSPGraphZoomFactor);
            EditorPrefs.SetBool(k_DSPGraphHorizontalLayoutSettingsKey, m_DSPGraphHorizontalLayout);
            if (m_AudioProfilerGroupTreeViewState != null)
                SessionState.SetString(k_AudioProfilerGroupTreeViewStateSettingsKey, EditorJsonUtility.ToJson(m_AudioProfilerGroupTreeViewState));
            if (m_AudioProfilerGroupTreeViewState != null)
                SessionState.SetString(k_AudioProfilerClipTreeViewStateSettingsKey, EditorJsonUtility.ToJson(m_AudioProfilerClipTreeViewState));
        }

        public override void DrawToolbar(Rect position)
        {
            // This module still needs to be broken apart into Toolbar and View.
        }

        public override void DrawDetailsView(Rect position)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            ProfilerAudioView newShowDetailedAudioPane = m_ShowDetailedAudioPane;
            if (AudioDeepProfileToggle())
            {
                if (GUILayout.Toggle(newShowDetailedAudioPane == ProfilerAudioView.Channels, "Channels", EditorStyles.toolbarButton)) newShowDetailedAudioPane = ProfilerAudioView.Channels;
                if (GUILayout.Toggle(newShowDetailedAudioPane == ProfilerAudioView.Groups, "Groups", EditorStyles.toolbarButton)) newShowDetailedAudioPane = ProfilerAudioView.Groups;
                if (GUILayout.Toggle(newShowDetailedAudioPane == ProfilerAudioView.ChannelsAndGroups, "Channels and groups", EditorStyles.toolbarButton)) newShowDetailedAudioPane = ProfilerAudioView.ChannelsAndGroups;
                if (Unsupported.IsDeveloperMode() && GUILayout.Toggle(newShowDetailedAudioPane == ProfilerAudioView.DSPGraph, "DSP Graph", EditorStyles.toolbarButton)) newShowDetailedAudioPane = ProfilerAudioView.DSPGraph;
                if (Unsupported.IsDeveloperMode() && GUILayout.Toggle(newShowDetailedAudioPane == ProfilerAudioView.Clips, "Clips", EditorStyles.toolbarButton)) newShowDetailedAudioPane = ProfilerAudioView.Clips;
                if (newShowDetailedAudioPane != m_ShowDetailedAudioPane)
                {
                    m_ShowDetailedAudioPane = newShowDetailedAudioPane;
                    m_LastAudioProfilerFrame = -1; // force update
                }
                if (m_ShowDetailedAudioPane == ProfilerAudioView.DSPGraph)
                {
                    m_ShowInactiveDSPChains = GUILayout.Toggle(m_ShowInactiveDSPChains, "Show inactive", EditorStyles.toolbarButton);
                    if (m_ShowInactiveDSPChains)
                        m_HighlightAudibleDSPChains = GUILayout.Toggle(m_HighlightAudibleDSPChains, "Highlight audible", EditorStyles.toolbarButton);
                    m_DSPGraphHorizontalLayout = GUILayout.Toggle(m_DSPGraphHorizontalLayout, "Horizontal Layout", EditorStyles.toolbarButton);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    var graphRect = DrawAudioStatsPane(ref m_PaneScroll_AudioDSPLeft);

                    m_PaneScroll_AudioDSPRight_ScrollPos = GUI.BeginScrollView(graphRect, m_PaneScroll_AudioDSPRight_ScrollPos, new Rect(0, 0, m_PaneScroll_AudioDSPRight_Size.x, m_PaneScroll_AudioDSPRight_Size.y));

                    var clippingRect = new Rect(m_PaneScroll_AudioDSPRight_ScrollPos.x, m_PaneScroll_AudioDSPRight_ScrollPos.y, graphRect.width, graphRect.height);

                    if (m_AudioProfilerDSPView == null)
                        m_AudioProfilerDSPView = new AudioProfilerDSPView();

                    ProfilerProperty property = ProfilerWindow.CreateProperty();
                    if (property != null &&
                        property.frameDataReady)
                    {
                        using (property)
                        {
                            m_AudioProfilerDSPView.OnGUI(clippingRect, property, m_ShowInactiveDSPChains, m_HighlightAudibleDSPChains, m_DSPGraphHorizontalLayout, ref m_DSPGraphZoomFactor, ref m_PaneScroll_AudioDSPRight_ScrollPos, ref m_PaneScroll_AudioDSPRight_Size);
                        }
                    }

                    GUI.EndScrollView();
                }
                else if (m_ShowDetailedAudioPane == ProfilerAudioView.Clips)
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    var treeRect = DrawAudioStatsPane(ref m_PaneScroll_AudioClips);

                    // TREE
                    if (m_AudioProfilerClipTreeViewState == null)
                        m_AudioProfilerClipTreeViewState = new AudioProfilerClipTreeViewState();

                    if (m_AudioProfilerClipViewBackend == null)
                        m_AudioProfilerClipViewBackend = new AudioProfilerClipViewBackend(m_AudioProfilerClipTreeViewState);

                    ProfilerProperty property = ProfilerWindow.CreateProperty();
                    if (property == null)
                        return;
                    if (!property.frameDataReady)
                        return;

                    using (property)
                    {
                        var currentFrame = ProfilerWindow.GetActiveVisibleFrameIndex();
                        if (currentFrame == -1 || m_LastAudioProfilerFrame != currentFrame)
                        {
                            m_LastAudioProfilerFrame = currentFrame;
                            var sourceItems = property.GetAudioProfilerClipInfo();
                            if (sourceItems != null && sourceItems.Length > 0)
                            {
                                var items = new List<AudioProfilerClipInfoWrapper>();
                                foreach (var s in sourceItems)
                                {
                                    items.Add(new AudioProfilerClipInfoWrapper(s, property.GetAudioProfilerNameByOffset(s.assetNameOffset)));
                                }
                                m_AudioProfilerClipViewBackend.SetData(items);
                                if (m_AudioProfilerClipView == null)
                                {
                                    m_AudioProfilerClipView = new AudioProfilerClipView(ProfilerWindow as EditorWindow, m_AudioProfilerClipTreeViewState);
                                    m_AudioProfilerClipView.Init(treeRect, m_AudioProfilerClipViewBackend);
                                }
                            }
                        }
                        if (m_AudioProfilerClipView != null)
                            m_AudioProfilerClipView.OnGUI(treeRect);
                    }
                }
                else
                {
                    bool resetAllAudioClipPlayCountsOnPlay = GUILayout.Toggle(AudioUtil.resetAllAudioClipPlayCountsOnPlay, "Reset play count on play", EditorStyles.toolbarButton);
                    if (resetAllAudioClipPlayCountsOnPlay != AudioUtil.resetAllAudioClipPlayCountsOnPlay)
                        AudioUtil.resetAllAudioClipPlayCountsOnPlay = resetAllAudioClipPlayCountsOnPlay;
                    if (Unsupported.IsDeveloperMode())
                    {
                        GUILayout.Space(5);
                        bool showAllGroups = EditorPrefs.GetBool("AudioProfilerShowAllGroups");
                        bool newShowAllGroups = GUILayout.Toggle(showAllGroups, "Show all groups (dev mode only)", EditorStyles.toolbarButton);
                        if (showAllGroups != newShowAllGroups)
                            EditorPrefs.SetBool("AudioProfilerShowAllGroups", newShowAllGroups);
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    var treeRect = DrawAudioStatsPane(ref m_PaneScroll_AudioChannels);

                    // TREE
                    if (m_AudioProfilerGroupTreeViewState == null)
                        m_AudioProfilerGroupTreeViewState = new AudioProfilerGroupTreeViewState();

                    if (m_AudioProfilerGroupViewBackend == null)
                        m_AudioProfilerGroupViewBackend = new AudioProfilerGroupViewBackend(m_AudioProfilerGroupTreeViewState);

                    ProfilerProperty property = ProfilerWindow.CreateProperty();
                    if (property == null)
                        return;
                    if (!property.frameDataReady)
                        return;

                    using (property)
                    {
                        var currentFrame = ProfilerWindow.GetActiveVisibleFrameIndex();
                        if (currentFrame == -1 || m_LastAudioProfilerFrame != currentFrame)
                        {
                            m_LastAudioProfilerFrame = currentFrame;
                            var sourceItems = property.GetAudioProfilerGroupInfo();
                            if (sourceItems != null && sourceItems.Length > 0)
                            {
                                var items = new List<AudioProfilerGroupInfoWrapper>();
                                var parentMapping = new Dictionary<int, AudioProfilerGroupInfo>();
                                foreach (var s in sourceItems)
                                    parentMapping.Add(s.uniqueId, s);
                                foreach (var s in sourceItems)
                                {
                                    bool isGroup = (s.flags & AudioProfilerGroupInfoHelper.AUDIOPROFILER_FLAGS_GROUP) != 0;
                                    if (m_ShowDetailedAudioPane == ProfilerAudioView.Channels && isGroup)
                                        continue;
                                    if (m_ShowDetailedAudioPane == ProfilerAudioView.Groups && !isGroup)
                                        continue;
                                    var wrapper = new AudioProfilerGroupInfoWrapper(s, property.GetAudioProfilerNameByOffset(s.assetNameOffset), property.GetAudioProfilerNameByOffset(s.objectNameOffset), m_ShowDetailedAudioPane == ProfilerAudioView.Channels);
                                    if (parentMapping.TryGetValue(s.parentId, out var parent))
                                        wrapper.parentName = property.GetAudioProfilerNameByOffset(parent.objectNameOffset);
                                    else
                                        wrapper.parentName = "ROOT";
                                    items.Add(wrapper);
                                }
                                m_AudioProfilerGroupViewBackend.SetData(items);
                                if (m_AudioProfilerGroupView == null)
                                {
                                    m_AudioProfilerGroupView = new AudioProfilerGroupView(ProfilerWindow as EditorWindow, m_AudioProfilerGroupTreeViewState);
                                    m_AudioProfilerGroupView.Init(treeRect, m_AudioProfilerGroupViewBackend);
                                }
                            }
                            else
                            {
                                var items = new List<AudioProfilerGroupInfoWrapper>();
                                m_AudioProfilerGroupViewBackend.SetData(items);
                                m_AudioProfilerGroupView = null;
                            }
                        }
                        if (m_AudioProfilerGroupView != null)
                            m_AudioProfilerGroupView.OnGUI(treeRect, m_ShowDetailedAudioPane);
                    }
                }
            }
            else
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                DrawDetailsViewText(position);
            }

            ProfilerWindow.Repaint();
        }

        bool AudioDeepProfileToggle()
        {
            int toggleFlags = (int)ProfilerCaptureFlags.Channels;
            if (Unsupported.IsDeveloperMode())
                toggleFlags |= (int)ProfilerCaptureFlags.Clips | (int)ProfilerCaptureFlags.DSPNodes;
            ProfilerAudioPopupItems oldShowDetailedAudioPane = (AudioSettings.profilerCaptureFlags & toggleFlags) != 0 ? ProfilerAudioPopupItems.Detailed : ProfilerAudioPopupItems.Simple;
            ProfilerAudioPopupItems newShowDetailedAudioPane = (ProfilerAudioPopupItems)EditorGUILayout.EnumPopup(oldShowDetailedAudioPane, EditorStyles.toolbarDropDownLeft, GUILayout.Width(70f));
            if (oldShowDetailedAudioPane != newShowDetailedAudioPane)
                ProfilerDriver.SetAudioCaptureFlags((AudioSettings.profilerCaptureFlags & ~toggleFlags) | (newShowDetailedAudioPane == ProfilerAudioPopupItems.Detailed ? toggleFlags : 0));
            return (AudioSettings.profilerCaptureFlags & toggleFlags) != 0;
        }

        Rect DrawAudioStatsPane(ref Vector2 scrollPos)
        {
            var totalRect = GUILayoutUtility.GetRect(20f, 20000f, 10, 10000f);
            var statsRect = new Rect(totalRect.x, totalRect.y, 230f, totalRect.height);
            var rightRect = new Rect(statsRect.xMax, totalRect.y, totalRect.width - statsRect.width, totalRect.height);

            // STATS
            var content = ProfilerDriver.GetOverviewText(area, ProfilerWindow.GetActiveVisibleFrameIndex());
            var textSize = EditorStyles.wordWrappedLabel.CalcSize(GUIContent.Temp(content));
            scrollPos = GUI.BeginScrollView(statsRect, scrollPos, new Rect(0, 0, textSize.x, textSize.y));
            GUI.Label(new Rect(3, 3, textSize.x, textSize.y), content, EditorStyles.wordWrappedLabel);
            GUI.EndScrollView();
            EditorGUI.DrawRect(new Rect(statsRect.xMax - 1, statsRect.y, 1, statsRect.height), Color.black);

            return rightRect;
        }
    }
}
