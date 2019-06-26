// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class UpmAddOperation : UpmBaseOperation<AddRequest>
    {
        private string m_SpecialUniqueId = string.Empty;

        public override string specialUniqueId { get { return m_SpecialUniqueId; } }

        public void Add(string packageId)
        {
            m_PackageId = packageId;
            m_PackageName = string.Empty;
            m_SpecialUniqueId = string.Empty;
            Start();
        }

        public void AddByUrlOrPath(string urlOrPath)
        {
            m_SpecialUniqueId = urlOrPath;
            m_PackageId = string.Empty;
            m_PackageName = string.Empty;
            Start();
        }

        protected override AddRequest CreateRequest()
        {
            var uniqueId = string.IsNullOrEmpty(specialUniqueId) ? packageId : specialUniqueId;
            return Client.Add(uniqueId);
        }
    }
}
