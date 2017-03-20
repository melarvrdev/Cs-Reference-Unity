// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.RMGUI
{
    // value that determines if a event handler stops propagation of events or allows it to continue.
    public enum  EventPropagation
    {
        // continue event propagation after this handler
        Continue,
        // stop event propagation after this handler
        Stop
    }

    // determines in which event phase an event handler wants to handle events
    // the handler always gets called if it is the target VisualElement
    public enum EventPhase
    {
        // propagation from root of tree to immediate parent of target
        Capture,

        // Target, The second phase is implicit, when the widget is the target it always gets the event.
        // for key event, target is the keyboardFocus or panel root if none.

        // after the target has gotten the chance to handle the event the event walks back up the parent hierarchy back to root
        BubbleUp
    }

    // With the following VisualElement tree existing
    // root
    //  container A
    //      button B
    //      Textfield C with KeyboardFocus  <-- Event 2 Key Down A
    //  container D
    //      container E
    //          button F  <-- Event 1 Click
    //
    // For example: In the case of Event 1 Button F getting clicked, the following handlers will be called if registered:
    // result ==> Phase Capture [ root, D, E ], Phase Target [F], Phase BubbleUp [ E, D, root ]
    //
    // For example 2: A keydown with Textfocus in TextField C
    // result ==> Phase Capture [ root, A], Phase Target [C], Phase BubbleUp [ A, root ]

    // TODO this interface is going to be refactored in a later iteration
    public interface IEventHandler
    {
        // return Stop to stop event propagation to other handlers
        EventPropagation HandleEvent(Event evt, VisualElement finalTarget);
        IPanel      panel { get; }
        EventPhase  phaseInterest { get; set; }
        void OnLostCapture();
        void OnLostKeyboardFocus();
    }

    public static class EventHandlerExtensions
    {
        public static void TakeCapture(this IEventHandler handler)
        {
            if (handler.panel != null)
            {
                handler.panel.dispatcher.TakeCapture(handler);
                handler.panel.dispatcher.ReleaseKeyboardFocus();
            }
        }

        public static bool HasCapture(this IEventHandler handler)
        {
            if (handler.panel != null)
            {
                return handler.panel.dispatcher.capture == handler;
            }
            else
            {
                return false;
            }
        }

        public static void ReleaseCapture(this IEventHandler handler)
        {
            if (handler.panel != null)
            {
                handler.panel.dispatcher.ReleaseCapture(handler);
            }
        }

        public static void RemoveCapture(this IEventHandler handler)
        {
            if (handler.panel != null)
            {
                handler.panel.dispatcher.RemoveCapture();
            }
        }

        public static void TakeKeyboardFocus(this IEventHandler handler, VisualElement element)
        {
            if (handler.panel != null)
            {
                handler.panel.dispatcher.TakeKeyboardFocus(element);
            }
        }

        public static void TakeKeyboardFocus(this IEventHandler handler)
        {
            if (handler.panel != null)
            {
                var element = handler as VisualElement;
                if (element != null)
                {
                    handler.panel.dispatcher.TakeKeyboardFocus(element);
                }
            }
        }

        public static bool HasKeyboardFocus(this IEventHandler handler, VisualElement element)
        {
            if (handler.panel != null)
            {
                return handler.panel.dispatcher.keyboardFocus == element;
            }
            return false;
        }

        public static bool HasKeyboardFocus(this IEventHandler handler)
        {
            if (handler.panel != null)
            {
                var element = handler as VisualElement;
                if (element != null)
                {
                    return handler.panel.dispatcher.keyboardFocus == element;
                }
            }
            return false;
        }

        public static void ReleaseKeyboardFocus(this IEventHandler handler)
        {
            if (handler.panel != null)
            {
                handler.panel.dispatcher.ReleaseKeyboardFocus();
            }
        }

        public static void RemoveKeyboardFocus(this IEventHandler handler)
        {
            if (handler.panel != null)
            {
                handler.panel.dispatcher.RemoveKeyboardFocus();
            }
        }

        public static ScheduleBuilder Schedule(this IEventHandler handler, Action<TimerState> timerUpdateEvent)
        {
            if (handler.panel == null || handler.panel.scheduler == null)
            {
                Debug.LogError("Cannot schedule an event without a valid panel");
                return new ScheduleBuilder();
            }

            return handler.panel.scheduler.Schedule(timerUpdateEvent, handler);
        }

        public static void Unschedule(this IEventHandler handler, Action<TimerState> timerUpdateEvent)
        {
            if (handler.panel == null || handler.panel.scheduler == null)
            {
                Debug.LogError("Cannot unschedule an event without a valid panel");
                return;
            }

            handler.panel.scheduler.Unschedule(timerUpdateEvent);
        }
    }

    public interface IDispatcher
    {
        IEventHandler capture { get; }

        // TODO: FIX: IMGUI remembers keyboardFocus per GUIObjectState
        // BUG: RMGUI currently "forgets" when switching panel
        VisualElement keyboardFocus { get; set; }

        // used when the capture is done receiving events
        void ReleaseCapture(IEventHandler handler);

        // removed a capture without it being done (will invoke OnLostCapture())
        void RemoveCapture();

        // use to set a capture. if any capture is set, it will be removed
        void TakeCapture(IEventHandler handler);

        // used when the keyboard focus is done receiving events
        void ReleaseKeyboardFocus();

        // removed a keyboard focus without it being done (will invoke OnLostCapture())
        void RemoveKeyboardFocus();

        // use to set keyboard focus. if any keyboard focus is set, it will be removed
        void TakeKeyboardFocus(VisualElement element);
    }

    internal class EventDispatcher : IDispatcher
    {
        // 0. global capture
        public IEventHandler capture { get; set; }

        public void ReleaseCapture(IEventHandler handler)
        {
            Debug.Assert(handler == capture, "Element releasing capture does not have capture");
            capture = null;
        }

        public void RemoveCapture()
        {
            if (capture != null)
            {
                capture.OnLostCapture();
            }
            capture = null;
        }

        public void TakeCapture(IEventHandler handler)
        {
            if (capture == handler)
                return;
            if (GUIUtility.hotControl != 0)
            {
                Debug.Log("Should not be capturing when there is a hotcontrol");
                return;
            }
            // TODO: assign a reserved control id to hotControl so that repaint events in OnGUI() have their hotcontrol check behave normally
            RemoveCapture();
            capture = handler;
        }

        // 1. keyboard capture
        public VisualElement keyboardFocus { get; set; }

        public void ReleaseKeyboardFocus()
        {
            keyboardFocus = null;
        }

        public void RemoveKeyboardFocus()
        {
            if (keyboardFocus != null)
            {
                keyboardFocus.OnLostKeyboardFocus();
            }
            keyboardFocus = null;
        }

        public void TakeKeyboardFocus(VisualElement element)
        {
            if (keyboardFocus == element)
                return;

            RemoveKeyboardFocus();
            keyboardFocus = element;
        }

        public EventPropagation DispatchEvent(Event e, IVisualElementPanel panel)
        {
            if (e.type == EventType.Repaint)
            {
                Debug.Log("Repaint should be handled by Panel before Dispatcher");
                return EventPropagation.Continue;
            }

            bool invokedHandleEvent = false;

            if (capture != null && capture.panel == null)
            {
                Debug.Log(string.Format("Capture has no panel, forcing removal (capture={0} eventType={1})", capture, e.type));
                RemoveCapture();
            }

            if (capture != null)
            {
                if (capture.panel.contextType != panel.contextType)
                    return EventPropagation.Continue;

                var ve = capture as VisualElement;
                if (ve != null)
                {
                    e.mousePosition = ve.GlobalToBound(e.mousePosition);
                }
                else
                {
                    var m = capture as IManipulator;
                    if (m != null)
                    {
                        e.mousePosition = m.target.GlobalToBound(e.mousePosition);
                    }
                }
                invokedHandleEvent = true;
                if (capture.HandleEvent(e, capture as VisualElement) == EventPropagation.Stop)
                    return EventPropagation.Stop;
            }

            // 1. Keyboard?
            if (e.isKey)
            {
                invokedHandleEvent = true;
                if (keyboardFocus != null)
                {
                    if (PropagateToKeyboardFocus(keyboardFocus, e, panel) == EventPropagation.Stop)
                        return EventPropagation.Stop;
                }
                else
                {
                    // do the old behavior here, propagate to all IMGUI Container widgets
                    if (PropagateToIMGUIContainer(panel.visualTree, e) == EventPropagation.Stop)
                        return EventPropagation.Stop;
                }
            }
            else if (e.isMouse
                     || e.isScrollWheel
                     || e.type == EventType.DragUpdated
                     || e.type == EventType.DragPerform
                     || e.type == EventType.DragExited)
            {
                invokedHandleEvent = true;

                // 3. General dispatch
                EventPropagation result = EventPropagation.Continue;

                var target = panel.Pick(e.mousePosition);

                if (e.type == EventType.MouseDown &&
                    keyboardFocus != null &&
                    keyboardFocus != target)
                {
                    RemoveKeyboardFocus();
                    if (target != null)
                        result = EventPropagation.Stop;
                }

                if (target != null)
                {
                    result = PropagateEvent(target, e);
                }

                // TODO: Replace this by focus support
                if (e.type == EventType.MouseDown)
                {
                    TakeKeyboardFocus(target);
                    if (target == null)
                        result = EventPropagation.Stop;
                }

                return result;
            }

            if (e.type == EventType.ExecuteCommand
                || e.type == EventType.ValidateCommand
                )
            {
                invokedHandleEvent = true;
                // first try to propagate to focused element
                if (keyboardFocus != null && PropagateToKeyboardFocus(keyboardFocus, e, panel) == EventPropagation.Stop)
                {
                    return EventPropagation.Stop;
                }

                // do the old behavior here, propagate to all IMGUI Container widgets
                if (PropagateToIMGUIContainer(panel.visualTree, e) == EventPropagation.Stop)
                    return EventPropagation.Stop;
            }

            // It might happen in the editor that dummy events are sent to run some initialize code
            // Pass there to IMGUIContainers (note if any IMGUIContainer exist will get Stop)
            if (e.type == EventType.Used)
            {
                invokedHandleEvent = true;
                if (PropagateToIMGUIContainer(panel.visualTree, e) == EventPropagation.Stop)
                    return EventPropagation.Stop;
            }

            if (!invokedHandleEvent)
            {
                Debug.LogWarning("Fall back on IMGUI propagation for " + e);

                if (PropagateToIMGUIContainer(panel.visualTree, e) == EventPropagation.Stop)
                    return EventPropagation.Stop;
            }

            return EventPropagation.Continue;
        }

        private EventPropagation PropagateToIMGUIContainer(VisualElement root, Event evt)
        {
            var imContainer = root as IMGUIContainer;
            if (imContainer != null)
            {
                // only dispatches to IMGUIContainer, and returns if one handles
                // do not enter container to dispatch to children
                if (imContainer.HandleEvent(evt, imContainer) == EventPropagation.Stop)
                    return EventPropagation.Stop;
                return EventPropagation.Continue;
            }

            var container = root as VisualContainer;
            if (container != null)
            {
                for (int i = 0; i < container.childrenCount; i++)
                {
                    if (PropagateToIMGUIContainer(container.GetChildAt(i), evt) == EventPropagation.Stop)
                        return EventPropagation.Stop;
                }
            }
            return EventPropagation.Continue;
        }

        private EventPropagation PropagateEvent(VisualElement target, Event evt)
        {
            var path = BuildPropagationPath(target);

            var worldMouse = evt.mousePosition;

            // Phase 1: Down phase
            // from root to target.parent
            for (int i = 0; i < path.Count; i++)
            {
                var v = path[i];
                if (v.enabled)
                {
                    evt.mousePosition = v.GlobalToBound(worldMouse);
                    // manipulators
                    var enumerator = v.GetManipulatorsInternal();
                    while (enumerator.MoveNext())
                    {
                        var m = enumerator.Current;
                        if (m.phaseInterest == EventPhase.Capture
                            && m.HandleEvent(evt, target) == EventPropagation.Stop)
                            return EventPropagation.Stop;
                    }
                    // Do path
                    if (v.phaseInterest == EventPhase.Capture
                        && v.HandleEvent(evt, target) == EventPropagation.Stop)
                        return EventPropagation.Stop;
                }
            }

            // Phase 2: Target
            if (target.enabled)
            {
                evt.mousePosition = target.GlobalToBound(worldMouse);

                // Do capture phase manipulators first
                var enumerator = target.GetManipulatorsInternal();
                while (enumerator.MoveNext())
                {
                    var m = enumerator.Current;
                    if (m.phaseInterest == EventPhase.Capture
                        && m.HandleEvent(evt, target) == EventPropagation.Stop)
                        return EventPropagation.Stop;
                }

                // Do Target
                if (target.HandleEvent(evt, target) == EventPropagation.Stop)
                    return EventPropagation.Stop;

                // do BubbleUp manipulators last
                enumerator = target.GetManipulatorsInternal();
                while (enumerator.MoveNext())
                {
                    var m = enumerator.Current;
                    if (m.phaseInterest == EventPhase.BubbleUp
                        && m.HandleEvent(evt, target) == EventPropagation.Stop)
                        return EventPropagation.Stop;
                }
            }

            // Phase 3: bubble Up phase
            // from leaf.parent back to root
            for (int i = path.Count - 1; i >= 0; i--)
            {
                var v = path[i];
                if (v.enabled)
                {
                    evt.mousePosition = v.GlobalToBound(worldMouse);

                    // Do path
                    if (v.phaseInterest == EventPhase.BubbleUp
                        && v.HandleEvent(evt, target) == EventPropagation.Stop)
                        return EventPropagation.Stop;

                    // manipulators
                    var enumerator = v.GetManipulatorsInternal();
                    while (enumerator.MoveNext())
                    {
                        var m = enumerator.Current;
                        if (m.phaseInterest == EventPhase.BubbleUp
                            && m.HandleEvent(evt, target) == EventPropagation.Stop)
                            return EventPropagation.Stop;
                    }
                }
            }
            return EventPropagation.Continue;
        }

        private EventPropagation PropagateToKeyboardFocus(VisualElement element, Event e, IVisualElementPanel panel)
        {
            // Skip this event if the currently focused element does belong to the panel we are workinbg with.
            // This fixes some situation where command events are sent to all panel until handled
            // but some global state isn't properly set up for the IMGUIContainer being focus
            if (element is IMGUIContainer && element.panel != panel)
            {
                return EventPropagation.Continue;
            }

            return PropagateEvent(element, e);
        }

        private List<VisualElement> BuildPropagationPath(VisualElement elem)
        {
            var ret = new List<VisualElement>();
            if (elem == null)
                return ret;

            while (elem.parent != null)
            {
                ret.Insert(0, elem.parent);
                elem = elem.parent;
            }
            return ret;
        }
    }
}
