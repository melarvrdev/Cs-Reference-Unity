// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.UIElements;
using UnityEditor.StyleSheets;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Internal;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Experimental
{
    [ExcludeFromDocs]
    public partial class EditorResources
    {
        private static bool s_DelayedInitialization;
        private static bool s_IgnoreFirstPostProcessImport;
        private static bool s_RefreshGlobalCatalog;

        // Global editor styles
        private static readonly StyleCatalog s_StyleCatalog;

        static EditorResources()
        {
            s_IgnoreFirstPostProcessImport = true;
            s_StyleCatalog = new StyleCatalog();
            s_StyleCatalog.AddPath(UIElementsEditorUtility.s_DefaultCommonStyleSheetPath);
            s_StyleCatalog.AddPath(EditorGUIUtility.isProSkin
                ? UIElementsEditorUtility.s_DefaultCommonDarkStyleSheetPath
                : UIElementsEditorUtility.s_DefaultCommonLightStyleSheetPath);
            foreach (var editorUssPath in AssetDatabase.GetAllAssetPaths().Where(IsEditorStyleSheet))
                s_StyleCatalog.AddPath(editorUssPath);
            s_StyleCatalog.Refresh();

            EditorApplication.update -= DelayInitialization;
            EditorApplication.update += DelayInitialization;
        }

        private static bool IsEditorStyleSheet(string path)
        {
            var pathLowerCased = path.ToLower();
            return pathLowerCased.Contains("/editor/") && (pathLowerCased.EndsWith("common.uss") ||
                                                           pathLowerCased.EndsWith(EditorGUIUtility.isProSkin ? "dark.uss" : "light.uss"));
        }

        private static void DelayInitialization()
        {
            EditorApplication.update -= DelayInitialization;
            s_DelayedInitialization = true;
        }

        private static void RefreshGlobalCatalog()
        {
            EditorApplication.update -= RefreshGlobalCatalog;
            s_StyleCatalog.Refresh();
            InternalEditorUtility.RepaintAllViews();

            s_RefreshGlobalCatalog = false;
        }

        internal class StylePostprocessor : AssetPostprocessor
        {
            internal static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
            {
                if (!s_DelayedInitialization)
                    return;

                if (s_IgnoreFirstPostProcessImport)
                {
                    s_IgnoreFirstPostProcessImport = false;
                    return;
                }

                bool reloadStyles = false;
                foreach (var path in importedAssets)
                {
                    if (s_StyleCatalog.sheets.Contains(path))
                    {
                        reloadStyles = true;
                    }
                    else if (IsEditorStyleSheet(path))
                    {
                        if (s_StyleCatalog.AddPath(path))
                            reloadStyles = true;
                    }
                }

                if (reloadStyles && !s_RefreshGlobalCatalog)
                {
                    s_RefreshGlobalCatalog = true;
                    EditorApplication.update += RefreshGlobalCatalog;
                }
            }
        }

        // Returns the editor resources absolute file system path.
        public static string dataPath
        {
            get
            {
                return Application.dataPath;
            }
        }

        // Resolve an editor resource asset path.
        public static string ExpandPath(string path)
        {
            return path.Replace("\\", "/");
        }

        // Returns the full file system path of an editor resource asset path.
        public static string GetFullPath(string path)
        {
            if (File.Exists(path))
                return path;
            return new FileInfo(ExpandPath(path)).FullName;
        }

        // Checks if an editor resource asset path exists.
        public static bool Exists(string path)
        {
            if (File.Exists(path))
                return true;
            return File.Exists(ExpandPath(path));
        }

        // Loads an editor resource asset.
        public static T Load<T>(string assetPath, bool isRequired = true) where T : UnityEngine.Object
        {
            var obj = Load(assetPath, typeof(T));
            if (!obj && isRequired)
                throw new FileNotFoundException("Could not find editor resource " + assetPath);
            return obj as T;
        }

        // Returns a globally defined style element by name.
        internal static StyleBlock GetStyle(string selectorName, params StyleState[] states) { return s_StyleCatalog.GetStyle(selectorName, states); }
        internal static StyleBlock GetStyle(int selectorKey, params StyleState[] states) { return s_StyleCatalog.GetStyle(selectorKey, states); }

        // Loads an USS asset into the global style catalog
        internal static void LoadStyles(string ussPath)
        {
            s_StyleCatalog.Load(ussPath);
        }

        // Creates a new style catalog from a set of USS assets.
        internal static StyleCatalog LoadCatalog(string[] ussPaths, bool useResolver = true)
        {
            var catalog = new StyleCatalog(useResolver);
            foreach (var path in ussPaths)
                catalog.AddPath(path);
            catalog.Refresh();
            return catalog;
        }
    }
}
