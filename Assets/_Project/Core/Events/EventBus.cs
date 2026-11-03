using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kit.Core.Events
{
    /// <summary>
    /// Type-keyed publish/subscribe hub for struct events. Systems talk through it instead of
    /// holding references to each other, which is what lets one worker own one Gameplay folder
    /// without touching anyone else's code. Struct-only events keep Publish allocation-free.
    /// </summary>
    public static class EventBus
    {
        static readonly Dictionary<Type, Delegate> handlers = new Dictionary<Type, Delegate>();

        /// <summary>Drops every subscription on play-mode enter so listeners from the last session cannot leak in.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear() => handlers.Clear();

        /// <summary>Registers a handler. Pair every Subscribe in OnEnable with an Unsubscribe in OnDisable.</summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            handlers.TryGetValue(typeof(T), out var existing);
            handlers[typeof(T)] = Delegate.Combine(existing, handler);
        }

        /// <summary>Removes a handler previously passed to Subscribe. Safe to call if it was never added.</summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null || !handlers.TryGetValue(typeof(T), out var existing)) return;
            var remaining = Delegate.Remove(existing, handler);
            if (remaining == null) handlers.Remove(typeof(T));
            else handlers[typeof(T)] = remaining;
        }

        /// <summary>
        /// Delivers the event to every current subscriber. Delegates are immutable, so handlers
        /// may safely subscribe or unsubscribe while the event is being delivered.
        /// </summary>
        public static void Publish<T>(T evt) where T : struct
        {
            if (handlers.TryGetValue(typeof(T), out var d))
                ((Action<T>)d).Invoke(evt);
        }

        /// <summary>Number of handlers for T; used by tests and debug reports.</summary>
        public static int SubscriberCount<T>() where T : struct
        {
            return handlers.TryGetValue(typeof(T), out var d) ? d.GetInvocationList().Length : 0;
        }
    }
}
