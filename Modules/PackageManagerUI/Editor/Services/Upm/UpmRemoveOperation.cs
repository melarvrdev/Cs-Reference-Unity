// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UpmRemoveOperation : UpmBaseOperation<RemoveRequest>
    {
        public override RefreshOptions refreshOptions => RefreshOptions.None;

        protected override string operationErrorMessage => string.Format(L10n.Tr("Error removing package: {0}."), packageName);

        public void Remove(string packageIdOrName)
        {
            m_PackageIdOrName = packageIdOrName;
            Start();
        }

        protected override RemoveRequest CreateRequest()
        {
            return m_ClientProxy.Remove(packageName);
        }
    }
}
