// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Reflection;
using UnityEditor.Experimental.UIElements.Debugger;

namespace UnityEditor
{
    internal class HostView : GUIView
    {
        internal static Color kViewColor = new Color(0.76f, 0.76f, 0.76f, 1);
        internal static PrefColor kPlayModeDarken = new PrefColor("Playmode tint", .8f, .8f, .8f, 1);

        internal static event Action<HostView> actualViewChanged;

        internal GUIStyle background;
        [SerializeField] private EditorWindow m_ActualView;

        [System.NonSerialized] private Rect m_BackgroundClearRect = new Rect(0, 0, 0, 0);
        [System.NonSerialized] protected readonly RectOffset m_BorderSize = new RectOffset();

        private bool m_IsGameView;

        internal EditorWindow actualView
        {
            get { return m_ActualView; }
            set
            {
                if (m_ActualView == value)
                    return;
                DeregisterSelectedPane(true);
                m_ActualView = value;
                m_IsGameView = m_ActualView is GameView;
                RegisterSelectedPane();
                actualViewChanged?.Invoke(this);
            }
        }

        protected virtual void UpdateViewMargins(EditorWindow view)
        {
        }

        protected override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            SetActualViewPosition(newPos);
        }

        protected virtual void SetActualViewPosition(Rect newPos)
        {
            if (m_ActualView != null)
            {
                m_ActualView.m_Pos = newPos;
                UpdateViewMargins(m_ActualView);
                m_ActualView.OnResized();
            }
        }

        protected override void SetWindow(ContainerWindow win)
        {
            base.SetWindow(win);
            if (m_ActualView != null)
            {
                UpdateViewMargins(m_ActualView);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            background = null;
            RegisterSelectedPane();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            DeregisterSelectedPane(false);
        }

        protected override void OldOnGUI()
        {
            ClearBackground();

            // Call reset GUI state as first thing so GUI.color is correct when drawing window decoration.
            EditorGUIUtility.ResetGUIState();
            DoWindowDecorationStart();
            if (background == null)
            {
                background = "hostview";
                // Fix annoying GUILayout issue: When using guilayout in Utility windows there was always padded 10 px at the top! Todo: Fix this in EditorResources
                background.padding.top = 0;
            }

            using (new GUILayout.VerticalScope(background))
            {
                if (actualView)
                    actualView.m_Pos = screenPosition;

                try
                {
                    Invoke("OnGUI");
                }
                finally
                {
                    EditorGUIUtility.ResetGUIState();

                    if (m_ActualView != null)
                        if (m_ActualView.m_FadeoutTime != 0 && Event.current.type == EventType.Repaint)
                            m_ActualView.DrawNotification();

                    DoWindowDecorationEnd();
                    EditorGUI.ShowRepaints();
                }
            }
        }

        protected override bool OnFocus()
        {
            Invoke("OnFocus");

            // Callback could have killed us. If so, die now...
            if (!this)
                return false;

            Repaint();
            return true;
        }

        internal void OnLostFocus()
        {
            EditorGUI.EndEditingActiveTextField();
            Invoke("OnLostFocus");
            Repaint();
        }

        protected override void OnDestroy()
        {
            if (m_ActualView)
                UnityEngine.Object.DestroyImmediate(m_ActualView, true);
            base.OnDestroy();
        }

        protected System.Type[] GetPaneTypes()
        {
            return new System.Type[] {
                typeof(SceneView),
                typeof(GameView),
                typeof(InspectorWindow),
                typeof(SceneHierarchyWindow),
                typeof(ProjectBrowser),
                typeof(ProfilerWindow),
                typeof(AnimationWindow)
            };
        }

        // Messages sent by Unity to editor windows today.
        // The implementation is not very good, but oh well... it gets the message across.
        internal void OnProjectChange()
        {
            Invoke("OnProjectChange");
        }

        internal void OnSelectionChange()
        {
            Invoke("OnSelectionChange");
        }

        internal void OnDidOpenScene()
        {
            Invoke("OnDidOpenScene");
        }

        internal void OnInspectorUpdate()
        {
            Invoke("OnInspectorUpdate");
        }

        internal void OnHierarchyChange()
        {
            Invoke("OnHierarchyChange");
        }

        System.Reflection.MethodInfo GetPaneMethod(string methodName)
        {
            return GetPaneMethod(methodName, m_ActualView);
        }

        System.Reflection.MethodInfo GetPaneMethod(string methodName, object obj)
        {
            if (obj == null)
                return null;

            System.Type t = obj.GetType();

            while (t != null)
            {
                var method = t.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                    return method;

                t = t.BaseType;
            }
            return null;
        }

        static public void EndOffsetArea()
        {
            if (Event.current.type == EventType.Used)
                return;
            GUILayoutUtility.EndLayoutGroup();
            GUI.EndGroup();
        }

        static public void BeginOffsetArea(Rect screenRect, GUIContent content, GUIStyle style)
        {
            GUILayoutGroup g = EditorGUILayoutUtilityInternal.BeginLayoutArea(style, typeof(GUILayoutGroup));
            switch (Event.current.type)
            {
                case EventType.Layout:
                    g.resetCoords = false;
                    g.minWidth = g.maxWidth = screenRect.width + 1;
                    g.minHeight = g.maxHeight = screenRect.height + 2;
                    g.rect = Rect.MinMaxRect(-1, -1, g.rect.xMax, g.rect.yMax - 10);
                    break;
            }
            GUI.BeginGroup(screenRect, content, style);
        }

        static class HostViewStyles
        {
            public static readonly GUIStyle overlay = "dockareaoverlay";
        }

        public void InvokeOnGUI(Rect onGUIPosition, Rect viewRect)
        {
            // Handle window reloading.
            if (Unsupported.IsDeveloperMode() &&
                actualView != null &&
                Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.F5)
            {
                if (Event.current.control)
                    DebugWindow(actualView);
                else
                    Reload(actualView);
                return;
            }

            DoWindowDecorationStart();

            if (m_IsGameView) // GameView exits GUI, so draw overlay border earlier
                GUI.Box(onGUIPosition, GUIContent.none, HostViewStyles.overlay);

            BeginOffsetArea(viewRect, GUIContent.none, "TabWindowBackground");
            EditorGUIUtility.ResetGUIState();
            bool isExitGUIException = false;
            try
            {
                using (new PerformanceTracker(actualView.GetType().Name + ".OnGUI"))
                    Invoke("OnGUI");
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException is ExitGUIException)
                    isExitGUIException = true;
                throw;
            }
            finally
            {
                // We can't reset gui state after ExitGUI we just want to bail completely
                if (!isExitGUIException)
                {
                    bool isRepaint = (Event.current != null && Event.current.type == EventType.Repaint);
                    if (actualView != null && actualView.m_FadeoutTime != 0 && isRepaint)
                        actualView.DrawNotification();

                    EndOffsetArea();

                    EditorGUIUtility.ResetGUIState();

                    DoWindowDecorationEnd();

                    if (isRepaint)
                        HostViewStyles.overlay.Draw(onGUIPosition, GUIContent.none, 0);
                }
            }
        }

        ///  TODO: Optimize with Delegate.CreateDelegate
        protected void Invoke(string methodName)
        {
            Invoke(methodName, m_ActualView);
        }

        protected void Invoke(string methodName, object obj)
        {
            System.Reflection.MethodInfo mi = GetPaneMethod(methodName, obj);
            mi?.Invoke(obj, null);
        }

        protected void RegisterSelectedPane()
        {
            if (!m_ActualView)
                return;
            m_ActualView.m_Parent = this;

            visualTree.Add(m_ActualView.rootVisualContainer);
            panel.getViewDataDictionary = m_ActualView.GetViewDataDictionary;
            panel.savePersistentViewData = m_ActualView.SavePersistentViewData;
            panel.name = m_ActualView.GetType().Name;

            if (GetPaneMethod("Update") != null)
                EditorApplication.update += SendUpdate;

            if (GetPaneMethod("ModifierKeysChanged") != null)
                EditorApplication.modifierKeysChanged += SendModKeysChanged;

            m_ActualView.MakeParentsSettingsMatchMe();

            if (m_ActualView.m_FadeoutTime != 0)
            {
                EditorApplication.update += m_ActualView.CheckForWindowRepaint;
            }

            try
            {
                Invoke("OnBecameVisible");
                Invoke("OnFocus");
            }
            catch (TargetInvocationException ex)
            {
                // We need to catch these so the window initialization doesn't get screwed
                if (ex.InnerException != null)
                    Debug.LogError(ex.InnerException.GetType().Name + ":" + ex.InnerException.Message);
            }

            UpdateViewMargins(m_ActualView);
        }

        protected void DeregisterSelectedPane(bool clearActualView)
        {
            if (!m_ActualView)
                return;

            if (m_ActualView.rootVisualContainer.shadow.parent == visualTree)
            {
                visualTree.Remove(m_ActualView.rootVisualContainer);
                panel.getViewDataDictionary = null;
                panel.savePersistentViewData = null;
            }

            if (GetPaneMethod("Update") != null)
                EditorApplication.update -= SendUpdate;

            if (GetPaneMethod("ModifierKeysChanged") != null)
                EditorApplication.modifierKeysChanged -= SendModKeysChanged;

            if (m_ActualView.m_FadeoutTime != 0)
            {
                EditorApplication.update -= m_ActualView.CheckForWindowRepaint;
            }

            if (clearActualView)
            {
                EditorWindow oldActualView = m_ActualView;
                m_ActualView = null;
                Invoke("OnLostFocus", oldActualView);
                Invoke("OnBecameInvisible", oldActualView);
            }
        }

        void SendUpdate() { Invoke("Update"); }
        void SendModKeysChanged() { Invoke("ModifierKeysChanged"); }

        internal RectOffset borderSize => GetBorderSize();

        protected virtual RectOffset GetBorderSize() { return m_BorderSize; }

        protected void ShowGenericMenu(float topOffset)
        {
            GUIStyle gs = "PaneOptions";
            Rect paneMenu = new Rect(position.width - gs.fixedWidth - 4, topOffset, gs.fixedWidth, gs.fixedHeight);
            if (EditorGUI.DropdownButton(paneMenu, GUIContent.none, FocusType.Passive, "PaneOptions"))
                PopupGenericMenu(m_ActualView, paneMenu);

            // Give panes an option of showing a small button next to the generic menu (used for inspector lock icon
            MethodInfo mi = GetPaneMethod("ShowButton", m_ActualView);
            if (mi != null)
            {
                const float rightOffset = 18f;
                object[] lockButton = { new Rect(position.width - gs.fixedWidth - rightOffset, topOffset - 4f, 16, 16) };
                mi.Invoke(m_ActualView, lockButton);
            }

            // Developer-mode render doc button to enable capturing any HostView content/panels
            if (Unsupported.IsDeveloperMode() && UnityEditorInternal.RenderDoc.IsLoaded() && UnityEditorInternal.RenderDoc.IsSupported())
            {
                Rect renderDocRect = new Rect(position.width - gs.fixedWidth - (mi == null ? 20 : 36), Mathf.Floor(background.margin.top + 4), 17, 16);
                RenderDocCaptureButton(renderDocRect);
            }
        }

        static GUIContent s_RenderDocContent;
        private void RenderDocCaptureButton(Rect r)
        {
            if (s_RenderDocContent == null)
                s_RenderDocContent = EditorGUIUtility.TrIconContent("renderdoc", "Capture this view in RenderDoc");

            Rect r2 = new Rect(r.xMax - r.width, r.y, r.width, r.height);
            if (GUI.Button(r2, s_RenderDocContent, EditorStyles.iconButton))
                CaptureRenderDocFullContent();
        }

        public void PopupGenericMenu(EditorWindow view, Rect pos)
        {
            GenericMenu menu = new GenericMenu();

            IHasCustomMenu menuProviderFactoryThingy = view as IHasCustomMenu;
            menuProviderFactoryThingy?.AddItemsToMenu(menu);

            AddDefaultItemsToMenu(menu, view);
            menu.DropDown(pos);
            Event.current.Use();
        }

        private void Inspect(object userData)
        {
            Selection.activeObject = (UnityEngine.Object)userData;
        }

        internal void DebugWindow(object userData)
        {
            EditorWindow window = userData as EditorWindow;
            if (window == null)
                return;

            UIElementsDebugger.OpenAndInspectWindow(window);
        }

        internal void Reload(object userData)
        {
            EditorWindow window = userData as EditorWindow;
            if (window == null)
                return;

            // Get some info on the existing window.
            Type windowType = window.GetType();

            // Save what we can of the window.
            string windowJson = EditorJsonUtility.ToJson(window);

            DockArea dockArea = window.m_Parent as DockArea;
            if (dockArea != null)
            {
                int windowIndex = dockArea.m_Panes.IndexOf(window);

                // Destroy window.
                dockArea.RemoveTab(window, false); // Don't kill dock if empty.
                UnityEngine.Object.DestroyImmediate(window, true);

                // Create window.
                window = EditorWindow.CreateInstance(windowType) as EditorWindow;
                dockArea.AddTab(windowIndex, window);
            }
            else
            {
                // Close the existing window.
                window.Close();

                // Recreate window.
                window = EditorWindow.CreateInstance(windowType) as EditorWindow;
                if (window != null)
                    window.Show();
            }

            // Restore what we can of the window.
            EditorJsonUtility.FromJsonOverwrite(windowJson, window);
        }

        protected virtual void AddDefaultItemsToMenu(GenericMenu menu, EditorWindow window)
        {
            if (menu.GetItemCount() != 0)
                menu.AddSeparator("");

            if (Unsupported.IsDeveloperMode())
            {
                menu.AddItem(EditorGUIUtility.TrTextContent("Inspect Window"), false, Inspect, window);
                menu.AddItem(EditorGUIUtility.TrTextContent("Inspect View"), false, Inspect, window.m_Parent);
                menu.AddItem(EditorGUIUtility.TrTextContent("Reload Window _f5"), false, Reload, window);
                menu.AddItem(EditorGUIUtility.TextContent("UIElements Debugger _%f5"), false, DebugWindow, window);

                menu.AddSeparator("");
            }
        }

        protected void ClearBackground()
        {
            if (Event.current.type != EventType.Repaint)
                return;
            EditorWindow view = actualView;
            if (view != null && view.dontClearBackground)
            {
                if (backgroundValid && position == m_BackgroundClearRect)
                    return;
            }
            Color col = EditorGUIUtility.isProSkin ? EditorGUIUtility.kDarkViewBackground : kViewColor;
            GL.Clear(true, true, EditorApplication.isPlayingOrWillChangePlaymode ? col * kPlayModeDarken : col);
            backgroundValid = true;
            m_BackgroundClearRect = position;
        }
    }
}
