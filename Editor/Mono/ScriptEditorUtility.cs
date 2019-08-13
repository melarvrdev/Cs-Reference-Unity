// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.CodeEditor;
using UnityEditor.Utils;

namespace UnityEditorInternal
{
    public class ScriptEditorUtility
    {
        // Keep in sync with enum ScriptEditorType in ExternalEditor.h
        public enum ScriptEditor { SystemDefault = 0, MonoDevelop = 1, VisualStudio = 2, VisualStudioExpress = 3, Other = 32 }

        public struct Installation
        {
            public string Name;
            public string Path;
        }

        static readonly List<Func<Installation[]>> k_PathCallbacks = new List<Func<Installation[]>>();

        [Obsolete("Use UnityEditor.ScriptEditor.Register()", false)]
        public static void RegisterIde(Func<Installation[]> pathCallBack)
        {
            k_PathCallbacks.Add(pathCallBack);
        }

        [Obsolete("This functionality is going to be removed. See IExternalCodeEditor for more information", false)]
        public static ScriptEditor GetScriptEditorFromPath(string path)
        {
            string lowerCasePath = path.ToLower();

            if (lowerCasePath == "internal")
                return ScriptEditor.SystemDefault;

            if (lowerCasePath.Contains("monodevelop") || lowerCasePath.Contains("xamarinstudio") || lowerCasePath.Contains("xamarin studio"))
                return ScriptEditor.MonoDevelop;

            if (lowerCasePath.EndsWith("devenv.exe"))
                return ScriptEditor.VisualStudio;

            if (lowerCasePath.EndsWith("vcsexpress.exe"))
                return ScriptEditor.VisualStudioExpress;

            // Visual Studio for Mac is based on MonoDevelop
            if (IsVisualStudioForMac(path))
                return ScriptEditor.MonoDevelop;

            return ScriptEditor.Other;
        }

        internal static bool IsVisualStudioForMac(string path)
        {
            var lowerCasePath = path.ToLower();
            var filename = Path.GetFileName(Paths.UnifyDirectorySeparator(lowerCasePath)).Replace(" ", "");
            return filename.StartsWith("visualstudio") && !filename.Contains("code") && filename.EndsWith(".app");
        }

        #pragma warning disable 618
        public static string GetExternalScriptEditor()
        {
            var editor =  EditorPrefs.GetString("kScriptsDefaultApp");

            if (!string.IsNullOrEmpty(editor))
                return editor;

            // If no script editor is set, try to use first found supported one.
            var editorPaths = GetFoundScriptEditorPaths(Application.platform);

            if (editorPaths.Count > 0)
                return editorPaths.Keys.ToArray()[0];

            return string.Empty;
        }

        [Obsolete("This method has been moved to CodeEditor.SetExternalScriptEditor", false)]
        public static void SetExternalScriptEditor(string path)
        {
            CodeEditor.SetExternalScriptEditor(path);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This functionality has been moved to the IExternalCodeEditor packages", true)]
        static string GetScriptEditorArgsKey(string path)
        {
            // Starting in Unity 5.5, we support setting script editor arguments on OSX and
            // use then when opening the script editor.
            // Before Unity 5.5, we would still save the default script editor args in EditorPrefs,
            // even though we never used them. This means that the user potentially has some
            // script editor args saved and once he upgrades to 5.5, they will be used when
            // open the script editor. Which unintended and causes a regression in behaviour.
            // So on OSX we change the key for per application for script editor args,
            // to avoid reading the one from previous versions.
            if (Application.platform == RuntimePlatform.OSXEditor)
                return "kScriptEditorArgs_" + path;

            return "kScriptEditorArgs" + path;
        }

        static string GetDefaultStringEditorArgs()
        {
            // On OSX there is a built-in mechanism for opening files in apps.
            // We use this mechanism when the external script editor args are not set.
            // Which was the only support behaviour before Unity 5.5. We therefor
            // default to this behavior.
            // If the script editor args are set, we only launch the script editor with args
            // specified and do not use the built-in mechanism for opening script files.
            if (Application.platform == RuntimePlatform.OSXEditor)
                return "";

            return "\"$(File)\"";
        }

        public static string GetExternalScriptEditorArgs()
        {
            throw new NotSupportedException("This functionality has been moved to the IExternalCodeEditor packages");
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This functionality has been moved to the IExternalCodeEditor packages", true)]
        public static void SetExternalScriptEditorArgs(string args)
        {
            throw new NotSupportedException("This functionality has been moved to the IExternalCodeEditor packages");
        }

        [Obsolete("Use UnityEditor.ScriptEditor.GetCurrentEditor()", false)]
        public static ScriptEditor GetScriptEditorFromPreferences()
        {
            return GetScriptEditorFromPath(GetExternalScriptEditor());
        }

        [Obsolete("This method is being internalized, please use UnityEditorInternal.CodeEditorUtility.GetFoundScriptEditorPaths", false)]
        public static Dictionary<string, string> GetFoundScriptEditorPaths(RuntimePlatform platform)
        {
            var result = new Dictionary<string, string>();

            if (platform == RuntimePlatform.OSXEditor)
            {
                AddIfDirectoryExists("Visual Studio", "/Applications/Visual Studio.app", result);
                AddIfDirectoryExists("Visual Studio (Preview)", "/Applications/Visual Studio (Preview).app", result);
            }

            foreach (var callback in k_PathCallbacks)
            {
                var pathCallbacks = callback.Invoke();
                foreach (var pathCallback in pathCallbacks)
                {
                    AddIfDirectoryExists(pathCallback.Name, pathCallback.Path, result);
                }
            }

            return result;
        }

        static void AddIfDirectoryExists(string name, string path, Dictionary<string, string> list)
        {
            if (list.ContainsKey(path))
                return;
            if (Directory.Exists(path)) list.Add(path, name);
            else if (File.Exists(path)) list.Add(path, name);
        }
    }
}
