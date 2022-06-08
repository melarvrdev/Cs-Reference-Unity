// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

/******************************************************************************/
//
//                             DO NOT MODIFY
//          This file has been generated by the UIElementsGenerator tool
//              See InlineStyleAccessCsGenerator class for details
//
/******************************************************************************/
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.Yoga;

namespace UnityEngine.UIElements
{
    internal partial class InlineStyleAccess : IStyle
    {
        StyleEnum<Align> IStyle.alignContent
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyId.AlignContent);
                return new StyleEnum<Align>((Align)tmp.value, tmp.keyword);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.AlignContent, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.AlignContent = (YogaAlign)ve.computedStyle.alignContent;
                }
            }
        }

        StyleEnum<Align> IStyle.alignItems
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyId.AlignItems);
                return new StyleEnum<Align>((Align)tmp.value, tmp.keyword);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.AlignItems, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.AlignItems = (YogaAlign)ve.computedStyle.alignItems;
                }
            }
        }

        StyleEnum<Align> IStyle.alignSelf
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyId.AlignSelf);
                return new StyleEnum<Align>((Align)tmp.value, tmp.keyword);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.AlignSelf, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.AlignSelf = (YogaAlign)ve.computedStyle.alignSelf;
                }
            }
        }

        StyleColor IStyle.backgroundColor
        {
            get
            {
                return GetStyleColor(StylePropertyId.BackgroundColor);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.BackgroundColor, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Color);
                }
            }
        }

        StyleBackground IStyle.backgroundImage
        {
            get
            {
                return GetStyleBackground(StylePropertyId.BackgroundImage);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.BackgroundImage, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleBackgroundPosition IStyle.backgroundPositionX
        {
            get
            {
                return GetStyleBackgroundPosition(StylePropertyId.BackgroundPositionX);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.BackgroundPositionX, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleBackgroundPosition IStyle.backgroundPositionY
        {
            get
            {
                return GetStyleBackgroundPosition(StylePropertyId.BackgroundPositionY);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.BackgroundPositionY, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleBackgroundRepeat IStyle.backgroundRepeat
        {
            get
            {
                return GetStyleBackgroundRepeat(StylePropertyId.BackgroundRepeat);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.BackgroundRepeat, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleColor IStyle.borderBottomColor
        {
            get
            {
                return GetStyleColor(StylePropertyId.BorderBottomColor);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.BorderBottomColor, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Color);
                }
            }
        }

        StyleLength IStyle.borderBottomLeftRadius
        {
            get
            {
                return GetStyleLength(StylePropertyId.BorderBottomLeftRadius);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.BorderBottomLeftRadius, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.BorderRadius | VersionChangeType.Repaint);
                }
            }
        }

        StyleLength IStyle.borderBottomRightRadius
        {
            get
            {
                return GetStyleLength(StylePropertyId.BorderBottomRightRadius);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.BorderBottomRightRadius, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.BorderRadius | VersionChangeType.Repaint);
                }
            }
        }

        StyleFloat IStyle.borderBottomWidth
        {
            get
            {
                return GetStyleFloat(StylePropertyId.BorderBottomWidth);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.BorderBottomWidth, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.BorderWidth | VersionChangeType.Layout | VersionChangeType.Repaint);
                    ve.yogaNode.BorderBottomWidth = ve.computedStyle.borderBottomWidth;
                }
            }
        }

        StyleColor IStyle.borderLeftColor
        {
            get
            {
                return GetStyleColor(StylePropertyId.BorderLeftColor);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.BorderLeftColor, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Color);
                }
            }
        }

        StyleFloat IStyle.borderLeftWidth
        {
            get
            {
                return GetStyleFloat(StylePropertyId.BorderLeftWidth);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.BorderLeftWidth, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.BorderWidth | VersionChangeType.Layout | VersionChangeType.Repaint);
                    ve.yogaNode.BorderLeftWidth = ve.computedStyle.borderLeftWidth;
                }
            }
        }

        StyleColor IStyle.borderRightColor
        {
            get
            {
                return GetStyleColor(StylePropertyId.BorderRightColor);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.BorderRightColor, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Color);
                }
            }
        }

        StyleFloat IStyle.borderRightWidth
        {
            get
            {
                return GetStyleFloat(StylePropertyId.BorderRightWidth);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.BorderRightWidth, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.BorderWidth | VersionChangeType.Layout | VersionChangeType.Repaint);
                    ve.yogaNode.BorderRightWidth = ve.computedStyle.borderRightWidth;
                }
            }
        }

        StyleColor IStyle.borderTopColor
        {
            get
            {
                return GetStyleColor(StylePropertyId.BorderTopColor);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.BorderTopColor, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Color);
                }
            }
        }

        StyleLength IStyle.borderTopLeftRadius
        {
            get
            {
                return GetStyleLength(StylePropertyId.BorderTopLeftRadius);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.BorderTopLeftRadius, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.BorderRadius | VersionChangeType.Repaint);
                }
            }
        }

        StyleLength IStyle.borderTopRightRadius
        {
            get
            {
                return GetStyleLength(StylePropertyId.BorderTopRightRadius);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.BorderTopRightRadius, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.BorderRadius | VersionChangeType.Repaint);
                }
            }
        }

        StyleFloat IStyle.borderTopWidth
        {
            get
            {
                return GetStyleFloat(StylePropertyId.BorderTopWidth);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.BorderTopWidth, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.BorderWidth | VersionChangeType.Layout | VersionChangeType.Repaint);
                    ve.yogaNode.BorderTopWidth = ve.computedStyle.borderTopWidth;
                }
            }
        }

        StyleLength IStyle.bottom
        {
            get
            {
                return GetStyleLength(StylePropertyId.Bottom);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.Bottom, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Bottom = ve.computedStyle.bottom.ToYogaValue();
                }
            }
        }

        StyleColor IStyle.color
        {
            get
            {
                return GetStyleColor(StylePropertyId.Color);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.Color, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Color);
                }
            }
        }

        StyleEnum<DisplayStyle> IStyle.display
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyId.Display);
                return new StyleEnum<DisplayStyle>((DisplayStyle)tmp.value, tmp.keyword);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.Display, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout | VersionChangeType.Repaint);
                    ve.yogaNode.Display = (YogaDisplay)ve.computedStyle.display;
                }
            }
        }

        StyleLength IStyle.flexBasis
        {
            get
            {
                return GetStyleLength(StylePropertyId.FlexBasis);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.FlexBasis, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.FlexBasis = ve.computedStyle.flexBasis.ToYogaValue();
                }
            }
        }

        StyleEnum<FlexDirection> IStyle.flexDirection
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyId.FlexDirection);
                return new StyleEnum<FlexDirection>((FlexDirection)tmp.value, tmp.keyword);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.FlexDirection, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.FlexDirection = (YogaFlexDirection)ve.computedStyle.flexDirection;
                }
            }
        }

        StyleFloat IStyle.flexGrow
        {
            get
            {
                return GetStyleFloat(StylePropertyId.FlexGrow);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.FlexGrow, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.FlexGrow = ve.computedStyle.flexGrow;
                }
            }
        }

        StyleFloat IStyle.flexShrink
        {
            get
            {
                return GetStyleFloat(StylePropertyId.FlexShrink);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.FlexShrink, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.FlexShrink = ve.computedStyle.flexShrink;
                }
            }
        }

        StyleEnum<Wrap> IStyle.flexWrap
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyId.FlexWrap);
                return new StyleEnum<Wrap>((Wrap)tmp.value, tmp.keyword);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.FlexWrap, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Wrap = (YogaWrap)ve.computedStyle.flexWrap;
                }
            }
        }

        StyleLength IStyle.fontSize
        {
            get
            {
                return GetStyleLength(StylePropertyId.FontSize);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.FontSize, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Layout);
                }
            }
        }

        StyleLength IStyle.height
        {
            get
            {
                return GetStyleLength(StylePropertyId.Height);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.Height, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Height = ve.computedStyle.height.ToYogaValue();
                }
            }
        }

        StyleEnum<Justify> IStyle.justifyContent
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyId.JustifyContent);
                return new StyleEnum<Justify>((Justify)tmp.value, tmp.keyword);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.JustifyContent, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.JustifyContent = (YogaJustify)ve.computedStyle.justifyContent;
                }
            }
        }

        StyleLength IStyle.left
        {
            get
            {
                return GetStyleLength(StylePropertyId.Left);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.Left, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Left = ve.computedStyle.left.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.letterSpacing
        {
            get
            {
                return GetStyleLength(StylePropertyId.LetterSpacing);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.LetterSpacing, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Layout | VersionChangeType.Repaint);
                }
            }
        }

        StyleLength IStyle.marginBottom
        {
            get
            {
                return GetStyleLength(StylePropertyId.MarginBottom);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.MarginBottom, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MarginBottom = ve.computedStyle.marginBottom.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.marginLeft
        {
            get
            {
                return GetStyleLength(StylePropertyId.MarginLeft);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.MarginLeft, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MarginLeft = ve.computedStyle.marginLeft.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.marginRight
        {
            get
            {
                return GetStyleLength(StylePropertyId.MarginRight);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.MarginRight, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MarginRight = ve.computedStyle.marginRight.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.marginTop
        {
            get
            {
                return GetStyleLength(StylePropertyId.MarginTop);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.MarginTop, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MarginTop = ve.computedStyle.marginTop.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.maxHeight
        {
            get
            {
                return GetStyleLength(StylePropertyId.MaxHeight);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.MaxHeight, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MaxHeight = ve.computedStyle.maxHeight.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.maxWidth
        {
            get
            {
                return GetStyleLength(StylePropertyId.MaxWidth);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.MaxWidth, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MaxWidth = ve.computedStyle.maxWidth.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.minHeight
        {
            get
            {
                return GetStyleLength(StylePropertyId.MinHeight);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.MinHeight, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MinHeight = ve.computedStyle.minHeight.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.minWidth
        {
            get
            {
                return GetStyleLength(StylePropertyId.MinWidth);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.MinWidth, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MinWidth = ve.computedStyle.minWidth.ToYogaValue();
                }
            }
        }

        StyleFloat IStyle.opacity
        {
            get
            {
                return GetStyleFloat(StylePropertyId.Opacity);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.Opacity, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Opacity);
                }
            }
        }

        StyleEnum<Overflow> IStyle.overflow
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyId.Overflow);
                return new StyleEnum<Overflow>((Overflow)tmp.value, tmp.keyword);
            }

            set
            {
                var tmp = new StyleEnum<OverflowInternal>((OverflowInternal)value.value, value.keyword);
                if (SetStyleValue(StylePropertyId.Overflow, tmp))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout | VersionChangeType.Overflow);
                    ve.yogaNode.Overflow = (YogaOverflow)ve.computedStyle.overflow;
                }
            }
        }

        StyleLength IStyle.paddingBottom
        {
            get
            {
                return GetStyleLength(StylePropertyId.PaddingBottom);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.PaddingBottom, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.PaddingBottom = ve.computedStyle.paddingBottom.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.paddingLeft
        {
            get
            {
                return GetStyleLength(StylePropertyId.PaddingLeft);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.PaddingLeft, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.PaddingLeft = ve.computedStyle.paddingLeft.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.paddingRight
        {
            get
            {
                return GetStyleLength(StylePropertyId.PaddingRight);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.PaddingRight, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.PaddingRight = ve.computedStyle.paddingRight.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.paddingTop
        {
            get
            {
                return GetStyleLength(StylePropertyId.PaddingTop);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.PaddingTop, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.PaddingTop = ve.computedStyle.paddingTop.ToYogaValue();
                }
            }
        }

        StyleEnum<Position> IStyle.position
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyId.Position);
                return new StyleEnum<Position>((Position)tmp.value, tmp.keyword);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.Position, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.PositionType = (YogaPositionType)ve.computedStyle.position;
                }
            }
        }

        StyleLength IStyle.right
        {
            get
            {
                return GetStyleLength(StylePropertyId.Right);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.Right, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Right = ve.computedStyle.right.ToYogaValue();
                }
            }
        }

        StyleEnum<TextOverflow> IStyle.textOverflow
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyId.TextOverflow);
                return new StyleEnum<TextOverflow>((TextOverflow)tmp.value, tmp.keyword);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.TextOverflow, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout | VersionChangeType.Repaint);
                }
            }
        }

        StyleLength IStyle.top
        {
            get
            {
                return GetStyleLength(StylePropertyId.Top);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.Top, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Top = ve.computedStyle.top.ToYogaValue();
                }
            }
        }

        StyleList<TimeValue> IStyle.transitionDelay
        {
            get
            {
                return GetStyleList<TimeValue>(StylePropertyId.TransitionDelay);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.TransitionDelay, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.TransitionProperty);
                }
            }
        }

        StyleList<TimeValue> IStyle.transitionDuration
        {
            get
            {
                return GetStyleList<TimeValue>(StylePropertyId.TransitionDuration);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.TransitionDuration, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.TransitionProperty);
                }
            }
        }

        StyleList<StylePropertyName> IStyle.transitionProperty
        {
            get
            {
                return GetStyleList<StylePropertyName>(StylePropertyId.TransitionProperty);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.TransitionProperty, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.TransitionProperty);
                }
            }
        }

        StyleList<EasingFunction> IStyle.transitionTimingFunction
        {
            get
            {
                return GetStyleList<EasingFunction>(StylePropertyId.TransitionTimingFunction);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.TransitionTimingFunction, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles);
                }
            }
        }

        StyleColor IStyle.unityBackgroundImageTintColor
        {
            get
            {
                return GetStyleColor(StylePropertyId.UnityBackgroundImageTintColor);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.UnityBackgroundImageTintColor, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Color);
                }
            }
        }

        StyleFont IStyle.unityFont
        {
            get
            {
                return GetStyleFont(StylePropertyId.UnityFont);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.UnityFont, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Layout | VersionChangeType.Repaint);
                }
            }
        }

        StyleFontDefinition IStyle.unityFontDefinition
        {
            get
            {
                return GetStyleFontDefinition(StylePropertyId.UnityFontDefinition);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.UnityFontDefinition, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Layout | VersionChangeType.Repaint);
                }
            }
        }

        StyleEnum<FontStyle> IStyle.unityFontStyleAndWeight
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyId.UnityFontStyleAndWeight);
                return new StyleEnum<FontStyle>((FontStyle)tmp.value, tmp.keyword);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.UnityFontStyleAndWeight, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Layout | VersionChangeType.Repaint);
                }
            }
        }

        StyleEnum<OverflowClipBox> IStyle.unityOverflowClipBox
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyId.UnityOverflowClipBox);
                return new StyleEnum<OverflowClipBox>((OverflowClipBox)tmp.value, tmp.keyword);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.UnityOverflowClipBox, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleLength IStyle.unityParagraphSpacing
        {
            get
            {
                return GetStyleLength(StylePropertyId.UnityParagraphSpacing);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.UnityParagraphSpacing, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Layout | VersionChangeType.Repaint);
                }
            }
        }

        StyleInt IStyle.unitySliceBottom
        {
            get
            {
                return GetStyleInt(StylePropertyId.UnitySliceBottom);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.UnitySliceBottom, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleInt IStyle.unitySliceLeft
        {
            get
            {
                return GetStyleInt(StylePropertyId.UnitySliceLeft);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.UnitySliceLeft, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleInt IStyle.unitySliceRight
        {
            get
            {
                return GetStyleInt(StylePropertyId.UnitySliceRight);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.UnitySliceRight, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleFloat IStyle.unitySliceScale
        {
            get
            {
                return GetStyleFloat(StylePropertyId.UnitySliceScale);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.UnitySliceScale, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout | VersionChangeType.Repaint);
                }
            }
        }

        StyleInt IStyle.unitySliceTop
        {
            get
            {
                return GetStyleInt(StylePropertyId.UnitySliceTop);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.UnitySliceTop, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleEnum<TextAnchor> IStyle.unityTextAlign
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyId.UnityTextAlign);
                return new StyleEnum<TextAnchor>((TextAnchor)tmp.value, tmp.keyword);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.UnityTextAlign, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Repaint);
                }
            }
        }

        StyleColor IStyle.unityTextOutlineColor
        {
            get
            {
                return GetStyleColor(StylePropertyId.UnityTextOutlineColor);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.UnityTextOutlineColor, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Repaint);
                }
            }
        }

        StyleFloat IStyle.unityTextOutlineWidth
        {
            get
            {
                return GetStyleFloat(StylePropertyId.UnityTextOutlineWidth);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.UnityTextOutlineWidth, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Layout | VersionChangeType.Repaint);
                }
            }
        }

        StyleEnum<TextOverflowPosition> IStyle.unityTextOverflowPosition
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyId.UnityTextOverflowPosition);
                return new StyleEnum<TextOverflowPosition>((TextOverflowPosition)tmp.value, tmp.keyword);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.UnityTextOverflowPosition, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleEnum<Visibility> IStyle.visibility
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyId.Visibility);
                return new StyleEnum<Visibility>((Visibility)tmp.value, tmp.keyword);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.Visibility, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Repaint);
                }
            }
        }

        StyleEnum<WhiteSpace> IStyle.whiteSpace
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyId.WhiteSpace);
                return new StyleEnum<WhiteSpace>((WhiteSpace)tmp.value, tmp.keyword);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.WhiteSpace, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Layout);
                }
            }
        }

        StyleLength IStyle.width
        {
            get
            {
                return GetStyleLength(StylePropertyId.Width);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.Width, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Width = ve.computedStyle.width.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.wordSpacing
        {
            get
            {
                return GetStyleLength(StylePropertyId.WordSpacing);
            }

            set
            {
                if (SetStyleValue(StylePropertyId.WordSpacing, value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Layout | VersionChangeType.Repaint);
                }
            }
        }
    }
}
