// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


// If you wish to modify this template do so and then regenerate the unity
// events with the command line as shown below:
//
// ./jam GenerateUnityEventClasses

using System;
using System.Reflection;
using UnityEngineInternal;
using UnityEngine.Scripting;
using System.Collections.Generic;

namespace UnityEngine.Events
{
    public delegate void UnityAction<T0, T1, T2>(T0 arg0, T1 arg1, T2 arg2);

    [Serializable]
    public class UnityEvent<T0, T1, T2> : UnityEventBase
    {
        [RequiredByNativeCode]
        public UnityEvent() {}

        public void AddListener(UnityAction<T0, T1, T2> call)
        {
            AddCall(GetDelegate(call));
        }

        public void RemoveListener(UnityAction<T0, T1, T2> call)
        {
            RemoveListener(call.Target, call.Method);
        }

        protected override MethodInfo FindMethod_Impl(string name, Type targetObjType)
        {
            return GetValidMethodInfo(targetObjType, name, new Type[] {typeof(T0), typeof(T1), typeof(T2)});
        }

        internal override BaseInvokableCall GetDelegate(object target, MethodInfo theFunction)
        {
            return new InvokableCall<T0, T1, T2>(target, theFunction);
        }

        private static BaseInvokableCall GetDelegate(UnityAction<T0, T1, T2> action)
        {
            return new InvokableCall<T0, T1, T2>(action);
        }

        private object[] m_InvokeArray = null;
        public void Invoke(T0 arg0, T1 arg1, T2 arg2)
        {
            List<BaseInvokableCall> calls = PrepareInvoke();
            for (var i = 0; i < calls.Count; i++)
            {
                var curCall = calls[i] as InvokableCall<T0, T1, T2>;
                if (curCall != null)
                    curCall.Invoke(arg0, arg1, arg2);
                else
                {
                    var staticCurCall = calls[i] as InvokableCall;
                    if (staticCurCall != null)
                        staticCurCall.Invoke();
                    else
                    {
                        var cachedCurCall = calls[i];
                        if (m_InvokeArray == null)
                            m_InvokeArray = new object[3];
                        m_InvokeArray[0] = arg0; m_InvokeArray[1] = arg1; m_InvokeArray[2] = arg2;
                        cachedCurCall.Invoke(m_InvokeArray);
                    }
                }
            }
        }


        internal void AddPersistentListener(UnityAction<T0, T1, T2> call)
        {
            AddPersistentListener(call, UnityEventCallState.RuntimeOnly);
        }

        internal void AddPersistentListener(UnityAction<T0, T1, T2> call, UnityEventCallState callState)
        {
            var count = GetPersistentEventCount();
            AddPersistentListener();
            RegisterPersistentListener(count, call);
            SetPersistentListenerState(count, callState);
        }

        internal void RegisterPersistentListener(int index, UnityAction<T0, T1, T2> call)
        {
            if (call == null)
            {
                Debug.LogWarning("Registering a Listener requires an action");
                return;
            }

            RegisterPersistentListener(index, call.Target as UnityEngine.Object, call.Method.DeclaringType, call.Method);
        }

    }
}
