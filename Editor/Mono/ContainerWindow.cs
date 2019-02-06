// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.StyleSheets;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;

namespace UnityEditor
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial class ContainerWindow : ScriptableObject
    {
        [SerializeField] MonoReloadableIntPtr m_WindowPtr;
        [SerializeField] Rect m_PixelRect;
        [SerializeField] int m_ShowMode;
        [SerializeField] string m_Title = "";

        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_MainView")] View m_RootView;
        [SerializeField] Vector2 m_MinSize = new Vector2(120, 80);
        [SerializeField] Vector2 m_MaxSize = new Vector2(4000, 4000);

        internal bool m_DontSaveToLayout = false;
        private int m_ButtonCount;
        private float m_TitleBarWidth;

        const float kTitleHeight = 24f, kButtonWidth = 16f, kButtonHeight = 16f;

        static internal bool macEditor => Application.platform == RuntimePlatform.OSXEditor;

        private static class Styles
        {
            // Title Bar Buttons (Non)
            public static GUIStyle buttonMin = "WinBtnMinMac";
            public static GUIStyle buttonInactive = "WinBtnInactiveMac";
            public static GUIStyle buttonClose = macEditor ? "WinBtnCloseMac" : "WinBtnClose";
            public static GUIStyle buttonMax = macEditor ? "WinBtnMaxMac" : "WinBtnMax";
            public static GUIStyle buttonRestore = macEditor ? "WinBtnRestoreMac" : "WinBtnRestore";

            public static float borderSize => macEditor ? osxBorderSize : winBorderSize;
            public static float buttonMargin => macEditor ? osxBorderMargin : winBorderMargin;

            public static SVC<float> buttonTop = new SVC<float>("--container-window-button-top-margin");

            private static SVC<float> winBorderSize = new SVC<float>("--container-window-buttons-right-margin-win");
            private static SVC<float> osxBorderSize = new SVC<float>("--container-window-buttons-right-margin-osx");
            private static SVC<float> winBorderMargin = new SVC<float>("--container-window-button-left-right-margin-win");
            private static SVC<float> osxBorderMargin = new SVC<float>("--container-window-button-left-right-margin-osx");
        }

        private const float kButtonCountOSX = 3;
        private const float kButtonCountWin = 2;
        static internal float buttonHorizontalSpace => (kButtonWidth + Styles.buttonMargin * 2f);
        static internal float buttonStackWidth => buttonHorizontalSpace * (macEditor ? kButtonCountOSX : kButtonCountWin) + Styles.borderSize;

        public ContainerWindow()
        {
            m_PixelRect = new Rect(0, 0, 400, 300);
        }

        internal void __internalAwake()
        {
            hideFlags = HideFlags.DontSave;
        }

        internal ShowMode showMode => (ShowMode)m_ShowMode;

        internal static bool IsPopup(ShowMode mode)
        {
            return (ShowMode.PopupMenu == mode);
        }

        internal bool isPopup => IsPopup((ShowMode)m_ShowMode);

        internal void ShowPopup()
        {
            ShowPopupWithMode(ShowMode.PopupMenu, true);
        }

        internal void ShowTooltip()
        {
            ShowPopupWithMode(ShowMode.Tooltip, false);
        }

        internal void ShowPopupWithMode(ShowMode mode, bool giveFocus)
        {
            m_ShowMode = (int)mode;
            Internal_Show(m_PixelRect, m_ShowMode, m_MinSize, m_MaxSize);
            if (m_RootView)
                m_RootView.SetWindowRecurse(this);
            Internal_SetTitle(m_Title);
            Save();
            //  only set focus if mode is a popupMenu.
            Internal_BringLiveAfterCreation(false, giveFocus);

            // Fit window to screen - needs to be done after bringing the window live
            position = FitWindowRectToScreen(m_PixelRect, true, false);
            rootView.position = new Rect(0, 0, Mathf.Ceil(m_PixelRect.width), Mathf.Ceil(m_PixelRect.height));
            rootView.Reflow();
        }

        static Color skinBackgroundColor => EditorGUIUtility.isProSkin ? Color.gray.RGBMultiplied(0.3f).AlphaMultiplied(0.5f) : Color.gray.AlphaMultiplied(0.32f);

        // Show the editor window.
        public void Show(ShowMode showMode, bool loadPosition, bool displayImmediately, bool setFocus)
        {
            if (showMode == ShowMode.AuxWindow)
                showMode = ShowMode.Utility;

            if (showMode == ShowMode.Utility || showMode == ShowMode.ModalUtility || IsPopup(showMode))
                m_DontSaveToLayout = true;

            m_ShowMode = (int)showMode;

            // Load previous position/size
            if (!isPopup)
                Load(loadPosition);

            Internal_Show(m_PixelRect, m_ShowMode, m_MinSize, m_MaxSize);

            // Tell the main view its now in this window (quick hack to get platform-specific code to move its views to the right window)
            if (m_RootView)
                m_RootView.SetWindowRecurse(this);
            Internal_SetTitle(m_Title);

            SetBackgroundColor(skinBackgroundColor);

            Internal_BringLiveAfterCreation(displayImmediately, setFocus);

            // Window could be killed by now in user callbacks...
            if (!this)
                return;

            // Fit window to screen - needs to be done after bringing the window live
            position = FitWindowRectToScreen(m_PixelRect, true, false);
            rootView.position = new Rect(0, 0, Mathf.Ceil(m_PixelRect.width), Mathf.Ceil(m_PixelRect.height));

            rootView.Reflow();

            // save position right away
            Save();
        }

        public void OnEnable()
        {
            if (m_RootView)
                m_RootView.Initialize(this);
            SetBackgroundColor(skinBackgroundColor);
        }

        public void SetMinMaxSizes(Vector2 min, Vector2 max)
        {
            m_MinSize = min;
            m_MaxSize = max;
            Rect r = position;
            Rect r2 = r;
            r2.width = Mathf.Clamp(r.width, min.x, max.x);
            r2.height = Mathf.Clamp(r.height, min.y, max.y);
            if (r2.width != r.width || r2.height != r.height)
                position = r2;
            Internal_SetMinMaxSizes(min, max);
        }

        internal void InternalCloseWindow()
        {
            Save();
            if (m_RootView)
            {
                if (m_RootView is GUIView)
                    ((GUIView)m_RootView).RemoveFromAuxWindowList();
                DestroyImmediate(m_RootView, true);
                m_RootView = null;
            }

            DestroyImmediate(this, true);
        }

        public void Close()
        {
            Save();

            // Guard against destroy window or window in the process of being destroyed.
            if (this && m_WindowPtr.m_IntPtr != IntPtr.Zero)
                InternalClose();
            DestroyImmediate(this, true);
        }

        internal bool IsNotDocked()
        {
            return ( // hallelujah

                (m_ShowMode == (int)ShowMode.Utility || m_ShowMode == (int)ShowMode.AuxWindow) ||

                (rootView is SplitView &&
                    rootView.children.Length == 1 &&
                    rootView.children[0] is DockArea &&
                    ((DockArea)rootView.children[0]).m_Panes.Count == 1)
            );
        }

        private string NotDockedWindowID()
        {
            if (IsNotDocked())
            {
                HostView v = rootView as HostView;

                if (v == null)
                {
                    if (rootView is SplitView)
                        v = (HostView)rootView.children[0];
                    else
                        return rootView.GetType().ToString();
                }


                return (m_ShowMode == (int)ShowMode.Utility || m_ShowMode == (int)ShowMode.AuxWindow) ? v.actualView.GetType().ToString()
                    : ((DockArea)rootView.children[0]).m_Panes[0].GetType().ToString();
            }

            return null;
        }

        public void Save()
        {
            // only save it if its not docked and its not the MainWindow
            if ((m_ShowMode != (int)ShowMode.MainWindow) && IsNotDocked() && !IsZoomed())
            {
                string ID = NotDockedWindowID();

                // save position/size
                EditorPrefs.SetFloat(ID + "x", m_PixelRect.x);
                EditorPrefs.SetFloat(ID + "y", m_PixelRect.y);
                EditorPrefs.SetFloat(ID + "w", m_PixelRect.width);
                EditorPrefs.SetFloat(ID + "h", m_PixelRect.height);
            }
        }

        private void Load(bool loadPosition)
        {
            if ((m_ShowMode != (int)ShowMode.MainWindow) && IsNotDocked())
            {
                string ID = NotDockedWindowID();

                // get position/size
                Rect p = m_PixelRect;
                if (loadPosition)
                {
                    p.x = EditorPrefs.GetFloat(ID + "x", m_PixelRect.x);
                    p.y = EditorPrefs.GetFloat(ID + "y", m_PixelRect.y);
                }
                p.width = Mathf.Max(EditorPrefs.GetFloat(ID + "w", m_PixelRect.width), m_MinSize.x);
                p.width = Mathf.Min(p.width, m_MaxSize.x);
                p.height = Mathf.Max(EditorPrefs.GetFloat(ID + "h", m_PixelRect.height), m_MinSize.y);
                p.height = Mathf.Min(p.height, m_MaxSize.y);
                m_PixelRect = p;
            }
        }

        internal void OnResize()
        {
            if (rootView == null)
                return;
            rootView.position = new Rect(0, 0, Mathf.Ceil(position.width), Mathf.Ceil(position.height));

            // save position
            Save();
        }

        // The title of the window
        public string title
        {
            get { return m_Title; }
            set { m_Title = value; Internal_SetTitle(value); }
        }

        // Array of all visible ContainerWindows, from frontmost to last
        static List<ContainerWindow> s_AllWindows = new List<ContainerWindow>();
        public static ContainerWindow[] windows
        {
            get
            {
                s_AllWindows.Clear();
                GetOrderedWindowList();
                return s_AllWindows.ToArray();
            }
        }

        internal void AddToWindowList()
        {
            s_AllWindows.Add(this);
        }

        // TODO: Handle title bar height and other things
        public Vector2 WindowToScreenPoint(Vector2 windowPoint)
        {
            Vector2 hmm = Internal_GetTopleftScreenPosition();
            return windowPoint + hmm;// + new Vector2 (position.x, position.y) + hmm;
        }

        public View rootView
        {
            get { return m_RootView; }
            set
            {
                m_RootView = value;
                m_RootView.SetWindowRecurse(this);
                m_RootView.position = new Rect(0, 0, Mathf.Ceil(position.width), Mathf.Ceil(position.height));
                m_MinSize = value.minSize;
                m_MaxSize = value.maxSize;
            }
        }

        public SplitView rootSplitView
        {
            get
            {
                if (m_ShowMode == (int)ShowMode.MainWindow && rootView && rootView.children.Length == 3)
                    return rootView.children[1] as SplitView;
                else
                    return rootView as SplitView;
            }
        }

        internal string DebugHierarchy()
        {
            return rootView.DebugHierarchy(0);
        }

        internal Rect GetDropDownRect(Rect buttonRect, Vector2 minSize, Vector2 maxSize, PopupLocation[] locationPriorityOrder)
        {
            return PopupLocationHelper.GetDropDownRect(buttonRect, minSize, maxSize, this, locationPriorityOrder);
        }

        internal Rect GetDropDownRect(Rect buttonRect, Vector2 minSize, Vector2 maxSize)
        {
            return PopupLocationHelper.GetDropDownRect(buttonRect, minSize, maxSize, this);
        }

        internal Rect FitPopupWindowRectToScreen(Rect rect, float minimumHeight)
        {
            const float maxHeight = 900;
            float spaceFromBottom = 0f;
            if (Application.platform == RuntimePlatform.OSXEditor)
                spaceFromBottom = 10f;

            float minHeight = minimumHeight + spaceFromBottom;
            Rect p = rect;
            p.height = Mathf.Min(p.height, maxHeight);
            p.height += spaceFromBottom;
            p = FitWindowRectToScreen(p, true, true);

            float newHeight = Mathf.Max(p.yMax - rect.y, minHeight);
            p.y = p.yMax - newHeight;
            p.height = newHeight - spaceFromBottom;

            return p;
        }

        public void HandleWindowDecorationEnd(Rect windowPosition)
        {
            // No Op
        }

        public void HandleWindowDecorationStart(Rect windowPosition)
        {
            bool hasTitleBar = (windowPosition.y == 0 && showMode != ShowMode.Utility && !isPopup);

            if (!hasTitleBar)
                return;

            bool hasWindowButtons = Mathf.Abs(windowPosition.xMax - position.width) < 2;
            if (hasWindowButtons)
            {
                GUIStyle min = Styles.buttonMin;
                GUIStyle close = Styles.buttonClose;
                GUIStyle maxOrRestore = maximized ? Styles.buttonRestore : Styles.buttonMax;

                if (macEditor && (GUIView.focusedView == null || GUIView.focusedView.window != this))
                    close = min = maxOrRestore = Styles.buttonInactive;

                BeginTitleBarButtons(windowPosition);
                if (TitleBarButton(close))
                {
                    Close();
                    GUIUtility.ExitGUI();
                }

                if (macEditor && TitleBarButton(min))
                    Minimize();

                if (TitleBarButton(maxOrRestore))
                    ToggleMaximize();
            }

            DragTitleBar(new Rect(0, 0, position.width, kTitleHeight));
        }

        private void BeginTitleBarButtons(Rect windowPosition)
        {
            m_ButtonCount = 0;
            m_TitleBarWidth = windowPosition.width;
        }

        private bool TitleBarButton(GUIStyle style)
        {
            var buttonRect = new Rect(m_TitleBarWidth - Styles.borderSize - (buttonHorizontalSpace * ++m_ButtonCount), Styles.buttonTop, kButtonWidth, kButtonHeight);
            var guiView = rootView as GUIView;
            if (guiView == null)
            {
                var splitView = rootView as SplitView;
                if (splitView != null)
                    guiView = splitView.children.Length > 0 ? splitView.children[0] as GUIView : null;
            }
            if (guiView != null)
                guiView.MarkHotRegion(GUIClip.UnclipToWindow(buttonRect));

            return GUI.Button(buttonRect, GUIContent.none, style);
        }

        // Snapping windows
        static Vector2 s_LastDragMousePos;
        float startDragDpi;
        private void DragTitleBar(Rect titleBarRect)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event evt = Event.current;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.Repaint:
                    EditorGUIUtility.AddCursorRect(titleBarRect, MouseCursor.Arrow);
                    break;
                case EventType.MouseDown:
                    // If the mouse is inside the title bar rect, we say that we're the hot control
                    if (titleBarRect.Contains(evt.mousePosition) && GUIUtility.hotControl == 0 && evt.button == 0)
                    {
                        GUIUtility.hotControl = id;
                        Event.current.Use();
                        s_LastDragMousePos = evt.mousePosition;
                        startDragDpi = GUIUtility.pixelsPerPoint;
                        Unsupported.SetAllowCursorLock(false, Unsupported.DisallowCursorLockReasons.SizeMove);
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                        Unsupported.SetAllowCursorLock(true, Unsupported.DisallowCursorLockReasons.SizeMove);
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        Vector2 mousePos = evt.mousePosition;
                        if (startDragDpi != GUIUtility.pixelsPerPoint)
                        {
                            // We ignore this mouse event when changing screens in multi monitor setups with
                            // different dpi scalings as funky things might/will happen
                            startDragDpi = GUIUtility.pixelsPerPoint;
                            s_LastDragMousePos = mousePos;
                        }
                        else
                        {
                            Vector2 movement = mousePos - s_LastDragMousePos;

                            float minimumDelta = 1.0f / GUIUtility.pixelsPerPoint;

                            if (Mathf.Abs(movement.x) >= minimumDelta || Mathf.Abs(movement.y) >= minimumDelta)
                            {
                                Rect dragPosition = position;
                                dragPosition.x += movement.x;
                                dragPosition.y += movement.y;
                                position = dragPosition;

                                GUI.changed = true;
                            }
                        }
                    }
                    break;
            }
        }
    }
} //namespace
