// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements.Experimental.Debugger;

namespace UnityEditor.UIElements
{
    static class UIToolkitProjectSettings
    {
        const string k_EditorExtensionModeKey = "UIBuilder.EditorExtensionModeKey";
        const string k_HideNotificationAboutMissingUITKPackage = "UIBuilder.HideNotificationAboutMissingUITKPackage";
        const string k_DisableMouseWheelZooming = "UIBuilder.DisableMouseWheelZooming";
        const string k_EnableAbsolutePositionPlacement = "UIBuilder.EnableAbsolutePositionPlacement";
        const string k_EnableEventDebugger = "UIToolkit.EnableEventDebugger";

        public static bool enableEditorExtensionModeByDefault
        {
            get => GetBool(k_EditorExtensionModeKey);
            set => SetBool(k_EditorExtensionModeKey, value);
        }

        public static bool disableMouseWheelZooming
        {
            get => GetBool(k_DisableMouseWheelZooming);
            set => SetBool(k_DisableMouseWheelZooming, value);
        }

        public static bool hideNotificationAboutMissingUITKPackage
        {
            get => GetBool(k_HideNotificationAboutMissingUITKPackage);
            set => SetBool(k_HideNotificationAboutMissingUITKPackage, value);
        }

        public static bool enableAbsolutePositionPlacement
        {
            get => Unsupported.IsDeveloperMode() && GetBool(k_EnableAbsolutePositionPlacement);
            set => SetBool(k_EnableAbsolutePositionPlacement, value);
        }

        public static bool enableEventDebugger
        {
            get => GetBool(k_EnableEventDebugger);
            set
            {
                SetBool(k_EnableEventDebugger, value);
                if (value)
                    Menu.AddMenuItem("Window/UI Toolkit/Event Debugger", "", false, 3010,
                        UIElementsEventsDebugger.ShowUIElementsEventDebugger, null);
                else
                    EditorApplication.CallDelayed(RemoveEventDebuggerMenuItem);
            }
        }

        static void RemoveEventDebuggerMenuItem()
        {
            var menuItems = Menu.GetMenuItems("Window/UI Toolkit/Event Debugger", false, false);
            if (menuItems != null)
            {
                Menu.RemoveMenuItem("Window/UI Toolkit/Event Debugger");
                Menu.RebuildAllMenus();
            }
        }

        static bool GetBool(string name)
        {
            var value = EditorUserSettings.GetConfigValue(name);
            if (string.IsNullOrEmpty(value))
                return false;

            return Convert.ToBoolean(value);
        }

        static void SetBool(string name, bool value)
        {
            EditorUserSettings.SetConfigValue(name, value.ToString());
        }

        internal static void Reset()
        {
            EditorUserSettings.SetConfigValue(k_EditorExtensionModeKey, null);
            EditorUserSettings.SetConfigValue(k_HideNotificationAboutMissingUITKPackage, null);
            EditorUserSettings.SetConfigValue(k_DisableMouseWheelZooming, null);
            EditorUserSettings.SetConfigValue(k_EnableAbsolutePositionPlacement, null);
            EditorUserSettings.SetConfigValue(k_EnableEventDebugger, null);
        }
    }
}
