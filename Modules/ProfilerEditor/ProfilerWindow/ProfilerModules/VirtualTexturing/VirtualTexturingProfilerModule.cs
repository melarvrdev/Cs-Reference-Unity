// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEditor.Profiling;
using UnityEditor;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class VirtualTexturingProfilerModule : ProfilerModuleBase
    {
        [SerializeField]
        VirtualTexturingProfilerView m_VTProfilerView;

        const string k_IconName = "Profiler.VirtualTexturing";
        const int k_DefaultOrderIndex = 13;
        static readonly string k_UnLocalizedName = "Virtual Texturing";
        static readonly string k_Name = LocalizationDatabase.GetLocalizedString(k_UnLocalizedName);
        static readonly string k_VTCountersCategoryName = ProfilerCategory.VirtualTexturing.Name;

        static readonly string[] k_VirtualTexturingCounterNames =
        {
            "Required Tiles",
            "Max Cache Mip Bias",
            "Max Cache Demand",
            "Missing Streaming Tiles",
            "Missing Disk Data"
        };

        public VirtualTexturingProfilerModule(IProfilerWindowController profilerWindow) : base(profilerWindow, k_UnLocalizedName, k_Name, k_IconName, Chart.ChartType.Line) {}

        public override ProfilerArea area => ProfilerArea.VirtualTexturing;
        protected override int defaultOrderIndex => k_DefaultOrderIndex;

        public override void OnEnable()
        {
            base.OnEnable();
            if (m_VTProfilerView == null)
            {
                m_VTProfilerView = new VirtualTexturingProfilerView();
            }
        }

        public override void DrawToolbar(Rect position)
        {
            DrawEmptyToolbar();
        }

        public override void DrawDetailsView(Rect position)
        {
            m_VTProfilerView?.DrawUIPane(m_ProfilerWindow);
        }

        protected override List<ProfilerCounterData> CollectDefaultChartCounters()
        {
            var chartCounters = new List<ProfilerCounterData>(k_VirtualTexturingCounterNames.Length);
            foreach (var counterName in k_VirtualTexturingCounterNames)
            {
                chartCounters.Add(new ProfilerCounterData()
                {
                    m_Name = counterName,
                    m_Category = k_VTCountersCategoryName,
                });
            }

            return chartCounters;
        }

        protected override List<ProfilerCounterData> CollectDefaultDetailCounters()
        {
            var detailCounter = new List<ProfilerCounterData>(1);
            detailCounter.Add(new ProfilerCounterData() { m_Name = "Max Cache Mip Bias", m_Category = k_VTCountersCategoryName });
            return detailCounter;
        }
    }
}
