// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Connect
{
    [InitializeOnLoad]
    internal class BuildService : SingleService
    {
        public override string name { get; }
        public override string title { get; }
        public override string description { get; }
        public override string pathTowardIcon { get; }
        public override string projectSettingsPath { get; } = "Project/Services/Cloud Build";
        public override string settingsProviderClassName => nameof(CloudBuildProjectSettings);
        public override bool displayToggle { get; }
        public override Notification.Topic notificationTopic => Notification.Topic.BuildService;
        public override string packageName { get; }

        public override string editorGamePackageName { get; } = "com.unity.services.cloud-build";
        public override bool canShowFallbackProjectSettings { get; } = true;
        public override bool canShowBuiltInProjectSettings { get; } = false;
        public override string minimumEditorGamePackageVersion { get; } = "1.0.0";

        public override string serviceFlagName { get; }
        public override bool shouldSyncOnProjectRebind => true;

        static readonly BuildService k_Instance;

        public static BuildService instance => k_Instance;

        static BuildService()
        {
            k_Instance = new BuildService();
        }

        struct BuildServiceState
        {
            public bool build;
        }

        BuildService()
        {
            name = "Build";
            title = L10n.Tr("Cloud Build");
            description = L10n.Tr("Build games faster");
            pathTowardIcon = @"Builtin Skins\Shared\Images\ServicesWindow-ServiceIcon-Build.png";
            displayToggle = true;
            packageName = null;
            serviceFlagName = "build";
            ServicesRepository.AddService(this);
        }

        protected override void InternalEnableService(bool enable, bool shouldUpdateApiFlag)
        {
            if (IsServiceEnabled() != enable)
            {
                EditorAnalytics.SendEventServiceInfo(new BuildServiceState() { build = enable });
            }
            base.InternalEnableService(enable, shouldUpdateApiFlag);
        }
    }
}
