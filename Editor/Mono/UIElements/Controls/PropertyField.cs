// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// A SerializedProperty wrapper VisualElement that, on Bind(), will generate the correct field elements with the correct bindingPaths.
    /// </summary>
    public class PropertyField : VisualElement, IBindable
    {
        private static readonly Regex s_MatchPPtrTypeName = new Regex(@"PPtr\<(\w+)\>");
        internal static readonly string foldoutTitleBoundLabelProperty = "unity-foldout-bound-title";
        internal static readonly string decoratorDrawersContainerClassName = "unity-decorator-drawers-container";

        /// <summary>
        /// Instantiates a <see cref="PropertyField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<PropertyField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="PropertyField"/>.
        /// </summary>
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_PropertyPath;
            UxmlStringAttributeDescription m_Label;

            /// <summary>
            /// Constructor.
            /// </summary>
            public UxmlTraits()
            {
                m_PropertyPath = new UxmlStringAttributeDescription { name = "binding-path" };
                m_Label = new UxmlStringAttributeDescription { name = "label", defaultValue = null };
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var field = ve as PropertyField;
                if (field == null)
                    return;

                field.label = m_Label.GetValueFromBag(bag, cc);

                string propPath = m_PropertyPath.GetValueFromBag(bag, cc);
                if (!string.IsNullOrEmpty(propPath))
                    field.bindingPath = propPath;
            }
        }

        /// <summary>
        /// Binding object that will be updated.
        /// </summary>
        public IBinding binding { get; set; }

        /// <summary>
        /// Path of the target property to be bound.
        /// </summary>
        public string bindingPath { get; set; }

        /// <summary>
        /// Optionally overwrite the label of the generate property field. If no label is provided the string will be taken from the SerializedProperty.
        /// </summary>
        public string label { get; set; }

        private SerializedProperty m_SerializedProperty;
        private PropertyField m_ParentPropertyField;
        private int m_FoldoutDepth;
        private VisualElement m_InspectorElement;
        private VisualElement m_ContextWidthElement;

        private int m_DrawNestingLevel;
        private PropertyField m_DrawParentProperty;
        private VisualElement m_DecoratorDrawersContainer;

        SerializedProperty serializedProperty => m_SerializedProperty;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-property-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public static readonly string inputUssClassName = ussClassName + "__input";
        /// <summary>
        /// USS class name of property fields in inspector elements
        /// </summary>
        public static readonly string inspectorElementUssClassName = ussClassName + "__inspector-property";

        /// <summary>
        /// PropertyField constructor.
        /// </summary>
        /// <remarks>
        /// You will still have to call Bind() on the PropertyField afterwards.
        /// </remarks>
        public PropertyField() : this(null, null) {}

        /// <summary>
        /// PropertyField constructor.
        /// </summary>
        /// <param name="property">Providing a SerializedProperty in the construct just sets the bindingPath. You will still have to call Bind() on the PropertyField afterwards.</param>
        /// <remarks>
        /// You will still have to call Bind() on the PropertyField afterwards.
        /// </remarks>
        public PropertyField(SerializedProperty property) : this(property, null) {}

        /// <summary>
        /// PropertyField constructor.
        /// </summary>
        /// <param name="property">Providing a SerializedProperty in the construct just sets the bindingPath. You will still have to call Bind() on the PropertyField afterwards.</param>
        /// <param name="label">Optionally overwrite the property label.</param>
        /// <remarks>
        /// You will still have to call Bind() on the PropertyField afterwards.
        /// </remarks>
        public PropertyField(SerializedProperty property, string label)
        {
            AddToClassList(ussClassName);
            this.label = label;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);

            if (property == null)
                return;

            bindingPath = property.propertyPath;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null)
                return;

            m_FoldoutDepth = this.GetFoldoutDepth();

            var currentElement = parent;
            while (currentElement != null)
            {
                if (currentElement.ClassListContains(InspectorElement.ussClassName))
                {
                    AddToClassList(inspectorElementUssClassName);
                    m_InspectorElement = currentElement;
                }

                if (currentElement.ClassListContains(PropertyEditor.s_MainContainerClassName))
                {
                    m_ContextWidthElement = currentElement;
                    break;
                }

                currentElement = currentElement.parent;
            }
        }

        private void OnDetachFromPanelEvent(DetachFromPanelEvent evt)
        {
            RemoveFromClassList(inspectorElementUssClassName);
        }

        [EventInterest(typeof(SerializedPropertyBindEvent))]
        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            var bindEvent = evt as SerializedPropertyBindEvent;
            if (bindEvent == null)
                return;

            Reset(bindEvent);

            // Don't allow the binding of `this` to continue because `this` is not
            // the actually bound field, it is just a container.
            evt.StopPropagation();
        }

        void Reset(SerializedProperty property)
        {
            m_SerializedProperty = property;

            if (m_SerializedProperty != null && m_SerializedProperty.isValid)
            {
                // if we already have a serialized property, determine if the property field can be reused without reset
                // this is only supported for non propertydrawer types
                if (m_ChildField != null && m_SerializedProperty.propertyType == property.propertyType)
                {
                    var propertyHandler = ScriptAttributeUtility.GetHandler(m_SerializedProperty);
                    ResetDecoratorDrawers(propertyHandler);

                    var newField = CreateOrUpdateFieldFromProperty(property, m_ChildField);
                    // there was an issue where we weren't able to swap the bindings on the original field
                    if (newField != m_ChildField)
                    {
                        m_ChildField.Unbind();
                        var childIndex = IndexOf(m_ChildField);
                        if (childIndex >= 0)
                        {
                            m_ChildField.RemoveFromHierarchy();
                            m_ChildField = newField;
                            hierarchy.Insert(childIndex, m_ChildField);
                        }
                    }

                    return;
                }
            }

            Clear();
            m_ChildField?.Unbind();
            m_ChildField = null;
            m_DecoratorDrawersContainer = null;

            if (property == null)
                return;

            ComputeNestingLevel();

            VisualElement customPropertyGUI = null;

            // Case 1292133: set proper nesting level before calling CreatePropertyGUI
            var handler = ScriptAttributeUtility.GetHandler(m_SerializedProperty);

            using (var nestingContext = handler.ApplyNestingContext(m_DrawNestingLevel))
            {
                if (handler.hasPropertyDrawer)
                {
                    customPropertyGUI = handler.propertyDrawer.CreatePropertyGUI(m_SerializedProperty);

                    if (customPropertyGUI == null)
                    {
                        customPropertyGUI = CreatePropertyIMGUIContainer();
                        m_imguiChildField = customPropertyGUI;
                    }
                    else
                    {
                        RegisterPropertyChangesOnCustomDrawerElement(customPropertyGUI);
                    }
                }
                else
                {
                    customPropertyGUI = CreateOrUpdateFieldFromProperty(m_SerializedProperty);
                    m_ChildField = customPropertyGUI;
                }
            }

            ResetDecoratorDrawers(handler);

            if (customPropertyGUI != null)
            {
                PropagateNestingLevel(customPropertyGUI);
                hierarchy.Add(customPropertyGUI);
            }

            if (m_SerializedProperty.propertyType == SerializedPropertyType.ManagedReference)
                BindingExtensions.TrackPropertyValue(this, m_SerializedProperty, Reset);
        }

        private void ResetDecoratorDrawers(PropertyHandler handler)
        {
             var decorators = handler.decoratorDrawers;

             if (decorators == null || decorators.Count == 0)
             {
                 if (m_DecoratorDrawersContainer != null)
                 {
                     Remove(m_DecoratorDrawersContainer);
                     m_DecoratorDrawersContainer = null;
                 }

                 return;
             }

             if (m_DecoratorDrawersContainer == null)
             {
                 m_DecoratorDrawersContainer = new VisualElement();
                 m_DecoratorDrawersContainer.AddToClassList(decoratorDrawersContainerClassName);
                 Insert(0, m_DecoratorDrawersContainer);
             }
             else
             {
                 m_DecoratorDrawersContainer.Clear();
             }

             foreach (var decorator in decorators)
             {
                 var ve = decorator.CreatePropertyGUI();

                 if (ve == null)
                 {
                     ve = new IMGUIContainer(() =>
                     {
                         var decoratorRect = new Rect();
                         decoratorRect.height = decorator.GetHeight();
                         decoratorRect.width = resolvedStyle.width;
                         decorator.OnGUI(decoratorRect);
                     });
                     ve.style.height = decorator.GetHeight();
                 }

                 m_DecoratorDrawersContainer.Add(ve);
             }
        }

        private void Reset(SerializedPropertyBindEvent evt)
        {
            Reset(evt.bindProperty);
        }

        private VisualElement CreatePropertyIMGUIContainer()
        {
            GUIContent customLabel = string.IsNullOrEmpty(label) ? null : new GUIContent(label);

            var imguiContainer = new IMGUIContainer(() =>
            {
                var originalWideMode = InspectorElement.SetWideModeForWidth(this);
                var oldLabelWidth = EditorGUIUtility.labelWidth;

                try
                {
                    if (!serializedProperty.isValid)
                        return;

                    EditorGUI.BeginChangeCheck();
                    serializedProperty.serializedObject.Update();

                    if (classList.Contains(inspectorElementUssClassName))
                    {
                        var spacing = 0f;

                        if (m_imguiChildField != null)
                        {
                            spacing = m_imguiChildField.worldBound.x - m_InspectorElement.worldBound.x - m_InspectorElement.resolvedStyle.paddingLeft;
                        }

                        var imguiSpacing = EditorGUI.kLabelWidthMargin - EditorGUI.kLabelWidthPadding;
                        var contextWidthElement = m_ContextWidthElement ?? m_InspectorElement;
                        var contextWidth = contextWidthElement.resolvedStyle.width;
                        var labelWidth = (contextWidth * EditorGUI.kLabelWidthRatio - imguiSpacing - spacing);
                        var minWidth = EditorGUI.kMinLabelWidth + EditorGUI.kLabelWidthPadding;
                        var minLabelWidth = Mathf.Max(minWidth - spacing, 0f);

                        EditorGUIUtility.labelWidth = Mathf.Max(labelWidth, minLabelWidth);
                    }
                    else
                    {
                        if (m_FoldoutDepth > 0)
                            EditorGUI.indentLevel += m_FoldoutDepth;
                    }

                    // Wait at last minute to call GetHandler, sometimes the handler cache is cleared between calls.
                    var handler = ScriptAttributeUtility.GetHandler(serializedProperty);
                    using (var nestingContext = handler.ApplyNestingContext(m_DrawNestingLevel))
                    {
                        if (label == null)
                        {
                            EditorGUILayout.PropertyField(serializedProperty, true);
                        }
                        else if (label == string.Empty)
                        {
                            EditorGUILayout.PropertyField(serializedProperty, GUIContent.none, true);
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(serializedProperty, new GUIContent(label), true);
                        }
                    }

                    if (!classList.Contains(inspectorElementUssClassName))
                    {
                        if (m_FoldoutDepth > 0)
                            EditorGUI.indentLevel -= m_FoldoutDepth;
                    }

                    serializedProperty.serializedObject.ApplyModifiedProperties();
                    if (EditorGUI.EndChangeCheck())
                    {
                        DispatchPropertyChangedEvent();
                    }
                }
                finally
                {
                    EditorGUIUtility.wideMode = originalWideMode;

                    if (classList.Contains(inspectorElementUssClassName))
                    {
                        EditorGUIUtility.labelWidth = oldLabelWidth;
                    }
                }
            });

            return imguiContainer;
        }

        private void ComputeNestingLevel()
        {
            m_DrawNestingLevel = 0;
            for (var ve = m_DrawParentProperty; ve != null; ve = ve.m_DrawParentProperty)
            {
                if (ve.m_SerializedProperty == m_SerializedProperty ||
                    ScriptAttributeUtility.CanUseSameHandler(ve.m_SerializedProperty, m_SerializedProperty))
                {
                    m_DrawNestingLevel = ve.m_DrawNestingLevel + 1;
                    break;
                }
            }
        }

        private void PropagateNestingLevel(VisualElement customPropertyGUI)
        {
            var p = customPropertyGUI as PropertyField;
            if (p != null)
            {
                p.m_DrawParentProperty = this;
            }

            int childCount = customPropertyGUI.hierarchy.childCount;
            for (var i = 0; i < childCount; i++)
            {
                PropagateNestingLevel(customPropertyGUI.hierarchy[i]);
            }
        }

        private void Rebind()
        {
            if (m_SerializedProperty == null)
                return;

            var serializedObject = m_SerializedProperty.serializedObject;
            this.Unbind();
            this.Bind(serializedObject);
        }

        private void UpdateArrayFoldout(
            ChangeEvent<int> changeEvent,
            PropertyField targetPropertyField,
            PropertyField parentPropertyField)
        {
            var propertyIntValue = targetPropertyField.m_SerializedProperty.intValue;

            if (targetPropertyField == null || targetPropertyField.m_SerializedProperty == null || parentPropertyField == null && targetPropertyField.m_SerializedProperty.intValue == changeEvent.newValue)
                return;

            var parentSerializedObject = parentPropertyField?.m_SerializedProperty?.serializedObject;

            if (propertyIntValue != changeEvent.newValue)
            {
                var serialiedObject = targetPropertyField.m_SerializedProperty.serializedObject;
                serialiedObject.UpdateIfRequiredOrScript();
                targetPropertyField.m_SerializedProperty.intValue = changeEvent.newValue;
                serialiedObject.ApplyModifiedProperties();
            }

            if (parentPropertyField != null)
            {
                parentPropertyField.RefreshChildrenProperties(parentPropertyField.m_SerializedProperty.Copy(), true);
                return;
            }
        }

        private List<PropertyField> m_ChildrenProperties;

        /// <summary>
        /// stores the child field if there is only a single child. Used for updating bindings when this field is rebound.
        /// </summary>
        private VisualElement m_ChildField;

        private VisualElement m_imguiChildField;
        private VisualElement m_ChildrenContainer;

        void TrimChildrenContainerSize(int targetSize)
        {
            if (m_ChildrenProperties != null)
            {
                while (m_ChildrenProperties.Count > targetSize)
                {
                    var c = m_ChildrenProperties.Count - 1;
                    var pf = m_ChildrenProperties[c];
                    pf.Unbind();
                    pf.RemoveFromHierarchy();
                    m_ChildrenProperties.RemoveAt(c);
                }
            }
        }

        void RefreshChildrenProperties(SerializedProperty property, bool bindNewFields)
        {
            if (m_ChildrenContainer == null)
            {
                return;
            }

            var endProperty = property.GetEndProperty();
            int propCount = 0;

            if (m_ChildrenProperties == null)
            {
                m_ChildrenProperties = new List<PropertyField>();
            }

            property.NextVisible(true); // Expand the first child.
            do
            {
                if (SerializedProperty.EqualContents(property, endProperty))
                    break;

                PropertyField field = null;
                var propPath = property.propertyPath;
                if (propCount < m_ChildrenProperties.Count)
                {
                    field = m_ChildrenProperties[propCount];
                    if (field.bindingPath != propPath)
                    {
                        field.bindingPath = property.propertyPath;
                        field.Bind(property.serializedObject);
                    }
                }
                else
                {
                    field = new PropertyField(property);
                    field.m_ParentPropertyField = this;
                    m_ChildrenProperties.Add(field);
                    field.bindingPath = property.propertyPath;

                    if (bindNewFields)
                        field.Bind(property.serializedObject);
                }
                field.name = "unity-property-field-" + property.propertyPath;

                // Not yet knowing what type of field we are dealing with, we defer the showMixedValue value setting
                // to be automatically done via the next Reset call
                m_ChildrenContainer.Add(field);
                propCount++;
            }
            while (property.NextVisible(false)); // Never expand children.

            TrimChildrenContainerSize(propCount);
        }

        private VisualElement CreateFoldout(SerializedProperty property, object originalField = null)
        {
            property = property.Copy();
            var foldout = originalField != null && originalField is Foldout ? originalField as Foldout : new Foldout();
            bool hasCustomLabel = !string.IsNullOrEmpty(label);
            foldout.text = hasCustomLabel ? label : property.localizedDisplayName;
            foldout.value = property.isExpanded;
            foldout.bindingPath = property.propertyPath;
            foldout.name = "unity-foldout-" + property.propertyPath;

            // Make PropertyField foldout react even when disabled, like EditorGUILayout.Foldout.
            var foldoutToggle = foldout.Q<Toggle>(className: Foldout.toggleUssClassName);
            foldoutToggle.m_Clickable.acceptClicksIfDisabled = true;

            // Get Foldout label.
            var foldoutLabel = foldoutToggle.Q<Label>(className: Toggle.textUssClassName);
            if (hasCustomLabel)
            {
                foldoutLabel.text = foldout.text;
            }
            else
            {
                foldoutLabel.bindingPath = property.propertyPath;
                foldoutLabel.SetProperty(foldoutTitleBoundLabelProperty, true);
            }

            m_ChildrenContainer = foldout;

            RefreshChildrenProperties(property, false);

            return foldout;
        }

        private VisualElement ConfigureField<TField, TValue>(TField field, SerializedProperty property, Func<TField> factory)
            where TField : BaseField<TValue>
        {
            if (field == null)
            {
                field = factory();
            }
            var fieldLabel = label ?? property.localizedDisplayName;
            field.bindingPath = property.propertyPath;
            field.SetProperty(BaseField<TValue>.serializedPropertyCopyName, property.Copy());
            field.name = "unity-input-" + property.propertyPath;
            field.label = fieldLabel;

            field.labelElement.AddToClassList(labelUssClassName);
            field.visualInput.AddToClassList(inputUssClassName);
            field.AddToClassList(BaseField<TValue>.alignedFieldUssClassName);

            var nestedFields = field.visualInput.Query<VisualElement>(
                classes: new []{BaseField<TValue>.ussClassName, BaseCompositeField<int, IntegerField, int>.ussClassName} );

            nestedFields.ForEach(x =>
            {
                x.AddToClassList(BaseField<TValue>.alignedFieldUssClassName);
            });

            field.RegisterValueChangedCallback((evt) =>
            {
                if (evt.target == field)
                {
                    DispatchPropertyChangedEvent();
                }
            });

            return field;
        }

        VisualElement ConfigureListView(ListView listView, SerializedProperty property, Func<ListView> factory)
        {
            listView ??= factory();
            var propertyCopy = property.Copy();
            listView.reorderMode = ListViewReorderMode.Animated;
            listView.reorderable = PropertyHandler.IsArrayReorderable(property);
            listView.showBorder = true;
            listView.showAddRemoveFooter = true;
            listView.showBoundCollectionSize = true;
            listView.showFoldoutHeader = true;
            listView.headerTitle = string.IsNullOrEmpty(label) ? propertyCopy.localizedDisplayName : label;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.userData = propertyCopy;
            listView.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
            listView.bindingPath = property.propertyPath;
            listView.viewDataKey = property.propertyPath;
            listView.name = "unity-list-" + property.propertyPath;
            listView.headerFoldout.viewDataKey = property.propertyPath;
            listView.Bind(property.serializedObject);
            return listView;
        }

        private VisualElement CreateOrUpdateFieldFromProperty(SerializedProperty property, object originalField = null)
        {
            var propertyType = property.propertyType;

            if (EditorGUI.HasVisibleChildFields(property, true) && !property.isArray)
                return CreateFoldout(property, originalField);

            TrimChildrenContainerSize(0);
            m_ChildrenContainer = null;

            switch (propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (property.type == "long")
                        return ConfigureField<LongField, long>(originalField as LongField, property, () => new LongField());
                    return ConfigureField<IntegerField, int>(originalField as IntegerField, property, () => new IntegerField());

                case SerializedPropertyType.Boolean:
                    return ConfigureField<Toggle, bool>(originalField as Toggle, property, () => new Toggle());

                case SerializedPropertyType.Float:
                    if (property.type == "double")
                        return ConfigureField<DoubleField, double>(originalField as DoubleField, property, () => new DoubleField());
                    return ConfigureField<FloatField, float>(originalField as FloatField, property, () => new FloatField());

                case SerializedPropertyType.String:
                    return ConfigureField<TextField, string>(originalField as TextField, property, () => new TextField());

                case SerializedPropertyType.Color:
                    return ConfigureField<ColorField, Color>(originalField as ColorField, property, () => new ColorField());

                case SerializedPropertyType.ObjectReference:
                {
                    ObjectField field = originalField as ObjectField;
                    if (field == null)
                        field = new ObjectField();

                    Type requiredType = null;

                    // Checking if the target ExtendsANativeType() avoids a native error when
                    // getting the type about: "type is not a supported pptr value"
                    var target = property.serializedObject.targetObject;
                    if (NativeClassExtensionUtilities.ExtendsANativeType(target))
                        ScriptAttributeUtility.GetFieldInfoFromProperty(property, out requiredType);

                    // case 1423715: built-in types that are defined on the native side will not reference a C# type, but rather a PPtr<Type>, so in the
                    // case where we can't extract the C# type from the FieldInfo, we need to extract it from the string representation.
                    if (requiredType == null)
                    {
                        var targetTypeName = s_MatchPPtrTypeName.Match(property.type).Groups[1].Value;
                        foreach (var objectTypes in TypeCache.GetTypesDerivedFrom<UnityEngine.Object>())
                        {
                            if (!objectTypes.Name.Equals(targetTypeName, StringComparison.OrdinalIgnoreCase))
                                continue;
                            requiredType = objectTypes;
                            break;
                        }
                    }

                    field.SetProperty(ObjectField.serializedPropertyKey, property);
                    field.SetObjectTypeWithoutDisplayUpdate(requiredType);
                    field.UpdateDisplay();

                    return ConfigureField<ObjectField, UnityEngine.Object>(field, property, () => new ObjectField());
                }
                case SerializedPropertyType.LayerMask:
                    return ConfigureField<LayerMaskField, int>(originalField as LayerMaskField, property, () => new LayerMaskField());

                case SerializedPropertyType.Enum:
                {
                    ScriptAttributeUtility.GetFieldInfoFromProperty(property, out var enumType);

                    if (enumType != null && enumType.IsDefined(typeof(FlagsAttribute), false))
                    {
                        // We should use property.longValue instead of intValue once long enum types are supported.
                        var enumData = EnumDataUtility.GetCachedEnumData(enumType);
                        if (originalField != null && originalField is EnumFlagsField enumFlagsField)
                        {
                            enumFlagsField.choices = enumData.displayNames.ToList();
                            enumFlagsField.value = (Enum)Enum.ToObject(enumType, property.intValue);
                        }
                        return ConfigureField<EnumFlagsField, Enum>(originalField as EnumFlagsField, property,
                            () => new EnumFlagsField
                            {
                                choices = enumData.displayNames.ToList(),
                                value = (Enum)Enum.ToObject(enumType, property.intValue)
                            });
                    }
                    else
                    {
                        // We need to use property.enumDisplayNames[property.enumValueIndex] as the source of truth for
                        // the popup index, because enumData.displayNames and property.enumDisplayNames might not be
                        // in the same order.
                        var enumData = enumType != null ? (EnumData?)EnumDataUtility.GetCachedEnumData(enumType) : null;
                        var propertyDisplayNames = EditorGUI.EnumNamesCache.GetEnumDisplayNames(property);
                        var popupEntries = (enumData?.displayNames ?? propertyDisplayNames).ToList();
                        int propertyFieldIndex = (property.enumValueIndex < 0 || property.enumValueIndex >= propertyDisplayNames.Length
                            ? PopupField<string>.kPopupFieldDefaultIndex : (enumData != null
                                ? Array.IndexOf(enumData.Value.displayNames, propertyDisplayNames[property.enumValueIndex])
                                : property.enumValueIndex));
                        if (originalField != null && originalField is PopupField<string> popupField)
                        {
                            popupField.choices = popupEntries;
                            popupField.index = propertyFieldIndex;
                        }
                        return ConfigureField<PopupField<string>, string>(originalField as PopupField<string>, property,
                            () => new PopupField<string>(popupEntries, property.enumValueIndex)
                            {
                                index = propertyFieldIndex
                            });
                    }
                }
                case SerializedPropertyType.Vector2:
                    return ConfigureField<Vector2Field, Vector2>(originalField as Vector2Field, property, () => new Vector2Field());

                case SerializedPropertyType.Vector3:
                    return ConfigureField<Vector3Field, Vector3>(originalField as Vector3Field, property, () => new Vector3Field());

                case SerializedPropertyType.Vector4:
                    return ConfigureField<Vector4Field, Vector4>(originalField as Vector4Field, property, () => new Vector4Field());

                case SerializedPropertyType.Rect:
                    return ConfigureField<RectField, Rect>(originalField as RectField, property, () => new RectField());

                case SerializedPropertyType.ArraySize:
                {
                    IntegerField field = originalField as IntegerField;
                    if (field == null)
                        field = new IntegerField();
                    field.SetValueWithoutNotify(property.intValue); // This avoids the OnValueChanged/Rebind feedback loop.
                    field.isDelayed = true; // To match IMGUI. Also, focus is lost anyway due to the rebind.
                    field.RegisterValueChangedCallback((e) => { UpdateArrayFoldout(e, this, m_ParentPropertyField); });
                    return ConfigureField<IntegerField, int>(field, property, () => new IntegerField());
                }

                case SerializedPropertyType.FixedBufferSize:
                    return ConfigureField<IntegerField, int>(originalField as IntegerField, property, () => new IntegerField());

                case SerializedPropertyType.Character:
                {
                    TextField field = originalField as TextField;
                    if (field != null)
                        field.maxLength = 1;
                    return ConfigureField<TextField, string>(field, property, () => new TextField { maxLength = 1 });
                }

                case SerializedPropertyType.AnimationCurve:
                    return ConfigureField<CurveField, AnimationCurve>(originalField as CurveField, property, () => new CurveField());

                case SerializedPropertyType.Bounds:
                    return ConfigureField<BoundsField, Bounds>(originalField as BoundsField, property, () => new BoundsField());

                case SerializedPropertyType.Gradient:
                    return ConfigureField<GradientField, Gradient>(originalField as GradientField, property, () => new GradientField());

                case SerializedPropertyType.Quaternion:
                    return null;
                case SerializedPropertyType.ExposedReference:
                    return null;

                case SerializedPropertyType.Vector2Int:
                    return ConfigureField<Vector2IntField, Vector2Int>(originalField as Vector2IntField, property, () => new Vector2IntField());

                case SerializedPropertyType.Vector3Int:
                    return ConfigureField<Vector3IntField, Vector3Int>(originalField as Vector3IntField, property, () => new Vector3IntField());

                case SerializedPropertyType.RectInt:
                    return ConfigureField<RectIntField, RectInt>(originalField as RectIntField, property, () => new RectIntField());

                case SerializedPropertyType.BoundsInt:
                    return ConfigureField<BoundsIntField, BoundsInt>(originalField as BoundsIntField, property, () => new BoundsIntField());


                case SerializedPropertyType.Generic:
                    return property.isArray
                        ? ConfigureListView(originalField as ListView, property, () => new ListView())
                        : null;

                default:
                    return null;
            }
        }

        private void RegisterPropertyChangesOnCustomDrawerElement(VisualElement customPropertyDrawer)
        {
            // We dispatch this async in order to minimize the number of changeEvents we'll end up dispatching
            customPropertyDrawer.RegisterCallback<ChangeEvent<SerializedProperty>>((changeEvent) => AsyncDispatchPropertyChangedEvent());

            // Now we add property change events for known SerializedPropertyTypes. Since we don't know what this
            // drawer can edit or what it will end up containing we need to register everything
            customPropertyDrawer.RegisterCallback<ChangeEvent<int>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<bool>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<float>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<double>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<string>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Color>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<UnityEngine.Object>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            // SerializedPropertyType.LayerMask -> int
            // SerializedPropertyType.Enum is handled either by string or
            customPropertyDrawer.RegisterCallback<ChangeEvent<Enum>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Vector2>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Vector3>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Vector4>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Rect>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            // SerializedPropertyType.ArraySize ->  int
            // SerializedPropertyType.Character -> string
            customPropertyDrawer.RegisterCallback<ChangeEvent<AnimationCurve>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Bounds>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Gradient>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Quaternion>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Vector2Int>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Vector3Int>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Vector3Int>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<RectInt>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<BoundsInt>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Hash128>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
        }

        private int m_PropertyChangedCounter = 0;

        void AsyncDispatchPropertyChangedEvent()
        {
            m_PropertyChangedCounter++;
            schedule.Execute(() => ExecuteAsyncDispatchPropertyChangedEvent());
        }

        void ExecuteAsyncDispatchPropertyChangedEvent()
        {
            m_PropertyChangedCounter--;

            if (m_PropertyChangedCounter <= 0)
            {
                DispatchPropertyChangedEvent();
                m_PropertyChangedCounter = 0;
            }
        }

        private void DispatchPropertyChangedEvent()
        {
            using (var evt = SerializedPropertyChangeEvent.GetPooled(m_SerializedProperty))
            {
                evt.target = this;
                SendEvent(evt);
            }
        }

        /// <summary>
        /// Registers this callback to receive SerializedPropertyChangeEvent when a value is changed.
        /// </summary>
        public void RegisterValueChangeCallback(EventCallback<SerializedPropertyChangeEvent> callback)
        {
            if (callback != null)
            {
                this.RegisterCallback<SerializedPropertyChangeEvent>((evt) =>
                {
                    if (evt.target == this)
                        callback(evt);
                });
            }
        }
    }
}
