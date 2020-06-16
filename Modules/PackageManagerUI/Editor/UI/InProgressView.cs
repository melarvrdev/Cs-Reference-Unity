// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class InProgressView : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<InProgressView> {}

        private Label m_Title;
        private LoadingSpinner m_Spinner;
        private Label m_Description;

        public InProgressView()
        {
            m_Title = new Label() { classList = { "title" } };
            m_Spinner = new LoadingSpinner();
            var spinnerContainer = new VisualElement() { classList = { "spinnerContainer" } };
            spinnerContainer.Add(m_Spinner);
            m_Description = new Label() { classList = { "description" } };

            Add(m_Title);
            Add(spinnerContainer);
            Add(m_Description);
        }

        public bool Refresh(IPackage package, IPackageVersion version)
        {
            if (package?.progress == PackageProgress.Installing && version?.HasTag(PackageTag.Git) == true)
            {
                m_Title.text = L10n.Tr("Please wait, installing a GIT package...");
                m_Description.text = package.uniqueId;
                m_Spinner.Start();
                UIUtils.SetElementDisplay(this, true);
                return true;
            }
            m_Title.text = string.Empty;
            m_Description.text = string.Empty;
            m_Spinner.Stop();
            UIUtils.SetElementDisplay(this, false);
            return false;
        }
    }
}
