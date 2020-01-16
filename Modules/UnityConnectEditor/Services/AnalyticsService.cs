// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Analytics;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Connect
{
    [InitializeOnLoad]
    internal class AnalyticsService : SingleService
    {
        public override string name { get; }
        public override string title { get; }
        public override string description { get; }
        public override string pathTowardIcon { get; }
        public override string projectSettingsPath { get; }
        public override string settingsProviderClassName => nameof(AnalyticsProjectSettings);
        public override bool displayToggle { get; }
        public override Notification.Topic notificationTopic => Notification.Topic.AnalyticsService;
        public override string packageId { get; }

        static readonly AnalyticsService k_Instance;

        public static AnalyticsService instance => k_Instance;

        private struct AnalyticsServiceState { public bool analytics; }

        static AnalyticsService()
        {
            k_Instance = new AnalyticsService();
        }

        AnalyticsService()
        {
            string serviceName = L10n.Tr("Analytics");
            name = serviceName;
            title = serviceName;
            description = L10n.Tr("Discover player insights");
            pathTowardIcon = @"Builtin Skins\Shared\Images\ServicesWindow-ServiceIcon-Analytics.png";
            projectSettingsPath = "Project/Services/Analytics";
            displayToggle = true;
            packageId = "com.unity.analytics";
            ServicesRepository.AddService(this);
        }

        public override bool IsServiceEnabled()
        {
            return AnalyticsSettings.enabled;
        }

        protected override void InternalEnableService(bool enable)
        {
            if (AnalyticsSettings.enabled != enable)
            {
                AnalyticsSettings.SetEnabledServiceWindow(enable);
                EditorAnalytics.SendEventServiceInfo(new AnalyticsServiceState() { analytics = enable });
                if (!enable && PurchasingService.instance.IsServiceEnabled())
                {
                    PurchasingService.instance.EnableService(false);
                }
            }

            base.InternalEnableService(enable);
        }

        public void RequestValidationData(Action<AsyncOperation> onGet, string authSignature, out UnityWebRequest request)
        {
            request = UnityWebRequest.Get(String.Format(AnalyticsConfiguration.instance.validatorUrl, UnityConnect.instance.projectInfo.projectGUID));
            var encodedAuthToken = ServicesUtils.Base64Encode((UnityConnect.instance.projectInfo.projectGUID + ":" + authSignature));
            request.SetRequestHeader("Authorization", $"Basic {encodedAuthToken}");
            var operation = request.SendWebRequest();
            operation.completed += onGet;
        }

        //TODO: Consider moving to 'core' of services:
        public void RequestAuthSignature(Action<AsyncOperation> onGet, out UnityWebRequest request)
        {
            request = UnityWebRequest.Get(String.Format(AnalyticsConfiguration.instance.coreProjectsUrl, UnityConnect.instance.projectInfo.projectGUID));
            request.SetRequestHeader("AUTHORIZATION", $"Bearer {UnityConnect.instance.GetUserInfo().accessToken}");
            var operation = request.SendWebRequest();
            operation.completed += onGet;
        }
    }
}
