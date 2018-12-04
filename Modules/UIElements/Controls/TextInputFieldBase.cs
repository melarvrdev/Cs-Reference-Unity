// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    internal interface ITextInputField : IEventHandler, ITextElement
    {
        bool hasFocus { get; }

        bool doubleClickSelectsWord { get; }
        bool tripleClickSelectsLine { get; }

        void SyncTextEngine();
        bool AcceptCharacter(char c);
        string CullString(string s);
        void UpdateText(string value);
    }

    public abstract class TextInputBaseField<TValueType> : BaseField<TValueType>
    {
        static CustomStyleProperty<Color> s_SelectionColorProperty = new CustomStyleProperty<Color>("--unity-selection-color");
        static CustomStyleProperty<Color> s_CursorColorProperty = new CustomStyleProperty<Color>("--unity-cursor-color");

        public new class UxmlTraits : BaseField<TValueType>.UxmlTraits
        {
            UxmlIntAttributeDescription m_MaxLength = new UxmlIntAttributeDescription { name = "max-length", obsoleteNames = new[] { "maxLength" }, defaultValue = kMaxLengthNone };
            UxmlBoolAttributeDescription m_Password = new UxmlBoolAttributeDescription { name = "password" };
            UxmlStringAttributeDescription m_MaskCharacter = new UxmlStringAttributeDescription { name = "mask-character", obsoleteNames = new[] { "maskCharacter" }, defaultValue = "*" };
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var field = ((TextInputBaseField<TValueType>)ve);
                field.maxLength = m_MaxLength.GetValueFromBag(bag, cc);
                field.isPasswordField = m_Password.GetValueFromBag(bag, cc);
                string maskCharacter = m_MaskCharacter.GetValueFromBag(bag, cc);
                if (maskCharacter != null && maskCharacter.Length > 0)
                {
                    field.maskChar = maskCharacter[0];
                }
                ((ITextElement)field).text = m_Text.GetValueFromBag(bag, cc);
            }
        }

        TextInputBase m_TextInputBase;
        protected TextInputBase textInputBase => m_TextInputBase;

        internal const int kMaxLengthNone = -1;

        public new static readonly string ussClassName = "unity-base-text-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public static readonly string textInputUssName = "unity-text-input";

        public string text
        {
            get { return m_TextInputBase.text; }
            protected set
            {
                m_TextInputBase.text = value;
            }
        }

        // Password field (indirectly lossy behaviour when activated via multiline)
        public bool isPasswordField
        {
            get { return m_TextInputBase.isPasswordField; }
            set { m_TextInputBase.isPasswordField = value; }
        }

        public Color selectionColor => m_TextInputBase.selectionColor;
        public Color cursorColor => m_TextInputBase.cursorColor;


        public int cursorIndex => m_TextInputBase.cursorIndex;
        public int selectIndex => m_TextInputBase.selectIndex;
        public int maxLength
        {
            get { return m_TextInputBase.maxLength; }
            set { m_TextInputBase.maxLength = value; }
        }

        public bool doubleClickSelectsWord
        {
            get { return m_TextInputBase.doubleClickSelectsWord; }
            set { m_TextInputBase.doubleClickSelectsWord = value; }
        }
        public bool tripleClickSelectsLine
        {
            get { return m_TextInputBase.tripleClickSelectsLine; }
            set { m_TextInputBase.tripleClickSelectsLine = value; }
        }

        public bool isDelayed { get; set; }

        public char maskChar
        {
            get { return m_TextInputBase.maskChar; }
            set { m_TextInputBase.maskChar = value; }
        }

        /* internal for VisualTree tests */
        internal TextEditorEventHandler editorEventHandler => m_TextInputBase.editorEventHandler;

        /* internal for VisualTree tests */
        internal TextEditorEngine editorEngine  => m_TextInputBase.editorEngine;

        internal bool hasFocus => m_TextInputBase.hasFocus;

        public void SelectAll()
        {
            m_TextInputBase.SelectAll();
        }

        internal void SyncTextEngine()
        {
            m_TextInputBase.SyncTextEngine();
        }

        internal void DrawWithTextSelectionAndCursor(IStylePainterInternal painter, string newText)
        {
            m_TextInputBase.DrawWithTextSelectionAndCursor(painter, newText);
        }

        protected TextInputBaseField(int maxLength, char maskChar, TextInputBase textInputBase)
            : this(null, maxLength, maskChar, textInputBase) {}

        protected TextInputBaseField(string label, int maxLength, char maskChar, TextInputBase textInputBase)
            : base(label, textInputBase)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            m_TextInputBase = textInputBase;
            m_TextInputBase.maxLength = maxLength;
            m_TextInputBase.maskChar = maskChar;
        }

        protected abstract class TextInputBase : VisualElement, ITextInputField
        {
            public void SelectAll()
            {
                editorEngine?.SelectAll();
            }

            internal void SelectNone()
            {
                editorEngine?.SelectNone();
            }

            private void UpdateText(string value)
            {
                if (text != value)
                {
                    // Setting the VisualElement text here cause a repaint since it dirty the layout flag.
                    using (InputEvent evt = InputEvent.GetPooled(text, value))
                    {
                        evt.target = parent;
                        text = value;
                        parent?.SendEvent(evt);
                    }
                }
            }

            public int cursorIndex
            {
                get { return editorEngine.cursorIndex; }
            }

            public int selectIndex
            {
                get { return editorEngine.selectIndex; }
            }

            public int maxLength { get; set; }
            public char maskChar { get; set; }

            public virtual bool isPasswordField { get; set; }

            public bool doubleClickSelectsWord { get; set; }
            public bool tripleClickSelectsLine { get; set; }

            internal bool isDragging { get; set; }

            bool touchScreenTextField
            {
                get { return TouchScreenKeyboard.isSupported; }
            }


            Color m_SelectionColor = Color.clear;
            Color m_CursorColor = Color.grey;

            public Color selectionColor => m_SelectionColor;
            public Color cursorColor => m_CursorColor;


            internal bool hasFocus
            {
                get { return elementPanel != null && elementPanel.focusController.GetLeafFocusedElement() == this; }
            }

            /* internal for VisualTree tests */
            internal TextEditorEventHandler editorEventHandler { get; private set; }

            /* internal for VisualTree tests */
            internal TextEditorEngine editorEngine { get; private set; }


            private string m_Text;

            public string text
            {
                get { return m_Text; }
                set
                {
                    if (m_Text == value)
                        return;

                    m_Text = value;
                    editorEngine.text = value;
                    IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint);
                }
            }

            internal TextInputBase()
            {
                focusable = true;

                AddToClassList(inputUssClassName);
                m_Text = string.Empty;
                name = TextField.textInputUssName;

                requireMeasureFunction = true;

                editorEngine = new TextEditorEngine(OnDetectFocusChange, OnCursorIndexChange);

                if (touchScreenTextField)
                {
                    editorEventHandler = new TouchScreenTextEditorEventHandler(editorEngine, this);
                }
                else
                {
                    // TODO: Default values should come from GUI.skin.settings
                    doubleClickSelectsWord = true;
                    tripleClickSelectsLine = true;

                    editorEventHandler = new KeyboardTextEditorEventHandler(editorEngine, this);
                }

                // Make the editor style unique across all textfields
                editorEngine.style = new GUIStyle(editorEngine.style);

                RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            }

            DropdownMenuAction.Status CutCopyActionStatus(DropdownMenuAction a)
            {
                return (editorEngine.hasSelection && !isPasswordField) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
            }

            DropdownMenuAction.Status PasteActionStatus(DropdownMenuAction a)
            {
                return (editorEngine.CanPaste() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            void Cut(DropdownMenuAction a)
            {
                editorEngine.Cut();

                editorEngine.text = CullString(editorEngine.text);
                UpdateText(editorEngine.text);
            }

            void Copy(DropdownMenuAction a)
            {
                editorEngine.Copy();
            }

            void Paste(DropdownMenuAction a)
            {
                editorEngine.Paste();

                editorEngine.text = CullString(editorEngine.text);
                UpdateText(editorEngine.text);
            }

            private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
            {
                Color selectionValue = Color.clear;
                Color cursorValue = Color.clear;

                ICustomStyle customStyle = e.customStyle;
                if (customStyle.TryGetValue(s_SelectionColorProperty, out selectionValue))
                    m_SelectionColor = selectionValue;

                if (customStyle.TryGetValue(s_CursorColorProperty, out cursorValue))
                    m_CursorColor = cursorValue;

                ComputedStyle.WriteToGUIStyle(computedStyle, editorEngine.style);
            }

            internal virtual void SyncTextEngine()
            {
                editorEngine.text = CullString(text);

                editorEngine.SaveBackup();

                editorEngine.position = layout;

                editorEngine.DetectFocusChange();
            }

            internal string CullString(string s)
            {
                if (maxLength >= 0 && s != null && s.Length > maxLength)
                    return s.Substring(0, maxLength);
                return s;
            }

            internal override void DoRepaint(IStylePainter painter)
            {
                var stylePainter = (IStylePainterInternal)painter;

                // When this is used, we can get rid of the content.text trick and use mask char directly in the text to print
                if (touchScreenTextField)
                {
                    var touchScreenEditor = editorEventHandler as TouchScreenTextEditorEventHandler;
                    if (touchScreenEditor != null && editorEngine.keyboardOnScreen != null)
                    {
                        UpdateText(CullString(editorEngine.keyboardOnScreen.text));

                        if (editorEngine.keyboardOnScreen.status != TouchScreenKeyboard.Status.Visible)
                        {
                            editorEngine.keyboardOnScreen = null;
                            GUI.changed = true;
                        }
                    }

                    // if we use system keyboard we will have normal text returned (hiding symbols is done inside os)
                    // so before drawing make sure we hide them ourselves
                    string drawText = text;
                    if (touchScreenEditor != null && !string.IsNullOrEmpty(touchScreenEditor.secureText))
                        drawText = "".PadRight(touchScreenEditor.secureText.Length, maskChar);

                    text = drawText;
                }
                else
                {
                    if (!hasFocus)
                    {
                        stylePainter.DrawText(text);
                    }
                    else
                    {
                        DrawWithTextSelectionAndCursor(stylePainter, text);
                    }
                }
            }

            internal void DrawWithTextSelectionAndCursor(IStylePainterInternal painter, string newText)
            {
                var keyboardTextEditor = editorEventHandler as KeyboardTextEditorEventHandler;
                if (keyboardTextEditor == null)
                    return;

                keyboardTextEditor.PreDrawCursor(newText);

                int cursorIndex = editorEngine.cursorIndex;
                int selectIndex = editorEngine.selectIndex;
                Rect localPosition = editorEngine.localPosition;
                Vector2 scrollOffset = editorEngine.scrollOffset;


                float textScaling = TextNative.ComputeTextScaling(worldTransform);

                var textParams = TextStylePainterParameters.GetDefault(this, text);
                textParams.text = " ";
                textParams.wordWrapWidth = 0.0f;
                textParams.wordWrap = false;

                var textNativeSettings = textParams.GetTextNativeSettings(textScaling);
                float lineHeight = TextNative.ComputeTextHeight(textNativeSettings);
                float wordWrapWidth = editorEngine.multiline
                    ? contentRect.width
                    : 0.0f;
                Input.compositionCursorPos = editorEngine.graphicalCursorPos - scrollOffset +
                    new Vector2(localPosition.x, localPosition.y + lineHeight);

                Color drawCursorColor = cursorColor;

                int selectionEndIndex = string.IsNullOrEmpty(Input.compositionString)
                    ? selectIndex
                    : cursorIndex + Input.compositionString.Length;

                CursorPositionStylePainterParameters cursorParams;

                // Draw highlighted section, if any
                if ((cursorIndex != selectionEndIndex) && !isDragging)
                {
                    var painterParams = RectStylePainterParameters.GetDefault(this);
                    painterParams.color = selectionColor;
                    painterParams.border.SetWidth(0.0f);
                    painterParams.border.SetRadius(0.0f);

                    int min = cursorIndex < selectionEndIndex ? cursorIndex : selectionEndIndex;
                    int max = cursorIndex > selectionEndIndex ? cursorIndex : selectionEndIndex;

                    cursorParams = CursorPositionStylePainterParameters.GetDefault(this, text);
                    cursorParams.text = editorEngine.text;
                    cursorParams.wordWrapWidth = wordWrapWidth;
                    cursorParams.cursorIndex = min;

                    textNativeSettings = cursorParams.GetTextNativeSettings(textScaling);
                    Vector2 minPos = TextNative.GetCursorPosition(textNativeSettings, cursorParams.rect, min);
                    Vector2 maxPos = TextNative.GetCursorPosition(textNativeSettings, cursorParams.rect, max);

                    minPos -= scrollOffset;
                    maxPos -= scrollOffset;

                    if (Mathf.Approximately(minPos.y, maxPos.y))
                    {
                        painterParams.rect = new Rect(minPos.x, minPos.y, maxPos.x - minPos.x, lineHeight);
                        painter.DrawRect(painterParams);
                    }
                    else
                    {
                        // Draw first line
                        painterParams.rect = new Rect(minPos.x, minPos.y, contentRect.xMax - minPos.x, lineHeight);
                        painter.DrawRect(painterParams);

                        var inbetweenHeight = (maxPos.y - minPos.y) - lineHeight;
                        if (inbetweenHeight > 0f)
                        {
                            // Draw all lines in-between
                            painterParams.rect = new Rect(contentRect.x, minPos.y + lineHeight, wordWrapWidth, inbetweenHeight);
                            painter.DrawRect(painterParams);
                        }

                        // Draw last line if not empty
                        if (maxPos.x != contentRect.x)
                        {
                            painterParams.rect = new Rect(contentRect.x, maxPos.y, maxPos.x, lineHeight);
                            painter.DrawRect(painterParams);
                        }
                    }
                }

                // Draw the text with the scroll offset
                if (!string.IsNullOrEmpty(editorEngine.text) && contentRect.width > 0.0f && contentRect.height > 0.0f)
                {
                    textParams = TextStylePainterParameters.GetDefault(this, text);
                    textParams.rect = new Rect(contentRect.x - scrollOffset.x, contentRect.y - scrollOffset.y, contentRect.width + scrollOffset.x, contentRect.height + scrollOffset.y);
                    textParams.text = editorEngine.text;
                    painter.DrawText(textParams);
                }

                // Draw the cursor
                if (!isDragging)
                {
                    if (cursorIndex == selectionEndIndex && computedStyle.unityFont.value != null)
                    {
                        cursorParams = CursorPositionStylePainterParameters.GetDefault(this, text);
                        cursorParams.text = editorEngine.text;
                        cursorParams.wordWrapWidth = wordWrapWidth;
                        cursorParams.cursorIndex = cursorIndex;

                        textNativeSettings = cursorParams.GetTextNativeSettings(textScaling);
                        Vector2 cursorPosition = TextNative.GetCursorPosition(textNativeSettings, cursorParams.rect, cursorParams.cursorIndex);
                        cursorPosition -= scrollOffset;
                        var painterParams = new RectStylePainterParameters
                        {
                            rect = new Rect(cursorPosition.x, cursorPosition.y, 1f, lineHeight),
                            color = drawCursorColor
                        };
                        painter.DrawRect(painterParams);
                    }

                    // Draw alternate cursor, if any
                    if (editorEngine.altCursorPosition != -1)
                    {
                        cursorParams = CursorPositionStylePainterParameters.GetDefault(this, text);
                        cursorParams.text = editorEngine.text.Substring(0, editorEngine.altCursorPosition);
                        cursorParams.wordWrapWidth = wordWrapWidth;
                        cursorParams.cursorIndex = editorEngine.altCursorPosition;

                        textNativeSettings = cursorParams.GetTextNativeSettings(textScaling);
                        Vector2 altCursorPosition = TextNative.GetCursorPosition(textNativeSettings, cursorParams.rect, cursorParams.cursorIndex);
                        altCursorPosition -= scrollOffset;

                        var painterParams = new RectStylePainterParameters
                        {
                            rect = new Rect(altCursorPosition.x, altCursorPosition.y, 1f, lineHeight),
                            color = drawCursorColor
                        };
                        painter.DrawRect(painterParams);
                    }
                }

                keyboardTextEditor.PostDrawCursor();
            }

            internal virtual bool AcceptCharacter(char c)
            {
                return true;
            }

            protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
            {
                if (evt?.target is TextInputBase)
                {
                    evt.menu.AppendAction("Cut", Cut, CutCopyActionStatus);
                    evt.menu.AppendAction("Copy", Copy, CutCopyActionStatus);
                    evt.menu.AppendAction("Paste", Paste, PasteActionStatus);
                }
            }

            private void OnDetectFocusChange()
            {
                if (editorEngine.m_HasFocus && !hasFocus)
                {
                    editorEngine.OnFocus();
                }

                if (!editorEngine.m_HasFocus && hasFocus)
                    editorEngine.OnLostFocus();
            }

            private void OnCursorIndexChange()
            {
                IncrementVersion(VersionChangeType.Repaint);
            }

            protected internal override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight, MeasureMode heightMode)
            {
                return TextElement.MeasureVisualElementTextSize(this, m_Text, desiredWidth, widthMode, desiredHeight, heightMode);
            }

            protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
            {
                base.ExecuteDefaultActionAtTarget(evt);

                if (elementPanel != null && elementPanel.contextualMenuManager != null)
                {
                    elementPanel.contextualMenuManager.DisplayMenuIfEventMatches(evt, this);
                }

                if (evt?.eventTypeId == ContextualMenuPopulateEvent.TypeId())
                {
                    ContextualMenuPopulateEvent e = evt as ContextualMenuPopulateEvent;
                    int count = e.menu.MenuItems().Count;
                    BuildContextualMenu(e);

                    if (count > 0 && e.menu.MenuItems().Count > count)
                    {
                        e.menu.InsertSeparator(null, count);
                    }
                }

                editorEventHandler.ExecuteDefaultActionAtTarget(evt);
            }

            protected internal override void ExecuteDefaultAction(EventBase evt)
            {
                base.ExecuteDefaultAction(evt);

                editorEventHandler.ExecuteDefaultAction(evt);
            }

            bool ITextInputField.hasFocus => hasFocus;

            void ITextInputField.SyncTextEngine()
            {
                SyncTextEngine();
            }

            bool ITextInputField.AcceptCharacter(char c)
            {
                return AcceptCharacter(c);
            }

            string ITextInputField.CullString(string s)
            {
                return CullString(s);
            }

            void ITextInputField.UpdateText(string value)
            {
                UpdateText(value);
            }
        }
    }
}
