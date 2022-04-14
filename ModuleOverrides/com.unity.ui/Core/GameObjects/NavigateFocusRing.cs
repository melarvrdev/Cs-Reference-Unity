// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    internal class NavigateFocusRing : IFocusRing
    {
        public class ChangeDirection : FocusChangeDirection
        {
            public ChangeDirection(int i) : base(i) {}
        }

        public static readonly ChangeDirection Left = new ChangeDirection(1);
        public static readonly ChangeDirection Right = new ChangeDirection(2);
        public static readonly ChangeDirection Up = new ChangeDirection(3);
        public static readonly ChangeDirection Down = new ChangeDirection(4);
        public static readonly FocusChangeDirection Next = VisualElementFocusChangeDirection.right;
        public static readonly FocusChangeDirection Previous = VisualElementFocusChangeDirection.left;

        private readonly VisualElement m_Root;
        private readonly VisualElementFocusRing m_Ring;
        private FocusController focusController => m_Root.focusController;

        public NavigateFocusRing(VisualElement root)
        {
            m_Root = root;
            m_Ring = new VisualElementFocusRing(root);
        }

        public FocusChangeDirection GetFocusChangeDirection(Focusable currentFocusable, EventBase e)
        {
            if (e.eventTypeId == PointerDownEvent.TypeId())
            {
                if (focusController.GetFocusableParentForPointerEvent(e.target as Focusable, out var target))
                    return VisualElementFocusChangeTarget.GetPooled(target);
            }

            if (e.eventTypeId == NavigationMoveEvent.TypeId())
            {
                switch (((NavigationMoveEvent)e).direction)
                {
                    case NavigationMoveEvent.Direction.Left: return Left;
                    case NavigationMoveEvent.Direction.Up: return Up;
                    case NavigationMoveEvent.Direction.Right: return Right;
                    case NavigationMoveEvent.Direction.Down: return Down;
                    case NavigationMoveEvent.Direction.Next: return Next;
                    case NavigationMoveEvent.Direction.Previous: return Previous;
                }
            }

            return FocusChangeDirection.none;
        }

        public virtual Focusable GetNextFocusable(Focusable currentFocusable, FocusChangeDirection direction)
        {
            if (direction == Up || direction == Down || direction == Right || direction == Left)
            {
                return GetNextFocusable2D(currentFocusable, (ChangeDirection)direction);
            }

            return m_Ring.GetNextFocusable(currentFocusable, direction);
        }

        // Searches for a navigable element starting from currentFocusable and scanning along the specified direction.
        // If no elements are found, wraps around from the other side of the panel and scan along the same direction
        // up to currentFocusable. If still no elements are found, returns currentFocusable.
        //
        // Though search order is hierarchy-based, the "best candidate" selection process is intended to be independent
        // from any hierarchy consideration. Scanned elements are validated using an intersection test between their
        // worldBound and a scanning validation rect, currentFocusable's worldBound extended to the panel's limits in
        // the direction of the search (or from the other side during the second "wrap-around" scan).
        //
        // The best candidate is the element whose border is "least advanced" in the scanning direction, using the
        // element's worldBound border opposite from scanning direction, left border when scanning right, right border
        // for scanning left, etc. See FocusableHierarchyTraversal for more details and how further ties are resolved.
        Focusable GetNextFocusable2D(Focusable currentFocusable, ChangeDirection direction)
        {
            if (!(currentFocusable is VisualElement ve))
                ve = m_Root;

            Rect panelBounds = m_Root.worldBoundingBox;
            Rect panelRect = new Rect(panelBounds.position - Vector2.one, panelBounds.size + Vector2.one * 2);
            Rect rect = ve.worldBound;
            Rect validRect = new Rect(rect.position - Vector2.one, rect.size + Vector2.one * 2);
            if (direction == Up) validRect.yMin = panelRect.yMin;
            else if (direction == Down) validRect.yMax = panelRect.yMax;
            else if (direction == Left) validRect.xMin = panelRect.xMin;
            else if (direction == Right) validRect.xMax = panelRect.xMax;

            Focusable best = new FocusableHierarchyTraversal
            {
                currentFocusable = ve,
                direction = direction,
                validRect = validRect,
                firstPass = true
            }.GetBestOverall(m_Root);

            if (best != null)
                return best;

            validRect = new Rect(rect.position - Vector2.one, rect.size + Vector2.one * 2);
            if (direction == Down) validRect.yMin = panelRect.yMin;
            else if (direction == Up) validRect.yMax = panelRect.yMax;
            else if (direction == Right) validRect.xMin = panelRect.xMin;
            else if (direction == Left) validRect.xMax = panelRect.xMax;

            best = new FocusableHierarchyTraversal
            {
                currentFocusable = ve,
                direction = direction,
                validRect = validRect,
                firstPass = false
            }.GetBestOverall(m_Root);

            if (best != null)
                return best;

            return currentFocusable;
        }

        static bool IsActive(VisualElement v)
        {
            return v.resolvedStyle.display != DisplayStyle.None && v.enabledInHierarchy;
        }

        // Valid navigation results are
        // - focusable: canGrabFocus and tabIndex >= 0
        // - leaf: !delegatesFocus and !excludeFromFocusRing
        static bool IsNavigable(Focusable focusable)
        {
            return focusable.canGrabFocus && focusable.tabIndex >= 0 &&
                   !focusable.delegatesFocus && !focusable.excludeFromFocusRing;
        }

        struct FocusableHierarchyTraversal
        {
            public VisualElement currentFocusable;
            public Rect validRect;
            public bool firstPass;
            public ChangeDirection direction;

            bool ValidateHierarchyTraversal(VisualElement v)
            {
                return IsActive(v) && v.worldBoundingBox.Overlaps(validRect);
            }

            bool ValidateElement(VisualElement v)
            {
                return IsNavigable(v) && v.worldBound.Overlaps(validRect);
            }

            int Order(VisualElement a, VisualElement b)
            {
                Rect ra = a.worldBound, rb = b.worldBound;
                int result = StrictOrder(ra, rb);
                return result != 0 ? result : TieBreaker(ra, rb);
            }

            int StrictOrder(VisualElement a, VisualElement b)
            {
                return StrictOrder(a.worldBound, b.worldBound);
            }

            int StrictOrder(Rect ra, Rect rb)
            {
                float diff = 0f;
                if (direction == Up) diff = rb.yMax - ra.yMax;
                else if (direction == Down) diff = ra.yMin - rb.yMin;
                else if (direction == Left) diff = rb.xMax - ra.xMax;
                else if (direction == Right) diff = ra.xMin - rb.xMin;
                if (!Mathf.Approximately(diff, 0f))
                    return diff > 0 ? 1 : -1;
                return 0;
            }

            int TieBreaker(Rect ra, Rect rb)
            {
                // Elements are aligned in the search axis. Find whose top-left corner is closer to current element.
                // TODO: use other corners if grow direction is not left-to-right / top-to-bottom
                Rect rc = currentFocusable.worldBound;
                float diff = (ra.min - rc.min).sqrMagnitude - (rb.min - rc.min).sqrMagnitude;
                if (!Mathf.Approximately(diff, 0f))
                    return diff > 0 ? 1 : -1;

                // Elements probably only differ through numerical rounding errors, so we don't force an order.
                return 0;
            }

            public VisualElement GetBestOverall(VisualElement candidate, VisualElement bestSoFar = null)
            {
                if (!ValidateHierarchyTraversal(candidate))
                    return bestSoFar;

                if (ValidateElement(candidate))
                {
                    if ((!firstPass || StrictOrder(candidate, currentFocusable) > 0) &&
                        (bestSoFar == null || Order(bestSoFar, candidate) > 0))
                        bestSoFar = candidate;

                    return bestSoFar;
                }

                int n = candidate.childCount;
                for (int i = 0; i < n; i++)
                {
                    var child = candidate[i];
                    bestSoFar = GetBestOverall(child, bestSoFar);
                }

                return bestSoFar;
            }
        }
    }
}
