// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AssetImporters;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Presets
{
    public abstract class PresetSelectorReceiver : ScriptableObject
    {
        public virtual void OnSelectionChanged(Preset selection) {}
        public virtual void OnSelectionClosed(Preset selection) {}
    }

    public class DefaultPresetSelectorReceiver : PresetSelectorReceiver
    {
        Object[] m_Targets;
        Preset[] m_InitialValues;

        internal void Init(Object[] targets)
        {
            m_Targets = targets;
            m_InitialValues = targets.Select(a => new Preset(a)).ToArray();
        }

        public override void OnSelectionChanged(Preset selection)
        {
            if (selection != null)
            {
                Undo.RecordObjects(m_Targets, "Apply Preset " + selection.name);
                foreach (var target in m_Targets)
                {
                    selection.ApplyTo(target);
                }
            }
            else
            {
                Undo.RecordObjects(m_Targets, "Cancel Preset");
                for (int i = 0; i < m_Targets.Length; i++)
                {
                    m_InitialValues[i].ApplyTo(m_Targets[i]);
                }
            }
        }

        public override void OnSelectionClosed(Preset selection)
        {
            OnSelectionChanged(selection);
            DestroyImmediate(this);
        }
    }

    class PresetListArea : ObjectListArea
    {
        public const int saveCurrentToInstanceID = int.MinValue;
        bool m_CanCreateNew;
        readonly LocalGroup.ExtraItem m_SaveCurrentToItem;

        public PresetListArea(ObjectListAreaState state, PresetSelector owner, bool canCreateNew)
            : base(state, owner, true)
        {
            m_CanCreateNew = canCreateNew;

            if (noneItem != null)
            {
                noneItem.m_Name = "Current";
                noneItem.m_Icon = EditorGUIUtility.LoadIcon("Preset.Current");
            }

            if (canCreateNew)
            {
                m_SaveCurrentToItem = new LocalGroup.ExtraItem()
                {
                    m_InstanceID = saveCurrentToInstanceID,
                    m_Name = "Save current to...",
                    m_Icon = EditorGUIUtility.LoadIcon("CreateAddNew")
                };
            }

            this.keyboardCallback += KeyboardCallback;
        }

        void KeyboardCallback()
        {
            if (m_CanCreateNew && Event.current.type == EventType.KeyDown)
            {
                m_LocalAssets.AssetReferenceAtIndex(numItemsDisplayed - 1, out InternalEditorUtility.AssetReference lastItem);
                switch (Event.current.keyCode)
                {
                    case KeyCode.DownArrow:
                        if (IsSelected(lastItem.instanceID))
                        {
                            SetSelection(new[] { saveCurrentToInstanceID }, false);
                            Event.current.Use();
                        }
                        else if (IsSelected(saveCurrentToInstanceID))
                        {
                            Event.current.Use();
                        }
                        break;
                    case KeyCode.UpArrow:
                        if (IsSelected(saveCurrentToInstanceID))
                        {
                            SetSelection(new[] { lastItem.instanceID }, false);
                            Event.current.Use();
                        }
                        break;
                }
            }
        }

        public override void OnGUI(Rect pos, int keyboardControlID)
        {
            if (m_CanCreateNew)
            {
                // The 'Save current to...' item will be "pinned" at the bottom of the area containing the list.
                // Reducing list area height by the height of one item.
                var itemHeight = m_LocalAssets.m_Grid.CalcRect(0, 0).height;

                var listPos = new Rect(pos);
                listPos.height = Math.Max(0, listPos.height - itemHeight);
                base.OnGUI(listPos, keyboardControlID);

                // Adjusting position of 'Save current to...' item
                var saveCurrentToItemRect = new Rect(pos.x, pos.y + listPos.height + 2, pos.width, itemHeight);
                m_LocalAssets.DrawItem(saveCurrentToItemRect, m_SaveCurrentToItem);
            }
            else
            {
                base.OnGUI(pos, keyboardControlID);
            }
        }

        LocalGroup.ExtraItem noneItem => m_LocalAssets?.NoneList != null && m_LocalAssets.NoneList.Length > 0 ? m_LocalAssets.NoneList[0] : null;
    }

    public class PresetSelector : EditorWindow
    {
        static class Style
        {
            public static GUIStyle bottomBarBg = "ProjectBrowserBottomBarBg";
            public static GUIStyle toolbarBack = "ObjectPickerToolbar";
            public static GUIContent presetIcon = EditorGUIUtility.IconContent("Preset.Context");
            public static GUIStyle selectedPathLabel = "Label";
        }

        // Filter
        string m_SearchField;
        IEnumerable<Preset> m_Presets;

        ObjectListAreaState m_ListAreaState;
        PresetListArea m_ListArea;

        // Layout
        const float kMinTopSize = 170;
        const float kMinWidth = 200;
        const float kPreviewMargin = 5;
        const float kPreviewExpandedAreaHeight = 75;
        const string k_PresetSelectorWidthEditorPref = "PresetSelectorWidth";
        const string k_PresetSelectorHeightEditorPref = "PresetSelectorHeight";
        float k_BottomBarHeight => EditorGUI.kWindowToolbarHeight;

        bool m_CanCreateNew;
        int m_ModalUndoGroup = -1;
        Object m_MainTarget;

        // get an existing ObjectSelector or create one
        static PresetSelector s_SharedPresetSelector = null;
        PresetSelectorReceiver m_EventObject;

        string m_SelectedPath;
        GUIContent m_SelectedPathContent = new GUIContent();

        bool canCreateNewPreset => m_CanCreateNew && m_ListArea != null && m_ListArea.IsSelected(PresetListArea.saveCurrentToInstanceID);

        internal static PresetSelector get
        {
            get
            {
                if (s_SharedPresetSelector == null)
                {
                    Object[] objs = Resources.FindObjectsOfTypeAll(typeof(PresetSelector));
                    if (objs != null && objs.Length > 0)
                        s_SharedPresetSelector = (PresetSelector)objs[0];
                    if (s_SharedPresetSelector == null)
                        s_SharedPresetSelector = CreateInstance<PresetSelector>();
                }
                return s_SharedPresetSelector;
            }
        }

        [EditorHeaderItem(typeof(Object), -1001)]
        public static bool DrawPresetButton(Rect rectangle, Object[] targets)
        {
            var target = targets[0];

            if (!target ||
                !new PresetType(target).IsValid() ||
                (target.hideFlags & HideFlags.NotEditable) != 0)
                return false;

            if (EditorGUI.DropdownButton(rectangle, Style.presetIcon , FocusType.Passive,
                EditorStyles.iconButton))
            {
                PresetContextMenu.CreateAndShow(targets);
            }
            return true;
        }

        public static void ShowSelector(Object[] targets, Preset currentSelection, bool createNewAllowed)
        {
            var eventHolder = CreateInstance<DefaultPresetSelectorReceiver>();
            eventHolder.Init(targets);
            ShowSelector(targets[0], currentSelection, createNewAllowed, eventHolder);
        }

        public static void ShowSelector(Object target, Preset currentSelection, bool createNewAllowed, PresetSelectorReceiver eventReceiver)
        {
            get.Init(target, currentSelection, createNewAllowed, eventReceiver);
        }

        public static void ShowSelector(PresetType presetType, Preset currentSelection, bool createNewAllowed, PresetSelectorReceiver eventReceiver)
        {
            get.Init(presetType, currentSelection, createNewAllowed, eventReceiver);
        }

        void Init(PresetType presetType, Preset currentSelection, bool createNewAllowed, PresetSelectorReceiver eventReceiver)
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            m_ModalUndoGroup = Undo.GetCurrentGroup();

            // Freeze to prevent flicker on OSX.
            // Screen will be updated again when calling
            // SetFreezeDisplay(false) further down.
            ContainerWindow.SetFreezeDisplay(true);

            // Set member variables
            m_SearchField = string.Empty;
            m_CanCreateNew = createNewAllowed;
            InitListArea();
            m_Presets = FindAllPresetsOfType(presetType);
            UpdateSearchResult(currentSelection != null ? currentSelection.GetInstanceID() : 0);

            m_EventObject = eventReceiver;

            ShowWithMode(ShowMode.AuxWindow);
            titleContent = EditorGUIUtility.TrTextContent("Select Preset");

            // Deal with window size
            Rect rect = m_Parent.window.position;
            rect.width = EditorPrefs.GetFloat(k_PresetSelectorWidthEditorPref, 200);
            rect.height = EditorPrefs.GetFloat(k_PresetSelectorHeightEditorPref, 200);
            position = rect;
            minSize = new Vector2(kMinWidth, kMinTopSize + kPreviewExpandedAreaHeight + 2 * kPreviewMargin);
            maxSize = new Vector2(500, 600);

            // Focus
            Focus();
            ContainerWindow.SetFreezeDisplay(false);

            // Add after unfreezing display because AuxWindowManager.cpp assumes that aux windows are added after we get 'got/lost'- focus calls.
            m_Parent.AddToAuxWindowList();


            SetSelectedPath(GetCurrentSelection());
        }

        void OnBeforeAssemblyReload()
        {
            Close();
        }

        void Init(Object target, Preset currentSelection, bool createNewAllowed, PresetSelectorReceiver eventReceiver)
        {
            m_MainTarget = target;
            Init(new PresetType(target), currentSelection, createNewAllowed, eventReceiver);
        }

        static IEnumerable<Preset> FindAllPresetsOfType(PresetType presetType)
        {
            return AssetDatabase.FindAssets("t:Preset")
                .Select(a => AssetDatabase.LoadAssetAtPath<Preset>(AssetDatabase.GUIDToAssetPath(a)))
                .Where(preset => preset.GetPresetType() == presetType);
        }

        void InitListArea()
        {
            if (m_ListAreaState == null)
                m_ListAreaState = new ObjectListAreaState(); // is serialized

            if (m_ListArea == null)
            {
                m_ListArea = new PresetListArea(m_ListAreaState, this, m_CanCreateNew);
                m_ListArea.allowDeselection = false;
                m_ListArea.allowDragging = false;
                m_ListArea.allowFocusRendering = false;
                m_ListArea.allowMultiSelect = false;
                m_ListArea.allowRenaming = false;
                m_ListArea.allowBuiltinResources = false;
                m_ListArea.repaintCallback += Repaint;
                m_ListArea.itemSelectedCallback += ListAreaItemSelectedCallback;
                m_ListArea.gridSize = m_ListArea.minGridSize;
            }
        }

        void UpdateSearchResult(int currentSelection)
        {
            var searchResult = m_Presets
                .Where(p => p.name.ToLower().Contains(m_SearchField.ToLower()))
                .Select(p => p.GetInstanceID())
                .ToArray();
            m_ListArea.ShowObjectsInList(searchResult);
            m_ListArea.InitSelection(new[] { currentSelection });
        }

        void ListAreaItemSelectedCallback(bool doubleClicked)
        {
            if (doubleClicked)
            {
                if (canCreateNewPreset)
                    CreatePreset(m_MainTarget);
                Close();
                GUIUtility.ExitGUI();
            }
            else
            {
                Preset selectedPreset = GetCurrentSelection();
                if (m_EventObject != null)
                {
                    m_EventObject.OnSelectionChanged(selectedPreset);
                }

                SetSelectedPath(selectedPreset);
            }
        }

        void SetSelectedPath(Preset selectedPreset)
        {
            m_SelectedPath = selectedPreset != null ? AssetDatabase.GetAssetPath(selectedPreset) : string.Empty;
            if (!string.IsNullOrEmpty(m_SelectedPath))
            {
                m_SelectedPathContent = new GUIContent(m_SelectedPath, AssetDatabase.GetCachedIcon(m_SelectedPath))
                {
                    tooltip = m_SelectedPath
                };
            }
            else
            {
                m_SelectedPathContent = new GUIContent();
            }
        }

        void OnGUI()
        {
            m_ListArea.HandleKeyboard(false);
            HandleKeyInput();
            EditorGUI.FocusTextInControl("ComponentSearch");
            DrawSearchField();

            var listPosition = EditorGUILayout.GetControlRect(true, GUILayout.ExpandHeight(true));
            int listKeyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);
            m_ListArea.OnGUI(new Rect(0, listPosition.y, position.width, listPosition.height), listKeyboardControlID);

            DrawBottomBar();
        }

        void DrawBottomBar()
        {
            using (new EditorGUILayout.HorizontalScope(Style.bottomBarBg, GUILayout.MinHeight(24f)))
            {
                Rect rect = new Rect(0, position.height - k_BottomBarHeight, position.width, k_BottomBarHeight);

                // File path
                EditorGUIUtility.SetIconSize(new Vector2(16, 16)); // If not set we see icons scaling down if text is being cropped
                const float k_Margin = 2;
                rect.width -= k_Margin * 2;
                rect.x += k_Margin;
                rect.height = k_BottomBarHeight;

                GUI.Label(rect, m_SelectedPathContent, Style.selectedPathLabel);
                EditorGUIUtility.SetIconSize(new Vector2(0, 0));

                GUILayout.FlexibleSpace();
            }
        }

        void DrawSearchField()
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                GUI.SetNextControlName("ComponentSearch");
                var rect = EditorGUILayout.GetControlRect(false, 24f, Style.toolbarBack);
                rect.height = 40f;
                GUI.Label(rect, GUIContent.none, Style.toolbarBack);
                m_SearchField = EditorGUI.SearchField(new Rect(5f, 5f, position.width - 10f, 15f), m_SearchField);
                if (change.changed)
                {
                    UpdateSearchResult(0);
                }
            }
        }

        void HandleKeyInput()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Escape:
                        if (m_SearchField == string.Empty)
                        {
                            Cancel();
                        }
                        break;
                    case KeyCode.KeypadEnter:
                    case KeyCode.Return:
                        if (canCreateNewPreset)
                            CreatePreset(m_MainTarget);
                        Close();
                        Event.current.Use();
                        GUIUtility.ExitGUI();
                        break;
                }
            }
        }

        void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            if (m_EventObject != null)
            {
                m_EventObject.OnSelectionClosed(GetCurrentSelection());
            }

            Undo.CollapseUndoOperations(m_ModalUndoGroup);

            EditorPrefs.SetFloat(k_PresetSelectorWidthEditorPref, position.width);
            EditorPrefs.SetFloat(k_PresetSelectorHeightEditorPref, position.height);
        }

        void OnDestroy()
        {
            if (m_ListArea != null)
                m_ListArea.OnDestroy();
        }

        Preset GetCurrentSelection()
        {
            Preset selection = null;
            if (m_ListArea != null)
            {
                var id = m_ListArea.GetSelection();
                if (id != null && id.Length > 0)
                    selection = EditorUtility.InstanceIDToObject(id[0]) as Preset;
            }
            return selection;
        }

        void Cancel()
        {
            Undo.RevertAllDownToGroup(m_ModalUndoGroup);

            // Clear selection so that object field doesn't grab it
            m_ListArea.InitSelection(new int[0]);

            Close();
            GUI.changed = true;
            GUIUtility.ExitGUI();
        }

        static bool ApplyImportSettingsBeforeSavingPreset(ref Preset preset, Object target)
        {
            // make sure modifications to importer get applied before creating preset.
            foreach (InspectorWindow i in InspectorWindow.GetAllInspectorWindows())
            {
                ActiveEditorTracker activeEditor = i.tracker;
                foreach (Editor e in activeEditor.activeEditors)
                {
                    var editor = e as AssetImporterEditor;
                    if (editor != null && editor.target == target && editor.HasModified())
                    {
                        if (EditorUtility.DisplayDialog("Unapplied import settings", "Apply settings before creating a new preset", "Apply", "Cancel"))
                        {
                            editor.ApplyAndImport();
                            // after reimporting, the target object has changed, so update the preset with the newly imported values.
                            preset.UpdateProperties(editor.target);
                            return false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        string CreatePresetDialog(ref Preset preset, Object target)
        {
            if (target is AssetImporter && ApplyImportSettingsBeforeSavingPreset(ref preset, target))
                return null;

            return EditorUtility.SaveFilePanelInProject("New Preset",
                !string.IsNullOrWhiteSpace(m_SearchField) ? m_SearchField : preset.GetTargetTypeName(),
                "preset",
                "",
                ProjectWindowUtil.GetActiveFolderPath());
        }

        void CreatePreset(Object target)
        {
            var preset = new Preset(target);
            var path = CreatePresetDialog(ref preset, target);
            if (!string.IsNullOrEmpty(path))
            {
                // If the asset already exist, we need to make sure we keep the same PPtr valid in memory.
                // To ensure that, we use CopySerialized on the Asset instance instead of erasing the asset with CreateAsset.
                var oldPreset = AssetDatabase.LoadAssetAtPath<Preset>(path);
                if (oldPreset != null)
                {
                    EditorUtility.CopySerialized(preset, oldPreset);

                    // replace name because it was erased by the CopySerialized
                    oldPreset.name = System.IO.Path.GetFileNameWithoutExtension(path);

                    AssetDatabase.SaveAssetIfDirty(oldPreset);

                    // If the preset is opened in any inspectors, rebuild them since the preset has been overwritten
                    var inspectors = InspectorWindow.GetInspectors().Where(iw => iw.GetInspectedObjects().Any(o => o == oldPreset));
                    foreach (InspectorWindow inspector in inspectors)
                        inspector.tracker.ForceRebuild();
                }
                else
                {
                    AssetDatabase.CreateAsset(preset, path);
                }
            }
            GUIUtility.ExitGUI();
        }
    }
}
