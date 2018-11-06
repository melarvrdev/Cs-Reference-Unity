// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class FloatField : TextValueField<float>
    {
        // This property to alleviate the fact we have to cast all the time
        FloatInput floatInput => (FloatInput)textInputBase;

        public new class UxmlFactory : UxmlFactory<FloatField, UxmlTraits> {}
        public new class UxmlTraits : BaseFieldTraits<float, UxmlFloatAttributeDescription> {}

        protected override string ValueToString(float v)
        {
            return v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);
        }

        protected override float StringToValue(string str)
        {
            double v;
            EditorGUI.StringToDouble(str, out v);
            return MathUtils.ClampToFloat(v);
        }

        public new static readonly string ussClassName = "unity-float-field";

        public FloatField() : this((string)null) {}

        public FloatField(int maxLength)
            : this(null, maxLength) {}

        public FloatField(string label, int maxLength = kMaxLengthNone)
            : base(label, maxLength, new FloatInput() { name = "unity-text-input" })
        {
            AddToClassList(ussClassName);
            AddLabelDragger<float>();
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, float startValue)
        {
            floatInput.ApplyInputDeviceDelta(delta, speed, startValue);
        }

        class FloatInput : TextValueInput
        {
            FloatField parentFloatField => (FloatField)parentField;

            internal FloatInput()
            {
                formatString = EditorGUI.kFloatFieldFormatString;
            }

            protected override string allowedCharacters => EditorGUI.s_AllowedCharactersForFloat;

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, float startValue)
            {
                double sensitivity = NumericFieldDraggerUtility.CalculateFloatDragSensitivity(startValue);
                float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                double v = parentFloatField.value;
                v += NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity;
                v = MathUtils.RoundBasedOnMinimumDifference(v, sensitivity);
                parentFloatField.value = MathUtils.ClampToFloat(v);
            }

            protected override string ValueToString(float v)
            {
                return v.ToString(formatString);
            }

            protected override float StringToValue(string str)
            {
                double v;
                EditorGUI.StringToDouble(str, out v);
                return MathUtils.ClampToFloat(v);
            }
        }
    }
}
