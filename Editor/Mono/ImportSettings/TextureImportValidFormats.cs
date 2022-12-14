// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor
{
    internal static class TextureImportValidFormats
    {
        private struct TextureTypeAndBuildTarget
        {
            private TextureImporterType textureType;
            private BuildTarget target;

            public TextureTypeAndBuildTarget(TextureImporterType textureType, BuildTarget target)
            {
                this.textureType = textureType;
                this.target = target;
            }

            public override int GetHashCode()
            {
                return textureType.GetHashCode() ^ target.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (!(obj is TextureTypeAndBuildTarget))
                    return false;

                var textureTypeAndBuildTarget = (TextureTypeAndBuildTarget)obj;

                return ((this.textureType == textureTypeAndBuildTarget.textureType) &&
                    (this.target == textureTypeAndBuildTarget.target));
            }
        }

        private struct Value
        {
            public int[] formatValues;
            public string[] formatStrings;

            public Value(int[] formatValues, string[] formatStrings)
            {
                this.formatValues = formatValues;
                this.formatStrings = formatStrings;
            }
        }

        private static Dictionary<TextureTypeAndBuildTarget, Value> s_ValidTextureFormats;
        private static Dictionary<TextureImporterType, Value> s_ValidDefaultTextureFormats;

        private static string[] BuildTextureStrings(int[] texFormatValues)
        {
            string[] retval = new string[texFormatValues.Length];
            for (int i = 0; i < texFormatValues.Length; i++)
            {
                int val = texFormatValues[i];
                retval[i] = (val < 0 ? "Automatic" : GraphicsFormatUtility.GetFormatString((TextureFormat)val));
            }
            return retval;
        }

        private static void BuildValidFormats()
        {
            if (s_ValidTextureFormats != null)
                return;

            s_ValidTextureFormats = new Dictionary<TextureTypeAndBuildTarget, Value>();
            s_ValidDefaultTextureFormats = new Dictionary<TextureImporterType, Value>();

            foreach (var textureTypeName in Enum.GetNames(typeof(TextureImporterType)))
            {
                var textureTypeField = typeof(TextureImporterType).GetField(textureTypeName);
                if (textureTypeField.IsDefined(typeof(ObsoleteAttribute), false))
                    continue;

                var textureType = (TextureImporterType)Enum.Parse(typeof(TextureImporterType), textureTypeName);

                List<int> defaultFormats = null;

                foreach (var targetName in Enum.GetNames(typeof(BuildTarget)))
                {
                    var targetField = typeof(BuildTarget).GetField(targetName);
                    if (targetField.IsDefined(typeof(ObsoleteAttribute), false))
                        continue;

                    var target = (BuildTarget)Enum.Parse(typeof(BuildTarget), targetName);

                    var Key = new TextureTypeAndBuildTarget(textureType, target);

                    var validFormats = Array.ConvertAll(TextureImporter.RecommendedFormatsFromTextureTypeAndPlatform(textureType, target), value => (int)value);
                    Array.Sort(validFormats, SortTextureFormats);
                    s_ValidTextureFormats.Add(Key, new Value(validFormats, BuildTextureStrings(validFormats)));

                    defaultFormats = defaultFormats?.Intersect(validFormats).ToList() ?? validFormats.ToList();
                }

                // need "Auto" as the first entry for defaults
                defaultFormats.Insert(0, -1);
                var defaultFormatsArray = defaultFormats.ToArray();
                s_ValidDefaultTextureFormats[textureType] = new Value(defaultFormatsArray, BuildTextureStrings(defaultFormatsArray));
            }
        }

        // formats with higher sorting code appear first on the dropdown lists
        static uint GetSortCodeForFormat(TextureFormat fmt)
        {
            var f = GraphicsFormatUtility.GetGraphicsFormat(fmt, false);

            // first by: normalized, floating point, integer
            uint type;
            if (GraphicsFormatUtility.IsNormFormat(f))
                type = 3;
            else if (GraphicsFormatUtility.IsHalfFormat(f) || GraphicsFormatUtility.IsFloatFormat(f))
                type = 2;
            else if (GraphicsFormatUtility.IsIntegerFormat(f))
                type = 1;
            else
                type = 0;

            // then by component count: RGBA, RGB, RG, R/A
            var components = GraphicsFormatUtility.GetComponentCount(f);

            // then compression: first compressed regular, then compressed Crunch, then uncompressed
            uint compression = 0;
            if (GraphicsFormatUtility.IsCompressedFormat(f))
            {
                compression++;
                if (!GraphicsFormatUtility.IsCrunchFormat(fmt))
                    compression++;
            }

            return (type << 24) | (components << 16) | (compression << 8);
        }

        static int SortTextureFormats(int fmta, int fmtb)
        {
            var sortA = GetSortCodeForFormat((TextureFormat)fmta);
            var sortB = GetSortCodeForFormat((TextureFormat)fmtb);
            if (sortA != sortB)
                return sortB.CompareTo(sortA);

            return fmtb.CompareTo(fmta);
        }

        public static void GetPlatformTextureFormatValuesAndStrings(TextureImporterType textureType, BuildTarget target, out int[] formatValues, out string[] formatStrings)
        {
            BuildValidFormats();

            var key = new TextureTypeAndBuildTarget(textureType, target);
            formatValues = s_ValidTextureFormats[key].formatValues;
            formatStrings = s_ValidTextureFormats[key].formatStrings;
        }

        public static void GetDefaultTextureFormatValuesAndStrings(TextureImporterType textureType, out int[] formatValues, out string[] formatStrings)
        {
            BuildValidFormats();

            formatValues = s_ValidDefaultTextureFormats[textureType].formatValues;
            formatStrings = s_ValidDefaultTextureFormats[textureType].formatStrings;
        }
    }
}
