// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.UIElements.StyleSheets
{
    static class ThemeRegistry
    {
        internal const string k_DefaultStyleSheetPath = "StyleSheets/Generated/Default.tss.asset";
        public const string kThemeScheme = "unity-theme";
        public const string kUnityThemesPath = "Assets/UI Toolkit/UnityThemes";
        private static Dictionary<string, string> m_Themes;

        public static Dictionary<string, string> themes
        {
            get
            {
                if (m_Themes == null)
                {
                    m_Themes = new Dictionary<string, string>();

                    RegisterTheme("default", k_DefaultStyleSheetPath);
                }
                return m_Themes;
            }
        }

        public static void RegisterTheme(string themeName, string path)
        {
            themes[themeName] = path;
        }

        public static void UnregisterTheme(string themeName)
        {
            themes.Remove(themeName);
        }
    }
}
