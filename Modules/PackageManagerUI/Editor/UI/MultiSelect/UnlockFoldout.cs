// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class UnlockFoldout : MultiSelectFoldout
    {
        public UnlockFoldout(PageManager pageManager) : base(new PackageUnlockButton(pageManager))
        {
            headerTextTemplate = L10n.Tr("Unlock {0}");
        }

        public override bool AddPackageVersion(IPackageVersion version)
        {
            if (!m_Button.GetActionState(version, out _, out _).HasFlag(PackageActionState.Visible))
                return false;
            return base.AddPackageVersion(version);
        }
    }
}
