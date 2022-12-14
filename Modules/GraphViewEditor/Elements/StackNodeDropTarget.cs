// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEditor.Experimental.GraphView
{
    public partial class StackNode : IDropTarget
    {
        private bool m_DragEntered;             // Indicates whether a drag has entered
        private IList<VisualElement> m_RemovedPreviews; // The preview being removed
        private IList<VisualElement> m_CurrentPreviews; // The current preview
        private int m_CurrentInsertIndex;       // The current index where the dragged item will be inserted once dropped

        private const string k_PreviewClass = "stack-node-preview";
        private Func<GraphElement, VisualElement> m_DropPreviewTemplate;

        // Animation features are not fully tested or implemented, disabling by setting this to 'true' and all RemovePreview operations to false.
        private bool m_InstantAdd = true; // Temporarily set to true right after an item is detached from the stack to show its preview right away (instead of being animated)

        private List<IValueAnimation> m_AddAnimations;
        private List<IValueAnimation> m_RemoveAnimations;
        private int m_AnimationDuration = 40;
        private static CustomStyleProperty<int> s_AnimationDuration = new CustomStyleProperty<int>("--animation-duration");
        private int animationDuration => m_AnimationDuration;

        private List<GraphElement> m_DraggedElements;

        protected bool dragEntered
        {
            get { return m_DragEntered; }
            private set
            {
                if (m_DragEntered == value)
                    return;

                m_DragEntered = value;
                m_SeparatorContainer.visible = !value; // Hide the separators when while dragging
                m_CurrentInsertIndex = -1;
            }
        }

        public Func<GraphElement, VisualElement> dropPreviewTemplate
        {
            get { return m_DropPreviewTemplate ?? DefaultDropPreviewTemplate; }
            set { m_DropPreviewTemplate = value; }
        }

        private VisualElement CreateDropPreview(GraphElement element)
        {
            VisualElement preview = dropPreviewTemplate(element);
            preview.Add(new Label(element.title));
            preview.AddToClassList(k_PreviewClass);
            return preview;
        }

        private static VisualElement DefaultDropPreviewTemplate(GraphElement source)
        {
            VisualElement preview = new VisualElement();

            preview.AddToClassList("default");

            return preview;
        }

        private void AddPreview(IList<VisualElement> previews, int index, bool animated)
        {
            // Update the current preview with the newly added preview
            m_CurrentPreviews = previews;

            foreach (VisualElement preview in m_CurrentPreviews)
            {
                Insert(index++, preview);
            }

            if (animated)
            {
                m_AddAnimations = new List<IValueAnimation>(m_CurrentPreviews.Count);

                foreach (VisualElement preview in m_CurrentPreviews)
                {
                    Vector2 size = new Vector2(preview.style.width.value.value, preview.style.height.value.value);
                    Rect startRect = new Rect(Vector2.zero, size);
                    Rect endRect = startRect;

                    startRect.height = 0f;

                    var addAnimation = preview.experimental.animation.Start(startRect, endRect, animationDuration, UpdatePreviewLayout).KeepAlive();

                    m_AddAnimations.Add(addAnimation);
                }
            }
            else
            {
                foreach (VisualElement currentPreview in m_CurrentPreviews)
                {
                    currentPreview.style.position = Position.Relative;
                }
            }
        }

        private void RemovePreview(bool animated)
        {
            if (m_CurrentPreviews == null || m_RemovedPreviews != null)
                return;

            if (m_AddAnimations != null)
            {
                foreach (var addAnimation in m_AddAnimations)
                {
                    addAnimation.Stop();
                    addAnimation.Recycle();
                }
                m_AddAnimations = null;
            }

            m_RemovedPreviews = m_CurrentPreviews;

            if (animated)
            {
                m_RemoveAnimations = new List<IValueAnimation>(m_CurrentPreviews.Count);

                foreach (VisualElement preview in m_RemovedPreviews)
                {
                    Rect startRect = new Rect(Vector2.zero, preview.layout.size);
                    Rect endRect = startRect;

                    endRect.height = 0f;

                    var removeAnimations = preview.experimental.animation.Start(startRect, endRect, animationDuration, UpdatePreviewLayout).KeepAlive();
                    removeAnimations.onAnimationCompleted = OnRemoveAnimationFinished;
                    m_RemoveAnimations.Add(removeAnimations);
                }
            }
            else
            {
                RemovePreviewHelper();
            }
        }

        private void RemovePreviewHelper()
        {
            if (m_RemovedPreviews == null)
                return;

            foreach (VisualElement preview in m_RemovedPreviews)
            {
                preview.RemoveFromHierarchy();
            }

            if (m_RemovedPreviews == m_CurrentPreviews)
                m_CurrentPreviews = null;
            m_RemovedPreviews = null;
        }

        private void UpdatePreviewLayout(VisualElement preview, Rect layout)
        {
            if (preview != null)
            {
                preview.style.width = layout.width;
                preview.style.height = layout.height;
            }
        }

        private void OnRemoveAnimationFinished()
        {
            RemovePreviewHelper();
            foreach (var a in m_RemoveAnimations)
            {
                a.Recycle();
            }
            m_RemoveAnimations = null;

            // If a new preview was added while another one was been removed and that a drag exit occurred
            // before the RemoveAnimation finished then removed this added preview
            if (dragEntered == false && m_CurrentPreviews != null)
                RemovePreview(false);
        }

        private void HandleDragAndDropEvent(IMouseEvent evt)
        {
            if ((m_AddAnimations != null && m_AddAnimations.Any(a => a.isRunning)) ||
                (m_RemoveAnimations != null && m_RemoveAnimations.Any(a => a.isRunning)))
                return;

            // Nothing interesting to do if nothing is dragged.
            if (m_DraggedElements == null || m_DraggedElements.Count == 0)
                return;

            dragEntered = true;

            int insertIndex = 0;
            Vector2 localMousePosition = graphView.ChangeCoordinatesTo(contentContainer, graphView.WorldToLocal(evt.mousePosition));
            int previewIndex = m_CurrentPreviews?.FirstOrDefault()?.parent.IndexOf(m_CurrentPreviews.First()) ?? -1;
            int maxIndex = 0;

            // If there is no child then add at index 0
            if (childCount != 0)
            {
                // Determine the insert index by traversing the stack from top to bottom ignoring the previews and footer.
                // For each child element, use the vertical center as reference to determine if the dragged element should be inserted above or below it.
                foreach (VisualElement child in Children())
                {
                    Rect rect = child.layout;

                    if ((m_RemovedPreviews == null || !m_RemovedPreviews.Contains(child)) &&
                        (m_CurrentPreviews == null || !m_CurrentPreviews.Contains(child)))
                    {
                        maxIndex++;
                        if (localMousePosition.y > (rect.y + rect.height / 2))
                        {
                            ++insertIndex;
                        }
                    }
                }
            }

            // Call AcceptsElement to get the first insert index
            AcceptsElement(m_DraggedElements.First(), ref insertIndex, maxIndex);

            // Do nothing if the insert index has not changed
            if (previewIndex == insertIndex)
                return;

            m_CurrentInsertIndex = insertIndex;
            previewIndex = insertIndex;

            // Remove the current preview if there is any with some animation
            RemovePreview(false);

            var previews = new List<VisualElement>();

            foreach (GraphElement draggedElement in m_DraggedElements)
            {
                // Create a preview
                VisualElement preview = CreateDropPreview(draggedElement);

                float previewWidth = contentContainer.layout.width;

                if (!m_InstantAdd)
                {
                    previewWidth = contentContainer.layout.width > 0f
                        ? contentContainer.layout.width
                        : draggedElement.layout.width;
                }

                preview.style.width = previewWidth;
                preview.style.height = draggedElement.layout.height + separatorHeight;
                previews.Add(preview);
            }

            // If there is already a preview being removed then adjust the insert index of the new preview
            if (m_RemovedPreviews != null)
            {
                int removePreviewIndex = m_RemovedPreviews.FirstOrDefault()?.parent.IndexOf(m_RemovedPreviews.First()) ?? -1;

                if (removePreviewIndex < previewIndex)
                {
                    previewIndex += m_RemovedPreviews.Count;
                }
            }

            AddPreview(previews, previewIndex, !m_InstantAdd);
        }

        protected virtual bool hasMultipleSelectionSupport { get { return false; } }

        public bool CanAcceptDrop(List<ISelectable> selection)
        {
            return m_DraggedElements?.Count > 0;
        }

        public virtual bool DragExited()
        {
            dragEntered = false;
            m_DraggedElements = null;
            return true;
        }

        public virtual bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            HandleDragAndDropEvent(evt);

            // Remove the current preview with no animation
            RemovePreview(false);

            if (m_CurrentInsertIndex != -1)
            {
                // Notify the model that an element should be inserted at the specified index
                if (graphView != null && graphView.elementsInsertedToStackNode != null)
                {
                    graphView.elementsInsertedToStackNode(this, m_CurrentInsertIndex, m_DraggedElements);
                }

                int cnt = 0;
                foreach (GraphElement draggedElement in m_DraggedElements)
                {
                    InsertElement(m_CurrentInsertIndex + cnt++, draggedElement);
                }
            }

            dragEntered = false;
            m_DraggedElements = null;

            return true;
        }

        public virtual bool DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            HandleDragAndDropEvent(evt);
            return true;
        }

        public virtual bool DragEnter(DragEnterEvent evt, IEnumerable<ISelectable> selection, IDropTarget enteredTarget, ISelection dragSource)
        {
            int proposedIndex = -1;
            int maxIndex = 1;
            // We just want to know which elements are accepted. The actual index is not relevant to us at this point.
            m_DraggedElements = selection
                .OfType<GraphElement>()
                .Where(e => e != this && AcceptsElementInternal(e, ref proposedIndex, maxIndex))
                .ToList();

            return true;
        }

        public virtual bool DragLeave(DragLeaveEvent evt, IEnumerable<ISelectable> selection, IDropTarget leftTarget, ISelection dragSource)
        {
            // Remove the current preview with some animation
            RemovePreview(false);

            dragEntered = false;
            m_DraggedElements = null;

            return true;
        }
    }
}
