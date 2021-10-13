// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace UnityEngine.UIElements
{
    // This is basically the same as the standard layout update except for 1 thing :
    // - Only call dirty repaint when the layout rect has changed instead of "yogaNode.HasNewLayout"
    internal class UIRLayoutUpdater : BaseVisualTreeUpdater
    {
        const int kMaxValidateLayoutCount = 5;


        private static readonly string s_Description = "Update Layout";
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & (VersionChangeType.Layout | VersionChangeType.Hierarchy)) == 0)
                return;

            // Yoga DOES NOT expect the node tree to mutate while layout is being computed.
            // it could lead to crashes (EXC_BAD_ACCESS)
            if ((versionChangeType & VersionChangeType.Hierarchy) != 0 && panel.duringLayoutPhase)
            {
                throw new InvalidOperationException("Hierarchy change detected while computing layout, this is not supported.");
            }

            var yogaNode = ve.yogaNode;
            if (yogaNode != null && yogaNode.IsMeasureDefined)
            {
                yogaNode.MarkDirty();
            }
        }

        public override void Update()
        {
            // update flex once
            int validateLayoutCount = 0;
            while (visualTree.yogaNode.IsDirty)
            {
                // Doing multiple layout passes requires to update the styles or else the
                // elements may not be initialized properly and the resulting layout will be invalid.
                if (validateLayoutCount > 0)
                    panel.ApplyStyles();

                panel.duringLayoutPhase = true;
                visualTree.yogaNode.CalculateLayout();
                panel.duringLayoutPhase = false;

                using (new EventDispatcherGate(visualTree.panel.dispatcher))
                {
                    UpdateSubTree(visualTree, validateLayoutCount);
                }

                if (validateLayoutCount++ >= kMaxValidateLayoutCount)
                {
                    Debug.LogError("Layout update is struggling to process current layout (consider simplifying to avoid recursive layout): " + visualTree);
                    break;
                }
            }

            // This call happens here for two reasons
            // 1. Visibility style of the focused element may have changed (regardless of the layout having updated)
            // 2. Display style of the focused element may have changed, but it's only manually propagated to children
            //    as part of this updater.
            // Note: this is a O(1) call
            visualTree.focusController.ReevaluateFocus();
        }

        private void UpdateSubTree(VisualElement ve, int currentLayoutPass, bool isDisplayed = true)
        {
            Rect yogaLayoutRect = new Rect(ve.yogaNode.LayoutX, ve.yogaNode.LayoutY, ve.yogaNode.LayoutWidth, ve.yogaNode.LayoutHeight);
            // This is not the "real" padding rect: it may differ in size and position because the borders are ignored.
            // Alone, it cannot be used to identify padding size changes, because the bottom and right values depend on the
            // layout rect width/height. A change in layout rect width/height with a corresponding variation in
            // right/bottom values may yield the same pseudoPaddingRect. Fortunately, the layout rect size and padding rect
            // size change trigger the same code path which explains why we haven't seen any issue with this so far.
            Rect yogaPseudoPaddingRect = new Rect(
                ve.yogaNode.LayoutPaddingLeft,
                ve.yogaNode.LayoutPaddingTop,
                ve.yogaNode.LayoutWidth - (ve.yogaNode.LayoutPaddingLeft + ve.yogaNode.LayoutPaddingRight),
                ve.yogaNode.LayoutHeight - (ve.yogaNode.LayoutPaddingTop + ve.yogaNode.LayoutPaddingBottom));
            Rect lastLayoutRect = ve.lastLayout;
            Rect lastPseudoPaddingRect = ve.lastPseudoPadding;
            bool wasHierarchyDisplayed = ve.isHierarchyDisplayed;

            VersionChangeType changeType = 0;
            // Changing the layout/padding size should trigger the following version changes:
            // - Size:    to update the clipping rect, when required
            // - Repaint: to update the geometry inside the new rect
            bool layoutSizeChanged = lastLayoutRect.size != yogaLayoutRect.size;
            bool pseudoPaddingSizeChanged = lastPseudoPaddingRect.size != yogaPseudoPaddingRect.size;
            if (layoutSizeChanged || pseudoPaddingSizeChanged)
                changeType |= VersionChangeType.Size | VersionChangeType.Repaint;

            // Changing the layout/padding position should trigger the following version change:
            // - Transform: to draw the element and content at the right position
            bool layoutPositionChanged = yogaLayoutRect.position != lastLayoutRect.position;
            bool paddingPositionChanged = yogaPseudoPaddingRect.position != lastPseudoPaddingRect.position;
            if (layoutPositionChanged || paddingPositionChanged)
                changeType |= VersionChangeType.Transform;

            isDisplayed &= ve.resolvedStyle.display != DisplayStyle.None;
            ve.isHierarchyDisplayed = isDisplayed;

            if (changeType != 0)
                ve.IncrementVersion(changeType);

            ve.lastLayout = yogaLayoutRect;
            ve.lastPseudoPadding = yogaPseudoPaddingRect;

            // ignore clean sub trees
            bool hasNewLayout = ve.yogaNode.HasNewLayout;
            if (hasNewLayout)
            {
                var childCount = ve.hierarchy.childCount;
                for (int i = 0; i < childCount; ++i)
                {
                    UpdateSubTree(ve.hierarchy[i], currentLayoutPass, isDisplayed);
                }
            }

            // Only send GeometryChanged events when the layout changes
            // (padding changes don't affect the element's geometry).
            if ((layoutSizeChanged || layoutPositionChanged) && ve.HasEventCallbacksOrDefaultActions(GeometryChangedEvent.EventCategory))
            {
                using (var evt = GeometryChangedEvent.GetPooled(lastLayoutRect, yogaLayoutRect))
                {
                    evt.layoutPass = currentLayoutPass;
                    evt.target = ve;
                    ve.HandleEventAtTargetAndDefaultPhase(evt);
                }
            }

            if (hasNewLayout)
            {
                ve.yogaNode.MarkLayoutSeen();
            }
        }
    }
}
