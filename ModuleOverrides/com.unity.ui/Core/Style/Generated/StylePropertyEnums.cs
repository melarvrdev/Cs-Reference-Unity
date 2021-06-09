// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

/******************************************************************************/
//
//                             DO NOT MODIFY
//          This file has been generated by the UIElementsGenerator tool
//              See StylePropertyEnumCsGenerator class for details
//
/******************************************************************************/
using static UnityEngine.UIElements.StyleSheets.StylePropertyUtil;

namespace UnityEngine.UIElements.StyleSheets
{
    internal enum StylePropertyGroup
    {
        Inherited = 1,
        Layout = 2,
        Rare = 3,
        Shorthand = 4,
        Transform = 5,
        Transition = 6,
        Visual = 7
    }

    internal enum StyleEnumType
    {
        Align,
        DisplayStyle,
        EasingMode,
        FlexDirection,
        FontStyle,
        Justify,
        Overflow,
        OverflowClipBox,
        OverflowInternal,
        Position,
        ScaleMode,
        TextAnchor,
        TextOverflow,
        TextOverflowPosition,
        TransformOriginOffset,
        Visibility,
        WhiteSpace,
        Wrap
    }

    internal enum StylePropertyId
    {
        Unknown = 0,
        Custom = -1,
        AlignContent = StylePropertyGroup.Layout << k_GroupOffset | 0,
        AlignItems = StylePropertyGroup.Layout << k_GroupOffset | 1,
        AlignSelf = StylePropertyGroup.Layout << k_GroupOffset | 2,
        All = StylePropertyGroup.Shorthand << k_GroupOffset | 0,
        BackgroundColor = StylePropertyGroup.Visual << k_GroupOffset | 0,
        BackgroundImage = StylePropertyGroup.Visual << k_GroupOffset | 1,
        BorderBottomColor = StylePropertyGroup.Visual << k_GroupOffset | 2,
        BorderBottomLeftRadius = StylePropertyGroup.Visual << k_GroupOffset | 3,
        BorderBottomRightRadius = StylePropertyGroup.Visual << k_GroupOffset | 4,
        BorderBottomWidth = StylePropertyGroup.Layout << k_GroupOffset | 3,
        BorderColor = StylePropertyGroup.Shorthand << k_GroupOffset | 1,
        BorderLeftColor = StylePropertyGroup.Visual << k_GroupOffset | 5,
        BorderLeftWidth = StylePropertyGroup.Layout << k_GroupOffset | 4,
        BorderRadius = StylePropertyGroup.Shorthand << k_GroupOffset | 2,
        BorderRightColor = StylePropertyGroup.Visual << k_GroupOffset | 6,
        BorderRightWidth = StylePropertyGroup.Layout << k_GroupOffset | 5,
        BorderTopColor = StylePropertyGroup.Visual << k_GroupOffset | 7,
        BorderTopLeftRadius = StylePropertyGroup.Visual << k_GroupOffset | 8,
        BorderTopRightRadius = StylePropertyGroup.Visual << k_GroupOffset | 9,
        BorderTopWidth = StylePropertyGroup.Layout << k_GroupOffset | 6,
        BorderWidth = StylePropertyGroup.Shorthand << k_GroupOffset | 3,
        Bottom = StylePropertyGroup.Layout << k_GroupOffset | 7,
        Color = StylePropertyGroup.Inherited << k_GroupOffset | 0,
        Cursor = StylePropertyGroup.Rare << k_GroupOffset | 0,
        Display = StylePropertyGroup.Layout << k_GroupOffset | 8,
        Flex = StylePropertyGroup.Shorthand << k_GroupOffset | 4,
        FlexBasis = StylePropertyGroup.Layout << k_GroupOffset | 9,
        FlexDirection = StylePropertyGroup.Layout << k_GroupOffset | 10,
        FlexGrow = StylePropertyGroup.Layout << k_GroupOffset | 11,
        FlexShrink = StylePropertyGroup.Layout << k_GroupOffset | 12,
        FlexWrap = StylePropertyGroup.Layout << k_GroupOffset | 13,
        FontSize = StylePropertyGroup.Inherited << k_GroupOffset | 1,
        Height = StylePropertyGroup.Layout << k_GroupOffset | 14,
        JustifyContent = StylePropertyGroup.Layout << k_GroupOffset | 15,
        Left = StylePropertyGroup.Layout << k_GroupOffset | 16,
        LetterSpacing = StylePropertyGroup.Inherited << k_GroupOffset | 2,
        Margin = StylePropertyGroup.Shorthand << k_GroupOffset | 5,
        MarginBottom = StylePropertyGroup.Layout << k_GroupOffset | 17,
        MarginLeft = StylePropertyGroup.Layout << k_GroupOffset | 18,
        MarginRight = StylePropertyGroup.Layout << k_GroupOffset | 19,
        MarginTop = StylePropertyGroup.Layout << k_GroupOffset | 20,
        MaxHeight = StylePropertyGroup.Layout << k_GroupOffset | 21,
        MaxWidth = StylePropertyGroup.Layout << k_GroupOffset | 22,
        MinHeight = StylePropertyGroup.Layout << k_GroupOffset | 23,
        MinWidth = StylePropertyGroup.Layout << k_GroupOffset | 24,
        Opacity = StylePropertyGroup.Visual << k_GroupOffset | 10,
        Overflow = StylePropertyGroup.Visual << k_GroupOffset | 11,
        Padding = StylePropertyGroup.Shorthand << k_GroupOffset | 6,
        PaddingBottom = StylePropertyGroup.Layout << k_GroupOffset | 25,
        PaddingLeft = StylePropertyGroup.Layout << k_GroupOffset | 26,
        PaddingRight = StylePropertyGroup.Layout << k_GroupOffset | 27,
        PaddingTop = StylePropertyGroup.Layout << k_GroupOffset | 28,
        Position = StylePropertyGroup.Layout << k_GroupOffset | 29,
        Right = StylePropertyGroup.Layout << k_GroupOffset | 30,
        Rotate = StylePropertyGroup.Transform << k_GroupOffset | 0,
        Scale = StylePropertyGroup.Transform << k_GroupOffset | 1,
        TextOverflow = StylePropertyGroup.Rare << k_GroupOffset | 1,
        TextShadow = StylePropertyGroup.Inherited << k_GroupOffset | 3,
        Top = StylePropertyGroup.Layout << k_GroupOffset | 31,
        TransformOrigin = StylePropertyGroup.Transform << k_GroupOffset | 2,
        Transition = StylePropertyGroup.Shorthand << k_GroupOffset | 7,
        TransitionDelay = StylePropertyGroup.Transition << k_GroupOffset | 0,
        TransitionDuration = StylePropertyGroup.Transition << k_GroupOffset | 1,
        TransitionProperty = StylePropertyGroup.Transition << k_GroupOffset | 2,
        TransitionTimingFunction = StylePropertyGroup.Transition << k_GroupOffset | 3,
        Translate = StylePropertyGroup.Transform << k_GroupOffset | 3,
        UnityBackgroundImageTintColor = StylePropertyGroup.Rare << k_GroupOffset | 2,
        UnityBackgroundScaleMode = StylePropertyGroup.Rare << k_GroupOffset | 3,
        UnityFont = StylePropertyGroup.Inherited << k_GroupOffset | 4,
        UnityFontDefinition = StylePropertyGroup.Inherited << k_GroupOffset | 5,
        UnityFontStyleAndWeight = StylePropertyGroup.Inherited << k_GroupOffset | 6,
        UnityOverflowClipBox = StylePropertyGroup.Rare << k_GroupOffset | 4,
        UnityParagraphSpacing = StylePropertyGroup.Inherited << k_GroupOffset | 7,
        UnitySliceBottom = StylePropertyGroup.Rare << k_GroupOffset | 5,
        UnitySliceLeft = StylePropertyGroup.Rare << k_GroupOffset | 6,
        UnitySliceRight = StylePropertyGroup.Rare << k_GroupOffset | 7,
        UnitySliceTop = StylePropertyGroup.Rare << k_GroupOffset | 8,
        UnityTextAlign = StylePropertyGroup.Inherited << k_GroupOffset | 8,
        UnityTextOutline = StylePropertyGroup.Shorthand << k_GroupOffset | 8,
        UnityTextOutlineColor = StylePropertyGroup.Inherited << k_GroupOffset | 9,
        UnityTextOutlineWidth = StylePropertyGroup.Inherited << k_GroupOffset | 10,
        UnityTextOverflowPosition = StylePropertyGroup.Rare << k_GroupOffset | 9,
        Visibility = StylePropertyGroup.Inherited << k_GroupOffset | 11,
        WhiteSpace = StylePropertyGroup.Inherited << k_GroupOffset | 12,
        Width = StylePropertyGroup.Layout << k_GroupOffset | 32,
        WordSpacing = StylePropertyGroup.Inherited << k_GroupOffset | 13
    }
}
