// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public class VisualElementFocusChangeDirection : FocusChangeDirection
    {
        public static VisualElementFocusChangeDirection kLeft = new VisualElementFocusChangeDirection(FocusChangeDirection.kLastValue + 1);
        public static VisualElementFocusChangeDirection kRight = new VisualElementFocusChangeDirection(FocusChangeDirection.kLastValue + 2);
        protected new static VisualElementFocusChangeDirection kLastValue = kRight;

        protected VisualElementFocusChangeDirection(int value) : base(value)
        {
        }
    }

    public class VisualElementFocusRing : IFocusRing
    {
        public enum DefaultFocusOrder
        {
            ChildOrder,
            PositionXY,
            PositionYX
        }

        public VisualElementFocusRing(VisualContainer root, DefaultFocusOrder dfo = DefaultFocusOrder.ChildOrder)
        {
            defaultFocusOrder = dfo;
            this.root = root;
            m_FocusRing = new List<FocusRingRecord>();
        }

        public VisualContainer root;

        public DefaultFocusOrder defaultFocusOrder { get; set; }

        struct FocusRingRecord
        {
            public int m_AutoIndex;
            public Focusable m_Focusable;
        }

        List<FocusRingRecord> m_FocusRing;

        int FocusRingSort(FocusRingRecord a, FocusRingRecord b)
        {
            if (a.m_Focusable.focusIndex == 0 && b.m_Focusable.focusIndex == 0)
            {
                switch (defaultFocusOrder)
                {
                    case DefaultFocusOrder.ChildOrder:
                    default:
                        return Comparer<int>.Default.Compare(a.m_AutoIndex, b.m_AutoIndex);

                    case DefaultFocusOrder.PositionXY:
                    {
                        VisualElement ave = a.m_Focusable as VisualElement;
                        VisualElement bve = b.m_Focusable as VisualElement;

                        if (ave != null && bve != null)
                        {
                            if (ave.layout.position.x < bve.layout.position.x)
                            {
                                return -1;
                            }
                            else if (ave.layout.position.x > bve.layout.position.x)
                            {
                                return 1;
                            }
                            else
                            {
                                if (ave.layout.position.y < bve.layout.position.y)
                                {
                                    return -1;
                                }
                                else if (ave.layout.position.y > bve.layout.position.y)
                                {
                                    return 1;
                                }
                            }
                        }

                        // a and b should be ordered using their order of appearance.
                        return Comparer<int>.Default.Compare(a.m_AutoIndex, b.m_AutoIndex);
                    }
                    case DefaultFocusOrder.PositionYX:
                    {
                        VisualElement ave = a.m_Focusable as VisualElement;
                        VisualElement bve = b.m_Focusable as VisualElement;

                        if (ave != null && bve != null)
                        {
                            if (ave.layout.position.y < bve.layout.position.y)
                            {
                                return -1;
                            }
                            else if (ave.layout.position.y > bve.layout.position.y)
                            {
                                return 1;
                            }
                            else
                            {
                                if (ave.layout.position.x < bve.layout.position.x)
                                {
                                    return -1;
                                }
                                else if (ave.layout.position.x > bve.layout.position.x)
                                {
                                    return 1;
                                }
                            }
                        }

                        // a and b should be ordered using their order of appearance.
                        return Comparer<int>.Default.Compare(a.m_AutoIndex, b.m_AutoIndex);
                    }
                }
            }
            else if (a.m_Focusable.focusIndex == 0)
            {
                // Only b has a focus index. It has priority.
                return 1;
            }
            else if (b.m_Focusable.focusIndex == 0)
            {
                // Only a has a focus index. It has priority.
                return -1;
            }
            else
            {
                // a and b should be ordered using their focus index.
                return Comparer<int>.Default.Compare(a.m_Focusable.focusIndex, b.m_Focusable.focusIndex);
            }
        }

        void DoUpdate()
        {
            m_FocusRing.Clear();
            if (root != null)
            {
                int fi = 0;
                BuildRingRecursive(root, ref fi);
                m_FocusRing.Sort(FocusRingSort);
            }
        }

        void BuildRingRecursive(VisualContainer vc, ref int focusIndex)
        {
            for (int i = 0; i < vc.childrenCount; i++)
            {
                var child = vc.GetChildAt(i);

                if (child.canGrabFocus)
                {
                    m_FocusRing.Add(new FocusRingRecord
                    {
                        m_AutoIndex = focusIndex++,
                        m_Focusable = child
                    });
                }

                if (child is VisualContainer)
                {
                    BuildRingRecursive(child as VisualContainer, ref focusIndex);
                }
            }
        }

        int GetFocusableInternalIndex(Focusable f)
        {
            if (f != null)
            {
                for (int i = 0; i < m_FocusRing.Count; i++)
                {
                    if (f == m_FocusRing[i].m_Focusable)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public FocusChangeDirection GetFocusChangeDirection(Focusable currentFocusable, EventBase e)
        {
            // FUTURE:
            // We could implement an extendable adapter system to convert event to a focus change direction.
            // This would enable new event sources to change the focus.

            if (currentFocusable as IMGUIContainer != null && e.imguiEvent != null)
            {
                // Let IMGUIContainer manage the focus change.
                return FocusChangeDirection.kNone;
            }

            if (e.GetEventTypeId() == KeyDownEvent.s_EventClassId)
            {
                KeyDownEvent kde = e as KeyDownEvent;
                EventModifiers modifiers = kde.modifiers;

                if (kde.keyCode == KeyCode.Tab)
                {
                    if (currentFocusable == null)
                    {
                        // Dont start going around a focus ring if there is no current focused element.
                        return FocusChangeDirection.kNone;
                    }
                    else if ((modifiers & EventModifiers.Shift) == 0)
                    {
                        return VisualElementFocusChangeDirection.kRight;
                    }
                    else
                    {
                        return VisualElementFocusChangeDirection.kLeft;
                    }
                }
            }

            return FocusChangeDirection.kNone;
        }

        public Focusable GetNextFocusable(Focusable currentFocusable, FocusChangeDirection direction)
        {
            if (direction == FocusChangeDirection.kNone || direction == FocusChangeDirection.kUnspecified)
            {
                return currentFocusable;
            }
            else
            {
                DoUpdate();

                if (m_FocusRing.Count == 0)
                {
                    return null;
                }

                int index = 0;
                if (direction == VisualElementFocusChangeDirection.kRight)
                {
                    index = GetFocusableInternalIndex(currentFocusable) + 1;
                    if (index == m_FocusRing.Count)
                    {
                        index = 0;
                    }
                }
                else if (direction == VisualElementFocusChangeDirection.kLeft)
                {
                    index = GetFocusableInternalIndex(currentFocusable) - 1;
                    if (index == -1)
                    {
                        index = m_FocusRing.Count - 1;
                    }
                }

                return m_FocusRing[index].m_Focusable;
            }
        }
    }
}
