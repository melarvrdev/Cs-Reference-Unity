// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageOperationDispatcher
    {
        [NonSerialized]
        private AssetDatabaseProxy m_AssetDatabase;
        [NonSerialized]
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        [NonSerialized]
        private UpmClient m_UpmClient;
        [NonSerialized]
        private IOProxy m_IOProxy;

        public void ResolveDependencies(AssetDatabaseProxy assetDatabase,
            AssetStoreDownloadManager assetStoreDownloadManager,
            UpmClient upmClient,
            IOProxy ioProxy)
        {
            m_AssetDatabase = assetDatabase;
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_UpmClient = upmClient;
            m_IOProxy = ioProxy;
        }

        public virtual bool isInstallOrUninstallInProgress => m_UpmClient.isAddOrRemoveInProgress;

        public virtual bool IsUninstallInProgress(IPackage package)
        {
            return m_UpmClient.IsRemoveInProgress(package?.name);
        }

        public virtual bool IsInstallInProgress(IPackageVersion version)
        {
            return m_UpmClient.IsAddInProgress(version?.packageId);
        }

        public virtual void Install(IPackageVersion version)
        {
            if (version == null || version.isInstalled)
                return;
            m_UpmClient.AddById(version.packageId);
        }

        public virtual void Install(IEnumerable<IPackageVersion> versions)
        {
            if (versions == null || !versions.Any())
                return;

            m_UpmClient.AddByIds(versions.Select(v => v.packageId));
        }

        public virtual void Install(string packageId)
        {
            m_UpmClient.AddById(packageId);
        }

        public virtual void InstallFromUrl(string url)
        {
            m_UpmClient.AddByUrl(url);
        }

        public virtual bool InstallFromPath(string path, out string tempPackageId)
        {
            return m_UpmClient.AddByPath(path, out tempPackageId);
        }

        public virtual void Uninstall(IPackage package)
        {
            if (package?.versions.installed == null)
                return;
            m_UpmClient.RemoveByName(package.name);
        }

        public virtual void Uninstall(IEnumerable<IPackage> packages)
        {
            if (packages == null || !packages.Any())
                return;
            m_UpmClient.RemoveByNames(packages.Select(p => p.name));
        }

        public virtual void InstallAndResetDependencies(IPackageVersion version, IEnumerable<IPackage> dependenciesToReset)
        {
            m_UpmClient.AddAndResetDependencies(version.packageId, dependenciesToReset?.Select(package => package.name) ?? Enumerable.Empty<string>());
        }

        public virtual void ResetDependencies(IPackageVersion version, IEnumerable<IPackage> dependenciesToReset)
        {
            m_UpmClient.ResetDependencies(version.packageId, dependenciesToReset?.Select(package => package.name) ?? Enumerable.Empty<string>());
        }

        public virtual void RemoveEmbedded(IPackage package)
        {
            if (package?.versions.installed == null)
                return;
            m_UpmClient.RemoveEmbeddedByName(package.name);
        }

        public virtual void FetchExtraInfo(IPackageVersion version)
        {
            if (version == null || version.isFullyFetched)
                return;
            m_UpmClient.ExtraFetch(version.packageId);
        }

        public virtual bool Download(IPackage package)
        {
            return Download(new[] { package });
        }

        public virtual bool Download(IEnumerable<IPackage> packages)
        {
            return PlayModeDownload.CanBeginDownload() && m_AssetStoreDownloadManager.Download(packages.Select(p => p.uniqueId));
        }

        public virtual void AbortDownload(IPackage package)
        {
            AbortDownload(new[] { package });
        }

        public virtual void AbortDownload(IEnumerable<IPackage> packages)
        {
            // We need to figure out why the IEnumerable is being altered instead of using ToArray.
            // It will be addressed in https://jira.unity3d.com/browse/PAX-1995.
            foreach (var package in packages.ToArray())
                m_AssetStoreDownloadManager.AbortDownload(package.uniqueId);
        }

        public virtual void PauseDownload(IPackage package)
        {
            if (package?.Is(PackageType.AssetStore) != true)
                return;
            m_AssetStoreDownloadManager.PauseDownload(package.uniqueId);
        }

        public virtual void ResumeDownload(IPackage package)
        {
            if (package?.Is(PackageType.AssetStore) != true || !PlayModeDownload.CanBeginDownload())
                return;
            m_AssetStoreDownloadManager.ResumeDownload(package.uniqueId);
        }

        public virtual void Import(IPackage package)
        {
            if (package?.Is(PackageType.AssetStore) != true)
                return;

            var path = package.versions.primary.localPath;
            try
            {
                if (m_IOProxy.FileExists(path))
                    m_AssetDatabase.ImportPackage(path, true);
            }
            catch (System.IO.IOException e)
            {
                Debug.Log($"[Package Manager Window] Cannot import package {package.displayName}: {e.Message}");
            }
        }
    }
}
