// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class MultiSelectDetails : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<MultiSelectDetails> {}

        private ResourceLoader m_ResourceLoader;
        private ApplicationProxy m_Application;
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private AssetStoreClient m_AssetStoreClient;
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private AssetStoreCache m_AssetStoreCache;
        private AssetStoreCallQueue m_AssetStoreCallQueue;
        private PackageFiltering m_PackageFiltering;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_Application = container.Resolve<ApplicationProxy>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
            m_PageManager = container.Resolve<PageManager>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_AssetStoreClient = container.Resolve<AssetStoreClient>();
            m_AssetStoreDownloadManager = container.Resolve<AssetStoreDownloadManager>();
            m_AssetStoreCache = container.Resolve<AssetStoreCache>();
            m_AssetStoreCallQueue = container.Resolve<AssetStoreCallQueue>();
            m_PackageFiltering = container.Resolve<PackageFiltering>();
        }

        private UnlockFoldout m_UnlockFoldout;
        private NoActionsFoldout m_NoActionFoldout;
        private CheckUpdateFoldout m_CheckUpdateFoldout;

        private MultiSelectFoldout[] m_StandaloneFoldouts;

        private InstallFoldoutGroup m_InstallFoldoutGroup;
        private UpdateFoldoutGroup m_UpdateFoldoutGroup;
        private RemoveFoldoutGroup m_RemoveFoldoutGroup;

        private DownloadFoldoutGroup m_DownloadFoldoutGroup;
        private DownloadUpdateFoldoutGroup m_DownloadUpdateFoldoutGroup;

        private MultiSelectFoldoutGroup[] m_UpmFoldoutGroups;
        private MultiSelectFoldoutGroup[] m_AssetStoreFoldoutGroups;

        private IEnumerable<MultiSelectFoldoutGroup> allFoldoutGroups => m_UpmFoldoutGroups.Concat(m_AssetStoreFoldoutGroups);
        private IEnumerable<IMultiSelectFoldoutElement> allFoldoutElements => m_StandaloneFoldouts.Cast<IMultiSelectFoldoutElement>().Concat(allFoldoutGroups);

        public MultiSelectDetails()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("MultiSelectDetails.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            lockedPackagesInfoBox.Q<Button>().clickable.clicked += OnDeselectLockedSelectionsClicked;

            InitializeFoldouts();
        }

        public void OnEnable()
        {
            m_AssetStoreClient.onUpdateChecked += OnUpdateChecked;
        }

        public void OnDisable()
        {
            m_AssetStoreClient.onUpdateChecked -= OnUpdateChecked;
        }

        private void InitializeFoldouts()
        {
            var disableIfCompiling = new ButtonDisableCondition(() => m_Application.isCompiling,
                L10n.Tr("You need to wait until the compilation is finished to perform this action."));
            var disableIfInstallOrUninstallInProgress = new ButtonDisableCondition(() => m_PackageDatabase.isInstallOrUninstallInProgress,
                L10n.Tr("You need to wait until other install or uninstall operations are finished to perform this action."));
            var disableIfNoNetwork = new ButtonDisableCondition(() => !m_Application.isInternetReachable,
                L10n.Tr("You need to restore your network connection to perform this action."));

            // Standalone foldouts
            m_UnlockFoldout = new UnlockFoldout(m_PageManager);
            m_UnlockFoldout.button.onAction += Refresh;

            m_NoActionFoldout = new NoActionsFoldout(m_PageManager);

            m_CheckUpdateFoldout = new CheckUpdateFoldout(m_PageManager, m_AssetStoreCache, m_AssetStoreCallQueue);

            m_StandaloneFoldouts = new MultiSelectFoldout[] { m_UnlockFoldout, m_NoActionFoldout, m_CheckUpdateFoldout };

            // Foldout groups
            m_InstallFoldoutGroup = new InstallFoldoutGroup(m_Application, m_PackageDatabase);
            m_InstallFoldoutGroup.mainButton.SetGlobalDisableConditions(disableIfCompiling, disableIfInstallOrUninstallInProgress);

            m_RemoveFoldoutGroup = new RemoveFoldoutGroup(m_Application, m_PackageManagerPrefs, m_PackageDatabase, m_PageManager);
            m_RemoveFoldoutGroup.mainButton.SetGlobalDisableConditions(disableIfCompiling, disableIfInstallOrUninstallInProgress);

            m_UpdateFoldoutGroup = new UpdateFoldoutGroup(m_Application, m_PackageDatabase, m_PageManager);
            m_UpdateFoldoutGroup.mainButton.SetGlobalDisableConditions(disableIfCompiling, disableIfInstallOrUninstallInProgress);

            m_DownloadFoldoutGroup = new DownloadFoldoutGroup(m_AssetStoreDownloadManager, m_AssetStoreCache, m_PackageDatabase);
            m_DownloadFoldoutGroup.mainButton.SetGlobalDisableConditions(disableIfCompiling, disableIfNoNetwork);
            m_DownloadFoldoutGroup.cancelButton.SetGlobalDisableConditions(disableIfCompiling);

            m_DownloadUpdateFoldoutGroup = new DownloadUpdateFoldoutGroup(m_AssetStoreDownloadManager, m_AssetStoreCache, m_PackageDatabase);
            m_DownloadUpdateFoldoutGroup.mainButton.SetGlobalDisableConditions(disableIfCompiling, disableIfNoNetwork);
            m_DownloadUpdateFoldoutGroup.cancelButton.SetGlobalDisableConditions(disableIfCompiling);

            m_UpmFoldoutGroups = new MultiSelectFoldoutGroup[] { m_InstallFoldoutGroup, m_RemoveFoldoutGroup, m_UpdateFoldoutGroup};
            m_AssetStoreFoldoutGroups = new MultiSelectFoldoutGroup[] { m_DownloadFoldoutGroup, m_DownloadUpdateFoldoutGroup };

            // Add foldouts to the UI in the correct order. Note that the order here is not the same as the initialization order from above.
            foldoutsContainer.Add(m_UnlockFoldout);

            foldoutsContainer.Add(m_InstallFoldoutGroup.mainFoldout);
            foldoutsContainer.Add(m_InstallFoldoutGroup.inProgressFoldout);
            foldoutsContainer.Add(m_RemoveFoldoutGroup.mainFoldout);
            foldoutsContainer.Add(m_RemoveFoldoutGroup.inProgressFoldout);
            foldoutsContainer.Add(m_UpdateFoldoutGroup.mainFoldout);
            foldoutsContainer.Add(m_UpdateFoldoutGroup.inProgressFoldout);

            foldoutsContainer.Add(m_DownloadFoldoutGroup.mainFoldout);
            foldoutsContainer.Add(m_DownloadFoldoutGroup.inProgressFoldout);
            foldoutsContainer.Add(m_DownloadUpdateFoldoutGroup.mainFoldout);
            foldoutsContainer.Add(m_DownloadUpdateFoldoutGroup.inProgressFoldout);

            foldoutsContainer.Add(m_CheckUpdateFoldout);
            foldoutsContainer.Add(m_NoActionFoldout);
        }

        private void OnUpdateChecked(IEnumerable<string> productIds)
        {
            var selection = m_PageManager.GetSelection();
            if (productIds.Any(id => selection.Contains(id)))
                Refresh(selection);
        }

        public bool Refresh(PageSelection selections)
        {
            if (selections.Count <= 1)
                return false;

            title.text = string.Format(L10n.Tr("{0} {1} selected"), selections.Count, selections.Count > 1 ? L10n.Tr("items") : L10n.Tr("item"));

            // We get the versions from the visual states instead of directly from the selection to keep the ordering of packages
            var versions = m_PageManager.GetCurrentPage().visualStates.Select(visualState =>
            {
                if (selections.TryGetValue(visualState.packageUniqueId, out var pair))
                {
                    m_PackageDatabase.GetPackageAndVersion(pair, out var package, out var version);
                    return version ?? package?.versions.primary;
                }
                return null;
            }).Where(version => version != null);

            foreach (var foldoutElement in allFoldoutElements)
                foldoutElement.ClearVersions();

            // We want to populate only a subset of all foldouts based on the current tab.
            var foldoutGroups = m_PackageFiltering.currentFilterTab == PackageFilterTab.AssetStore ? m_AssetStoreFoldoutGroups : m_UpmFoldoutGroups;

            foreach (var version in versions)
            {
                if (m_UnlockFoldout.AddPackageVersion(version))
                    continue;

                if (m_CheckUpdateFoldout.AddPackageVersion(version))
                    continue;

                var isActionable = false;
                foreach (var foldoutGroup in foldoutGroups)
                    isActionable |= foldoutGroup.AddPackageVersion(version);

                if (!isActionable)
                    m_NoActionFoldout.AddPackageVersion(version);
            }

            foreach (var foldoutElement in allFoldoutElements)
                foldoutElement.Refresh();

            UIUtils.SetElementDisplay(infoBoxContainer, m_UnlockFoldout.versions.Any());
            return true;
        }

        private void Refresh()
        {
            Refresh(m_PageManager.GetSelection());
        }

        private void OnDeselectLockedSelectionsClicked()
        {
            m_PageManager.RemoveSelection(m_UnlockFoldout.versions.Select(s => new PackageAndVersionIdPair(s.packageUniqueId, s.uniqueId)));
            PackageManagerWindowAnalytics.SendEvent("deselectLocked", packageIds: m_UnlockFoldout.versions.Select(v => v.packageUniqueId));
        }

        private VisualElementCache cache { get; set; }
        private Label title => cache.Get<Label>("multiSelectTitle");
        private VisualElement infoBoxContainer => cache.Get<VisualElement>("multiSelectInfoBoxContainer");
        private HelpBox lockedPackagesInfoBox => cache.Get<HelpBox>("lockedPackagesInfoBox");
        private VisualElement foldoutsContainer => cache.Get<VisualElement>("multiSelectFoldoutsContainer");
    }
}
