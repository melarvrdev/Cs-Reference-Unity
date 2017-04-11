// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Collaboration;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System;
using UnityEditor.Web;

namespace UnityEditor
{
    // Displays the Softlocks UI in the various areas of the Editor.
    internal class SoftlockViewController
    {
        private static SoftlockViewController s_Instance;
        public GUIStyle k_Style = null;
        public GUIContent k_Content = null;

        // Stores UI strings for reuse and Editor (inspector) references to trigger
        // a repaint when softlock data changes.
        private SoftlockViewController.Cache m_Cache = null;

        private const string k_TooltipHeader = "Currently Editing:";
        private const string k_TooltipNamePrefix = " \n \u2022  "; // u2022 displays a • (bullet point)

        private SoftlockViewController() {}
        ~SoftlockViewController() {}

        public static SoftlockViewController Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new SoftlockViewController();
                    s_Instance.m_Cache = new Cache();
                }
                return s_Instance;
            }
        }

        // Initialises dependencies.
        public void TurnOn()
        {
            RegisterDataDelegate();
            RegisterDrawDelegates();
        }

        public void TurnOff()
        {
            UnregisterDataDelegate();
            UnregisterDrawDelegates();
        }

        private void UnregisterDataDelegate()
        {
            SoftLockData.SoftlockSubscriber -= Instance.OnSoftlockUpdate;
        }

        private void RegisterDataDelegate()
        {
            UnregisterDataDelegate();
            SoftLockData.SoftlockSubscriber += Instance.OnSoftlockUpdate;
        }

        private void UnregisterDrawDelegates()
        {
            Editor.OnPostHeaderGUI -= Instance.DrawInspectorUI;
            GameObjectTreeViewGUI.OnPostHeaderGUI -= Instance.DrawSceneUI;
        }

        // Connects to the areas of the Editor that display softlocks.
        private void RegisterDrawDelegates()
        {
            UnregisterDrawDelegates();
            Editor.OnPostHeaderGUI += Instance.DrawInspectorUI;
            GameObjectTreeViewGUI.OnPostHeaderGUI += Instance.DrawSceneUI;
        }

        // Returns true when the 'editor' supports Softlock UI and the
        // user has Collaborate permissions.
        private bool HasSoftLocksSupport(Editor editor)
        {
            if (!CollabAccess.Instance.IsServiceEnabled() || editor == null || editor.targets.Length > 1)
            {
                return false;
            }

            if (editor.target == null || !SoftLockData.AllowsSoftLocks(editor.target))
            {
                return false;
            }

            // Support Scene and Game object Inspector headers, not others like MaterialEditor.
            bool hasSupport = true;
            Type editorType = editor.GetType();

            if (editorType != typeof(GameObjectInspector) && editorType != typeof(GenericInspector))
            {
                hasSupport = false;
            }
            return hasSupport;
        }

        // Redraws softlock UI associated with the given list of 'assetGUIDs'.
        public void OnSoftlockUpdate(string[] assetGUIDs)
        {
            // Remove cached UI for the assetGUIDs before triggered a redraw.
            m_Cache.InvalidateAssetGUIDs(assetGUIDs);
            Repaint();
        }

        // Repaints all the areas where softlocks are displayed.
        public void Repaint()
        {
            RepaintInspectors();
            RepaintSceneHierarchy();
        }

        private void RepaintSceneHierarchy()
        {
            List<SceneHierarchyWindow> sceneUIs = SceneHierarchyWindow.GetAllSceneHierarchyWindows();
            foreach (SceneHierarchyWindow sceneUI in sceneUIs)
            {
                sceneUI.Repaint();
            }
        }

        private void RepaintInspectors()
        {
            foreach (Editor editor in m_Cache.GetEditors())
            {
                // Does not repaint when editor is not visible, but the editor's
                // "DockArea" tab will redraw either way.
                editor.Repaint();
            }
        }

        // Draws over the headings in the Hierarchy area.
        public void DrawSceneUI(Rect availableRect, string scenePath)
        {
            if (!CollabAccess.Instance.IsServiceEnabled())
            {
                return;
            }

            string assetGuid = AssetDatabase.AssetPathToGUID(scenePath);
            int lockCount;
            SoftLockData.TryGetSoftlockCount(assetGuid, out lockCount);

            GUIContent content = GetGUIContent();
            content.image = SoftLockUIData.GetIconForSection(SoftLockUIData.SectionEnum.Scene, lockCount);
            content.text = GetDisplayCount(lockCount);
            content.tooltip = Instance.GetTooltip(assetGuid);

            GUIStyle style = GetStyle();
            Vector2 contentSize = GetContentSize(content, style);

            Rect drawRect = new Rect(availableRect.position, contentSize);
            const int kRightMargin = 4;
            drawRect.x = (availableRect.width - drawRect.width) - kRightMargin;
            EditorGUI.LabelField(drawRect, content);
        }

        // Assigned as a callback to Editor.OnPostHeaderGUI
        // Draws the Scene Inspector (Editor.cs) as well as the Game Object Inspector (GameObjectInspector.cs)
        private void DrawInspectorUI(Editor editor)
        {
            if (!HasSoftLocksSupport(editor))
            {
                return;
            }

            m_Cache.StoreEditor(editor);
            string assetGUID = null;
            AssetAccess.TryGetAssetGUIDFromObject(editor.target, out assetGUID);
            GUIContent content = GetInspectorContent(assetGUID);
            const int kHorizontalMargins = 74;
            content.text = FitTextToWidth(content.text, EditorGUIUtility.currentViewWidth - kHorizontalMargins, GetStyle());
            GUILayout.Label(content);
        }

        #region String Helpers

        // Text displayed in the Inspector window.
        // Has the format: (#) FirstName LastName, FirstName LastName
        // e.g. "(2) David McGregor, Beau Hangerfield"
        private string GetInspectorText(string assetGUID, int lockCount)
        {
            string text;
            if (!Instance.m_Cache.TryGetConcatenatedNames(assetGUID, out text))
            {
                text = GetDisplayCount(lockCount);
                text += " ";
                text += ConcatenateNamesForGUID(assetGUID);
                Instance.m_Cache.StoreConcatenatedNames(assetGUID, text);
            }
            return text;
        }

        // Returns a string formatted as a vertical list of names with a heading.
        private string GetTooltip(string assetGUID)
        {
            string formattedText;
            if (!m_Cache.TryGetTooltipForGUID(assetGUID, out formattedText))
            {
                List<string> softLockNames = SoftLockUIData.GetLocksNamesOnAsset(assetGUID);
                formattedText = k_TooltipHeader;

                foreach (string name in softLockNames)
                {
                    formattedText += k_TooltipNamePrefix + name + " ";
                }
                m_Cache.StoreTooltipForGUID(assetGUID, formattedText);
            }
            return formattedText;
        }

        // Provides a comma separated list of names that have a softlock on the
        // the given 'assetGUID'. Returns an empty string if no locks are present.
        private static string ConcatenateNamesForGUID(string assetGUID)
        {
            List<string> softlockNames = SoftLockUIData.GetLocksNamesOnAsset(assetGUID);
            string text = "";
            for (int index = 0; index < softlockNames.Count; index++)
            {
                string name = softlockNames[index];
                if (index > 0)
                {
                    text += ", ";
                }
                text += name;
            }
            return text;
        }

        // Retrieves a previously generated string from cache
        // or creates a string displaying the given 'count' surrounded by brackets.
        // e.g. "(0)"
        private static string GetDisplayCount(int count)
        {
            string totalLocksText;
            if (!Instance.m_Cache.TryGetDisplayCount(count, out totalLocksText))
            {
                totalLocksText = " (" + count.ToString() + ")";
                Instance.m_Cache.StoreDisplayCount(count, totalLocksText);
            }
            return totalLocksText;
        }

        // When the given 'text' exceeds the given 'width', out-of-bound characters
        // are removed as well as a few more to display a trailing ellipsis.
        // If 'text' does not exceed width, text is returned.
        private string FitTextToWidth(string text, float width, GUIStyle style)
        {
            int characterCountVisible = style.GetNumCharactersThatFitWithinWidth(text, width);
            if (characterCountVisible > 1 && characterCountVisible != text.Length)
            {
                string ellipsedText;
                int characterLength = (characterCountVisible - 1);
                if (!Instance.m_Cache.TryGetEllipsedNames(text, characterLength, out ellipsedText))
                {
                    ellipsedText = text.Substring(0, characterLength) + (" \u2026");    // 'horizontal ellipsis' (U+2026) is: ...
                    Instance.m_Cache.StoreEllipsedNames(text, ellipsedText, characterLength);
                }
                return ellipsedText;
            }
            return text;
        }

        #endregion
        #region GUI Content

        public GUIContent GetGUIContent()
        {
            if (k_Content == null)
            {
                k_Content = new GUIContent();
            }
            return k_Content;
        }

        private GUIContent GetInspectorContent(string assetGUID)
        {
            GUIContent content = GetGUIContent();
            int lockCount;
            SoftLockData.TryGetSoftlockCount(assetGUID, out lockCount);
            content.image = SoftLockUIData.GetIconForSection(SoftLockUIData.SectionEnum.Inspector, lockCount);
            content.text = GetInspectorText(assetGUID, lockCount);
            content.tooltip = GetTooltip(assetGUID);
            return content;
        }

        public GUIStyle GetStyle()
        {
            if (k_Style == null)
            {
                k_Style = new GUIStyle(EditorStyles.label);
                k_Style.normal.background = null;
            }
            return k_Style;
        }

        public static Vector2 GetContentSize(GUIContent content, GUIStyle style)
        {
            Vector2 sizeConstraints = new Vector2(0, 0);
            Vector2 size = style.CalcSizeWithConstraints(content, sizeConstraints);
            return size;
        }

        #endregion

        // Stores UI strings for reuse and Editors as WeakReferences.
        private class Cache
        {
            private List<WeakReference> m_EditorReferences = new List<WeakReference>();
            private List<WeakReference> m_CachedWeakReferences = new List<WeakReference>();
            private static Dictionary<int, string> s_CachedStringCount = new Dictionary<int, string>();
            private Dictionary<string, string> m_AssetGUIDToTooltip = new Dictionary<string, string>();
            private Dictionary<string, string> m_AssetGUIDToConcatenatedNames = new Dictionary<string, string>();
            private Dictionary<string, Dictionary<int, string>> m_NamesListToEllipsedNames = new Dictionary<string, Dictionary<int, string>>();

            public Cache() {}

            // Removes cached strings references by the given 'assetGUIDs'.
            public void InvalidateAssetGUIDs(string[] assetGUIDs)
            {
                for (int index = 0; index < assetGUIDs.Length; index++)
                {
                    string assetGUID = assetGUIDs[index];
                    m_AssetGUIDToTooltip.Remove(assetGUID);
                    m_AssetGUIDToConcatenatedNames.Remove(assetGUID);
                }
            }

            // Failure: assigns empty string ("") to 'ellipsedNames', returns false.
            // Success: assigns the cached string to 'ellipsedNames', returns true.
            public bool TryGetEllipsedNames(string allNames, int characterLength, out string ellipsedNames)
            {
                Dictionary<int, string> ellipsedVersions;
                if (m_NamesListToEllipsedNames.TryGetValue(allNames, out ellipsedVersions))
                {
                    return ellipsedVersions.TryGetValue(characterLength, out ellipsedNames);
                }
                ellipsedNames = "";
                return false;
            }

            // 'allNames' and 'characterLength' will be the keys to access the cached 'ellipsedNames'
            // see TryGetEllipsedNames() for retrieval.
            public void StoreEllipsedNames(string allNames, string ellipsedNames, int characterLength)
            {
                Dictionary<int, string> ellipsedVersions;
                if (!m_NamesListToEllipsedNames.TryGetValue(allNames, out ellipsedVersions))
                {
                    ellipsedVersions = new Dictionary<int, string>();
                }
                ellipsedVersions[characterLength] = ellipsedNames;
                m_NamesListToEllipsedNames[allNames] = ellipsedVersions;
            }

            // Failure: assigns empty string ("") to 'names', returns false.
            // Success: assigns the cached string to 'names', returns true.
            public bool TryGetConcatenatedNames(string assetGUID, out string names)
            {
                return m_AssetGUIDToConcatenatedNames.TryGetValue(assetGUID, out names);
            }

            // 'assetGUID' will be the key to access the cached 'names'
            // see TryGetConcatenatedNames() for retrieval.
            public void StoreConcatenatedNames(string assetGUID, string names)
            {
                m_AssetGUIDToConcatenatedNames[assetGUID] = names;
            }

            // Failure: assigns empty string ("") to 'tooltipText', returns false.
            // Success: assigns the cached string to 'tooltipText', returns true.
            public bool TryGetTooltipForGUID(string assetGUID, out string tooltipText)
            {
                return m_AssetGUIDToTooltip.TryGetValue(assetGUID, out tooltipText);
            }

            // 'assetGUID' will be the key to access the cached 'tooltipText'
            // see TryGetTooltipForGUID() for retrieval.
            public void StoreTooltipForGUID(string assetGUID, string tooltipText)
            {
                m_AssetGUIDToTooltip[assetGUID] = tooltipText;
            }

            // Failure: assigns empty string ("") to 'displayText', returns false.
            // Success: assigns the cached string to 'displayText', returns true.
            public bool TryGetDisplayCount(int count, out string displayText)
            {
                return s_CachedStringCount.TryGetValue(count, out displayText);
            }

            // 'count' will be the key to access the cached 'displayText'
            // see TryGetDisplayCount() for retrieval.
            public void StoreDisplayCount(int count, string displayText)
            {
                s_CachedStringCount.Add(count, displayText);
            }

            // Contains at most the list of all previously given Editors
            // via StoreEditor(). Garbage collected Editor(s) will be missing.
            public List<Editor> GetEditors()
            {
                List<Editor> editors = new List<Editor>();

                for (int index = 0; index < m_EditorReferences.Count; index++)
                {
                    WeakReference reference = m_EditorReferences[index];
                    Editor editor = reference.Target as Editor;

                    if (editor == null)
                    {
                        m_EditorReferences.RemoveAt(index);
                        m_CachedWeakReferences.Add(reference);
                        index--;
                    }
                    else
                    {
                        editors.Add(editor);
                    }
                }
                return editors;
            }

            // Stores the Editor in a WeakReference.
            public void StoreEditor(Editor editor)
            {
                bool canAdd = true;

                // Check for duplicates and purge any null targets.
                for (int index = 0; canAdd && (index < m_EditorReferences.Count); index++)
                {
                    WeakReference reference = m_EditorReferences[index];
                    Editor storedEditor = reference.Target as Editor;

                    if (storedEditor == null)
                    {
                        m_EditorReferences.RemoveAt(index);
                        m_CachedWeakReferences.Add(reference);
                        index--;
                    }
                    else if (storedEditor == editor)
                    {
                        canAdd = false;
                        break;
                    }
                }

                if (canAdd)
                {
                    WeakReference editorReference;

                    // Reuse any old WeakReference if available.
                    if (m_CachedWeakReferences.Count > 0)
                    {
                        editorReference = m_CachedWeakReferences[0];
                        m_CachedWeakReferences.RemoveAt(0);
                    }
                    else
                    {
                        editorReference = new WeakReference(null);
                    }
                    editorReference.Target = editor;
                    m_EditorReferences.Add(editorReference);
                }
            }
        }
    }
}

