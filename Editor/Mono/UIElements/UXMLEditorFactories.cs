// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [InitializeOnLoad]
    internal class UXMLEditorFactories
    {
        private static readonly bool k_Registered;

        static UXMLEditorFactories()
        {
            if (k_Registered)
                return;

            k_Registered = true;

            IUxmlFactory[] factories =
            {
                // Primitives
                new TextElement.UxmlFactory(),

                // Compounds
                new PropertyControl<int>.UxmlFactory(),
                new PropertyControl<long>.UxmlFactory(),
                new PropertyControl<float>.UxmlFactory(),
                new PropertyControl<double>.UxmlFactory(),
                new PropertyControl<string>.UxmlFactory(),

                new VisualSplitter.UxmlFactory(),

                // Toolbar
                new Toolbar.UxmlFactory(),
                new ToolbarButton.UxmlFactory(),
                new ToolbarToggle.UxmlFactory(),
                new ToolbarSpacer.UxmlFactory(),
                new ToolbarMenu.UxmlFactory(),
                new ToolbarSearchField.UxmlFactory(),
                new ToolbarPopupSearchField.UxmlFactory(),
                // Bound
                new PropertyField.UxmlFactory(),
                new InspectorElement.UxmlFactory(),

                // Fields
                new FloatField.UxmlFactory(),
                new DoubleField.UxmlFactory(),
                new IntegerField.UxmlFactory(),
                new LongField.UxmlFactory(),
                new CurveField.UxmlFactory(),
                new ObjectField.UxmlFactory(),
                new ColorField.UxmlFactory(),
                new EnumField.UxmlFactory(),
                new MaskField.UxmlFactory(),
                new LayerMaskField.UxmlFactory(),
                new LayerField.UxmlFactory(),
                new TagField.UxmlFactory(),
                new GradientField.UxmlFactory(),

                // Compounds
                new RectField.UxmlFactory(),
                new Vector2Field.UxmlFactory(),
                new Vector3Field.UxmlFactory(),
                new Vector4Field.UxmlFactory(),
                new BoundsField.UxmlFactory(),


                new RectIntField.UxmlFactory(),
                new Vector2IntField.UxmlFactory(),
                new Vector3IntField.UxmlFactory(),
                new BoundsIntField.UxmlFactory(),

                new ProgressBar.UxmlFactory(),
            };

            foreach (IUxmlFactory factory in factories)
            {
                VisualElementFactoryRegistry.RegisterFactory(factory);
            }
        }
    }
}
