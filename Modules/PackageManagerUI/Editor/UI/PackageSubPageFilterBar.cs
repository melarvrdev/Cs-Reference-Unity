// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageSubPageFilterBar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageSubPageFilterBar> {}

        private PackageFiltering m_PackageFiltering;
        private PageManager m_PageManager;

        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_PackageFiltering = container.Resolve<PackageFiltering>();
            m_PageManager = container.Resolve<PageManager>();
        }

        public PackageSubPageFilterBar()
        {
            ResolveDependencies();
            OnFilterTabChanged(m_PackageFiltering.currentFilterTab);
        }

        public void OnEnable()
        {
            m_PackageFiltering.onFilterTabChanged += OnFilterTabChanged;
            m_PageManager.onSubPageChanged += OnSubPageChanged;
        }

        public void OnDisable()
        {
            m_PackageFiltering.onFilterTabChanged -= OnFilterTabChanged;
            m_PageManager.onSubPageChanged -= OnSubPageChanged;
        }

        private void OnFilterTabChanged(PackageFilterTab filterTab)
        {
            var page = m_PageManager.GetPage(filterTab);
            Refresh(page);
        }
        private void OnSubPageChanged(IPage page)
        {
            if (page.tab != m_PackageFiltering.currentFilterTab)
                return;
            Refresh(page);
        }

        private void Refresh(IPage page = null)
        {
            if (page == null)
                page = m_PageManager.GetCurrentPage();
            var showOnFilterTab = page.subPages.Skip(1).Any();
            UIUtils.SetElementDisplay(this, showOnFilterTab);

            if (!showOnFilterTab)
                return;

            Clear();
            var currentSubPage = page.currentSubPage;
            foreach (var subPage in page.subPages)
            {
                var button = new Button();
                button.text = subPage.displayName;
                button.clickable.clicked += () =>
                {
                    if (page.currentSubPage == subPage)
                        return;
                    page.currentSubPage = subPage;
                    PackageManagerWindowAnalytics.SendEvent("changeSubPage");
                };
                Add(button);
                if (subPage == currentSubPage)
                    button.AddToClassList("active");
            }
        }
    }
}
