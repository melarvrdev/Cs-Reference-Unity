// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public abstract class ContextualMenuManager
    {
        public abstract void DisplayMenuIfEventMatches(EventBase evt, IEventHandler eventHandler);

        public void DisplayMenu(EventBase triggerEvent, IEventHandler target)
        {
            ContextualMenu menu = new ContextualMenu();
            using (ContextualMenuPopulateEvent cme = ContextualMenuPopulateEvent.GetPooled(triggerEvent, menu, target, this))
            {
                UIElementsUtility.eventDispatcher.DispatchEvent(cme, null);
            }
        }

        protected internal abstract void DoDisplayMenu(ContextualMenu menu, EventBase triggerEvent);
    }
}
