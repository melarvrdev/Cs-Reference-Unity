// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

namespace UnityEngine.UIElements
{
    internal static class UIRUtility
    {
        public static readonly string k_DefaultShaderName = UIR.Shaders.k_Runtime;
        public static readonly string k_DefaultWorldSpaceShaderName = UIR.Shaders.k_RuntimeWorld;

        // We provide our own epsilon to avoid issues such as case 1335430. Some native plugin
        // disable float-denormalization, which can lead to the wrong Mathf.Epsilon being used.
        public const float k_Epsilon = 1.0E-30f;

        public const float k_ClearZ = 0.99f; // At the far plane like standard Unity rendering
        public const float k_MeshPosZ = 0.0f; // The correct z value to draw a shape
        public const float k_MaskPosZ = 1.0f; // The correct z value to push/pop a mask
        public const int k_MaxMaskDepth = 7; // Requires 3 bits in the stencil

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool ShapeWindingIsClockwise(int maskDepth, int stencilRef)
        {
            Debug.Assert(maskDepth == stencilRef || maskDepth == stencilRef + 1);
            return maskDepth == stencilRef;
        }

        public static Vector4 ToVector4(Rect rc)
        {
            return new Vector4(rc.xMin, rc.yMin, rc.xMax, rc.yMax);
        }

        public static bool IsRoundRect(VisualElement ve)
        {
            var style = ve.resolvedStyle;
            return !(style.borderTopLeftRadius < k_Epsilon &&
                style.borderTopRightRadius < k_Epsilon &&
                style.borderBottomLeftRadius < k_Epsilon &&
                style.borderBottomRightRadius < k_Epsilon);
        }

        public static void Multiply2D(this Quaternion rotation, ref Vector2 point)
        {
            // Even though Quaternion coordinates aren't the same as Euler angles, it so happens that a rotation only
            // in the z axis will also have only a z (and w) value that is non-zero. Cool, heh!
            // Here we'll assume rotation.x = rotation.y = 0.
            float z = rotation.z * 2f;
            float zz = 1f - rotation.z * z;
            float wz = rotation.w * z;
            point = new Vector2(zz * point.x - wz * point.y, wz * point.x + zz * point.y);
        }

        public static bool IsVectorImageBackground(VisualElement ve)
        {
            return ve.computedStyle.backgroundImage.vectorImage != null;
        }

        public static bool IsElementSelfHidden(VisualElement ve)
        {
            return ve.resolvedStyle.visibility == Visibility.Hidden;
        }

        public static void Destroy(Object obj)
        {
            if (obj == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }

        public static int GetPrevPow2(int n)
        {
            int bits = 0;
            while (n > 1)
            {
                n >>= 1;
                ++bits;
            }

            return 1 << bits;
        }

        public static int GetNextPow2(int n)
        {
            int test = 1;
            while (test < n)
                test <<= 1;
            return test;
        }

        public static int GetNextPow2Exp(int n)
        {
            int test = 1;
            int exp = 0;
            while (test < n)
            {
                test <<= 1;
                ++exp;
            }

            return exp;
        }
    }
}
