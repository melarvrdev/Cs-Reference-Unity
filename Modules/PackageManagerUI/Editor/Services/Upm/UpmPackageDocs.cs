// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class UpmPackageDocs
    {
        // Module package.json files contain a documentation url embedded in the description.
        // We parse that to have the "View Documentation" button direct to it, instead of showing
        // the link in the description text.
        internal const string k_BuiltinPackageDocsUrlKey = "Scripting API: ";

        private static Version ParseShortVersion(string shortVersionId)
        {
            try
            {
                var versionToken = shortVersionId.Split('@')[1];
                return new Version(versionToken);
            }
            catch (Exception)
            {
                // Keep default version 0.0 on exception
                return new Version();
            }
        }

        // Method content must be matched in package manager doc tools
        private static string GetPackageUrlRedirect(string packageName, string shortVersionId)
        {
            var redirectUrl = "";
            if (packageName == "com.unity.ads")
                redirectUrl = "https://docs.unity3d.com/Manual/UnityAds.html";
            else if (packageName == "com.unity.analytics")
            {
                if (ParseShortVersion(shortVersionId) < new Version(3, 2))
                    redirectUrl = "https://docs.unity3d.com/Manual/UnityAnalytics.html";
            }
            else if (packageName == "com.unity.purchasing")
                redirectUrl = "https://docs.unity3d.com/Manual/UnityIAP.html";
            else if (packageName == "com.unity.standardevents")
                redirectUrl = "https://docs.unity3d.com/Manual/UnityAnalyticsStandardEvents.html";
            else if (packageName == "com.unity.xiaomi")
                redirectUrl = "https://unity3d.com/cn/partners/xiaomi/guide";
            else if (packageName == "com.unity.shadergraph")
            {
                if (ParseShortVersion(shortVersionId) < new Version(4, 1))
                    redirectUrl = "https://github.com/Unity-Technologies/ShaderGraph/wiki";
            }
            return redirectUrl;
        }

        public static string GetPackageUrlRedirect(IPackageVersion version)
        {
            var upmVersion = version as UpmPackageVersion;
            return upmVersion == null ? string.Empty : GetPackageUrlRedirect(upmVersion.name, upmVersion.shortVersionId);
        }

        public static string[] SplitBuiltinDescription(UpmPackageVersion version)
        {
            if (string.IsNullOrEmpty(version?.packageInfo?.description))
                return new string[] { string.Format(L10n.Tr("This built in package controls the presence of the {0} module."), version.displayName) };
            else
                return version.packageInfo.description.Split(new[] { k_BuiltinPackageDocsUrlKey }, StringSplitOptions.None);
        }

        public static string GetOfflineDocumentation(IOProxy IOProxy, IPackageVersion version)
        {
            if (version?.isAvailableOnDisk == true && version.packageInfo != null)
            {
                try
                {
                    var docsFolder = IOProxy.PathsCombine(version.packageInfo.resolvedPath, "Documentation~");
                    if (!IOProxy.DirectoryExists(docsFolder))
                        docsFolder = IOProxy.PathsCombine(version.packageInfo.resolvedPath, "Documentation");
                    if (IOProxy.DirectoryExists(docsFolder))
                    {
                        var mdFiles = IOProxy.DirectoryGetFiles(docsFolder, "*.md", System.IO.SearchOption.TopDirectoryOnly);
                        var docsMd = mdFiles.FirstOrDefault(d => IOProxy.GetFileName(d).ToLower() == "index.md")
                            ?? mdFiles.FirstOrDefault(d => IOProxy.GetFileName(d).ToLower() == "tableofcontents.md") ?? mdFiles.FirstOrDefault();
                        if (!string.IsNullOrEmpty(docsMd))
                            return docsMd;
                    }
                }
                catch (System.IO.IOException) {}
            }
            return string.Empty;
        }

        public static string GetDocumentationUrl(IPackageVersion version)
        {
            var upmVersion = version as UpmPackageVersion;
            if (upmVersion == null)
                return string.Empty;

            if (!string.IsNullOrEmpty(upmVersion.documentationUrl))
                return upmVersion.documentationUrl;

            if (upmVersion.HasTag(PackageTag.BuiltIn) && !string.IsNullOrEmpty(upmVersion.description))
            {
                var split = SplitBuiltinDescription(upmVersion);
                if (split.Length > 1)
                    return split[1];
            }
            return $"http://docs.unity3d.com/Packages/{upmVersion.shortVersionId}/index.html";
        }

        public static string GetChangelogUrl(IPackageVersion version)
        {
            var upmVersion = version as UpmPackageVersion;
            if (upmVersion == null)
                return string.Empty;

            if (!string.IsNullOrEmpty(upmVersion.changelogUrl))
                return upmVersion.changelogUrl;

            return $"http://docs.unity3d.com/Packages/{upmVersion.shortVersionId}/changelog/CHANGELOG.html";
        }

        public static string GetOfflineChangelog(IOProxy IOProxy, IPackageVersion version)
        {
            if (version?.isAvailableOnDisk == true && version.packageInfo != null)
            {
                try
                {
                    var changelogFile = IOProxy.PathsCombine(version.packageInfo.resolvedPath, "CHANGELOG.md");
                    return IOProxy.FileExists(changelogFile) ? changelogFile : string.Empty;
                }
                catch (System.IO.IOException) {}
            }
            return string.Empty;
        }

        public static string GetLicensesUrl(IPackageVersion version)
        {
            var upmVersion = version as UpmPackageVersion;
            if (upmVersion == null)
                return string.Empty;

            if (!string.IsNullOrEmpty(upmVersion.licensesUrl))
                return upmVersion.licensesUrl;

            string url;
            if (!string.IsNullOrEmpty(GetPackageUrlRedirect(upmVersion)))
                url = "https://unity3d.com/legal/licenses/Unity_Companion_License";
            else
                url = $"http://docs.unity3d.com/Packages/{upmVersion.shortVersionId}/license/index.html";
            return url;
        }

        public static string GetOfflineLicenses(IOProxy IOProxy, IPackageVersion version)
        {
            if (version?.isAvailableOnDisk == true && version.packageInfo != null)
            {
                try
                {
                    var licenseFile = IOProxy.PathsCombine(version.packageInfo.resolvedPath, "LICENSE.md");
                    return IOProxy.FileExists(licenseFile) ? licenseFile : string.Empty;
                }
                catch (System.IO.IOException) {}
            }
            return string.Empty;
        }

        public static bool HasDocs(IPackageVersion version)
        {
            var upmVersion = version as UpmPackageVersion;
            if (!string.IsNullOrEmpty(upmVersion?.documentationUrl))
                return true;
            return upmVersion != null && !version.HasTag(PackageTag.Feature);
        }

        public static bool HasChangelog(IPackageVersion version)
        {
            var upmVersion = version as UpmPackageVersion;
            if (!string.IsNullOrEmpty(upmVersion?.changelogUrl))
                return true;
            return upmVersion != null && !version.HasTag(PackageTag.BuiltIn | PackageTag.Feature) && string.IsNullOrEmpty(GetPackageUrlRedirect(version));
        }

        public static bool HasLicenses(IPackageVersion version)
        {
            var upmVersion = version as UpmPackageVersion;
            if (!string.IsNullOrEmpty(upmVersion?.licensesUrl))
                return true;
            return upmVersion != null && !version.HasTag(PackageTag.BuiltIn | PackageTag.Feature);
        }
    }
}
