// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditorInternal
{
    internal class ProfilerChart : Chart
    {
        public const int k_MaximumSeriesCount = 10;
        public const float k_ChartMinClamp = 110.0f;
        public const float k_ChartMaxClamp = 70000.0f;

        const string k_ProfilerChartSettingsPreferenceKeyFormat = "ProfilerChart.{0}.ChartSettings";

        private static readonly GUIContent performanceWarning =
            new GUIContent("", EditorGUIUtility.LoadIcon("console.warnicon.sml"), L10n.Tr("Collecting GPU Profiler data might have overhead. Close graph if you don't need its data"));

        public ProfilerArea m_Area;
        public ChartType m_Type;
        public float m_DataScale;
        public ChartViewData m_Data;
        public ChartSeriesViewData[] m_Series;
        // For some charts, every line is scaled individually, so every data series gets their own range based on their own max scale.
        // For charts that share their scale (like the Networking charts) all series get adjusted to the total max of the chart.
        // Shared scale is only used for line charts and should only be used when every series shares the same data unit.
        public bool m_SharedScale;

        string m_Name;
        string m_Tooltip;
        string m_IconName;
        float m_MaximumScaleInterpolationValue;

        public ProfilerChart(ProfilerArea area, ChartType type, float dataScale, float maximumScaleInterpolationValue, int seriesCount, string name, string iconName) : base()
        {
            Debug.Assert(seriesCount <= k_MaximumSeriesCount);

            labelRange = new Vector2(Mathf.Epsilon, Mathf.Infinity);
            graphRange = new Vector2(Mathf.Epsilon, Mathf.Infinity);
            m_Area = area;
            m_Type = type;
            m_DataScale = dataScale;
            m_MaximumScaleInterpolationValue = maximumScaleInterpolationValue;
            m_Data = new ChartViewData();
            m_Series = new ChartSeriesViewData[seriesCount];
            m_Name = name;
            m_IconName = iconName;

            var localizedTooltipFormat = LocalizationDatabase.GetLocalizedString("A chart showing performance counters related to '{0}'.");
            m_Tooltip = string.Format(localizedTooltipFormat, name);
        }

        string ChartSettingsPreferenceKey => string.Format(k_ProfilerChartSettingsPreferenceKeyFormat, m_Name);
        bool usesCounters => ((uint)m_Area == Profiler.invalidProfilerArea);

        /// <summary>
        /// Callback parameter will be either true if a state change occured
        /// </summary>
        /// <param name="onSeriesToggle"></param>
        public void SetOnSeriesToggleCallback(Action<bool> onSeriesToggle)
        {
            onDoSeriesToggle = onSeriesToggle;
        }

        public void SetName(string name, string legacyPreferenceKey)
        {
            m_Name = name;

            var chartSettingsPreferenceKey = string.IsNullOrEmpty(legacyPreferenceKey) ? ChartSettingsPreferenceKey : legacyPreferenceKey;
            SetChartSettingsNameAndUpdateAllPreferences(chartSettingsPreferenceKey);
        }

        protected override void DoLegendGUI(Rect position, ChartType type, ChartViewData cdata, EventType evtType, bool active)
        {
            base.DoLegendGUI(position, type, cdata, evtType, active);

            // TODO Move to a GPU chart subclass would be better.
            if (m_Area == ProfilerArea.GPU)
            {
                const float rightMmargin = 2f;
                const float topMargin = 4f;
                const float iconSize = 16f;
                var padding = GUISkin.current.label.padding;
                float width = iconSize + padding.horizontal;

                GUI.Label(new Rect(position.xMax - width - rightMmargin, position.y + topMargin, width, iconSize + padding.vertical), performanceWarning);
            }
        }

        public virtual int DoChartGUI(int currentFrame, bool active)
        {
            if (Event.current.type == EventType.Repaint)
            {
                string[] labels = new string[m_Series.Length];
                for (int s = 0; s < m_Series.Length; s++)
                {
                    string name =
                        m_Data.hasOverlay ?
                        "Selected" + m_Series[s].name :
                        m_Series[s].name;
                    if (usesCounters)
                    {
                        labels[s] = ProfilerDriver.GetFormattedCounterValue(currentFrame, m_Series[s].category, name);
                    }
                    else
                    {
                        labels[s] = ProfilerDriver.GetFormattedCounterValue(currentFrame, m_Area, name);
                    }
                }
                m_Data.AssignSelectedLabels(labels);
            }

            if (legendHeaderLabel == null)
            {
                legendHeaderLabel = new GUIContent(m_Name, EditorGUIUtility.LoadIconRequired(m_IconName), m_Tooltip);
            }

            return DoGUI(m_Type, currentFrame, m_Data, active);
        }

        public void LoadAndBindSettings(string legacyPreferenceKey)
        {
            // Use the legacy preference key to maintain user settings on built-in modules.
            var chartSettingsPreferenceKey = string.IsNullOrEmpty(legacyPreferenceKey) ? ChartSettingsPreferenceKey : legacyPreferenceKey;
            LoadAndBindSettings(chartSettingsPreferenceKey, m_Data);
        }

        public virtual void UpdateData(int firstEmptyFrame, int firstFrame, int frameCount)
        {
            float totalMaxValue = 1;
            for (int i = 0, count = m_Series.Length; i < count; ++i)
            {
                float maxValue;
                if (usesCounters)
                {
                    ProfilerDriver.GetCounterValuesBatch(m_Series[i].category, m_Series[i].name, firstEmptyFrame, 1.0f, m_Series[i].yValues, out maxValue);
                }
                else
                {
                    ProfilerDriver.GetCounterValuesBatch(m_Area, m_Series[i].name, firstEmptyFrame, 1.0f, m_Series[i].yValues, out maxValue);
                }

                m_Series[i].yScale = m_DataScale;
                maxValue *= m_DataScale;

                // Minimum size so we don't generate nans during drawing
                maxValue = Mathf.Max(maxValue, 0.0001F);

                if (maxValue > totalMaxValue)
                    totalMaxValue = maxValue;

                if (m_Type == ChartType.Line)
                {
                    // Scale line charts so they never hit the top. Scale them slightly differently for each line
                    // so that in "no stuff changing" case they will not end up being exactly the same.
                    maxValue *= (1.05f + i * 0.05f);
                    m_Series[i].rangeAxis = new Vector2(0f, maxValue);
                }
                if (usesCounters)
                {
                    ProfilerDriver.GetStatisticsAvailabilityStatesByCategory(m_Series[i].category, firstEmptyFrame, m_Data.dataAvailable);
                }
            }

            if (m_SharedScale && m_Type == ChartType.Line)
            {
                // For some charts, every line is scaled individually, so every data series gets their own range based on their own max scale.
                // For charts that share their scale (like the Networking charts) all series get adjusted to the total max of the chart.
                for (int i = 0, count = m_Series.Length; i < count; ++i)
                    m_Series[i].rangeAxis = new Vector2(0f, (1.05f + i * 0.05f) * totalMaxValue);
                m_Data.maxValue = totalMaxValue;
            }
            m_Data.Assign(m_Series, firstEmptyFrame, firstFrame);

            if (!usesCounters)
            {
                ProfilerDriver.GetStatisticsAvailabilityStates(m_Area, firstEmptyFrame, m_Data.dataAvailable);
            }
        }

        public void UpdateOverlayData(int firstEmptyFrame)
        {
            m_Data.hasOverlay = true;
            int numCharts = m_Data.numSeries;
            for (int i = 0; i < numCharts; ++i)
            {
                var chart = m_Data.series[i];
                m_Data.overlays[i] = new ChartSeriesViewData(chart.name, chart.yValues.Length, chart.color);
                for (int frameIdx = 0; frameIdx < chart.yValues.Length; ++frameIdx)
                    m_Data.overlays[i].xValues[frameIdx] = (float)frameIdx;
                float maxValue;
                ProfilerDriver.GetCounterValuesBatch(ProfilerArea.CPU, UnityString.Format("Selected{0}", chart.name), firstEmptyFrame, 1.0f, m_Data.overlays[i].yValues, out maxValue);
                m_Data.overlays[i].yScale = m_DataScale;
            }
        }

        public void UpdateScaleValuesIfNecessary(int firstEmptyFrame, int firstFrame, int frameCount)
        {
            if (m_Type == ChartType.StackedFill)
            {
                ComputeChartScaleValue(firstEmptyFrame, firstFrame, frameCount);
            }
        }

        public void ComputeChartScaleValue(int firstEmptyFrame, int firstFrame, int frameCount)
        {
            float timeMax = 0.0f;
            float timeMaxExcludeFirst = 0.0f;
            for (int k = 0; k < frameCount; k++)
            {
                float timeNow = 0.0F;
                for (int j = 0; j < m_Series.Length; j++)
                {
                    var series = m_Series[j];

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

            timeMax = Mathf.Clamp(timeMax * m_DataScale, k_ChartMinClamp, k_ChartMaxClamp);

            // Do not apply the new scale immediately, but gradually go towards it
            if (m_MaximumScaleInterpolationValue > 0.0f)
                timeMax = Mathf.Lerp(m_MaximumScaleInterpolationValue, timeMax, 0.4f);
            m_MaximumScaleInterpolationValue = timeMax;

            for (int k = 0; k < m_Data.numSeries; ++k)
                m_Data.series[k].rangeAxis = new Vector2(0f, timeMax);
            m_Data.UpdateChartGrid(timeMax);
        }
    }
}
