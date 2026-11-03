using System;
using System.Collections.Generic;
using Kit.Core.Events;
using UnityEngine;

namespace Kit.Core.FSM
{
    /// <summary>Published through the EventBus on every Transition, so other systems react to state without polling.</summary>
    public readonly struct StateChanged<TState> where TState : struct, Enum
    {
        public readonly string Machine;
        public readonly TState Previous;
        public readonly TState Current;

        public StateChanged(string machine, TState previous, TState current)
        {
            Machine = machine; Previous = previous; Current = current;
        }
    }

    /// <summary>
    /// Enum-keyed finite state machine. Enum keys keep states greppable and switch-friendly;
    /// callbacks per state keep the logic next to the state instead of in one giant Update.
    /// Call Tick from the owner's Update and Dispose from OnDestroy.
    /// </summary>
    public sealed class StateMachine<TState> : IStateMachineInfo, IDisposable where TState : struct, Enum
    {
        sealed class StateCallbacks
        {
            public Action Enter, Exit;
            public Action<float> Tick;
        }

        readonly Dictionary<TState, StateCallbacks> states = new Dictionary<TState, StateCallbacks>();

        public string Name { get; }
        public TState Current { get; private set; }
        public bool HasStarted { get; private set; }
        string IStateMachineInfo.CurrentName => HasStarted ? Current.ToString() : "-";

        /// <param name="name">Shown in the DebugOverlay and carried on StateChanged events.</param>
        public StateMachine(string name)
        {
            Name = name;
            StateMachineRegistry.Register(this);
        }

        /// <summary>Registers callbacks for a state. Any of them may be null. Returns this for chaining.</summary>
        public StateMachine<TState> Configure(TState state, Action enter = null, Action exit = null, Action<float> tick = null)
        {
            states[state] = new StateCallbacks { Enter = enter, Exit = exit, Tick = tick };
            return this;
        }

        /// <summary>
        /// Exits the current state, enters <paramref name="next"/>, then publishes StateChanged.
        /// The first call has no Exit. Transitioning to the current state re-runs Exit/Enter.
        /// </summary>
        public void Transition(TState next)
        {
            var previous = Current;
            if (HasStarted && states.TryGetValue(Current, out var from)) from.Exit?.Invoke();
            Current = next;
            HasStarted = true;
            if (states.TryGetValue(next, out var to)) to.Enter?.Invoke();
            EventBus.Publish(new StateChanged<TState>(Name, previous, next));
        }

        /// <summary>Runs the current state's tick callback.</summary>
        public void Tick(float deltaTime)
        {
            if (HasStarted && states.TryGetValue(Current, out var s)) s.Tick?.Invoke(deltaTime);
        }

        /// <summary>Removes this machine from the debug overlay listing.</summary>
        public void Dispose() => StateMachineRegistry.Unregister(this);
    }

    /// <summary>Non-generic view of a state machine, so the DebugOverlay can list machines of any enum type.</summary>
    public interface IStateMachineInfo
    {
        string Name { get; }
        string CurrentName { get; }
    }

    /// <summary>Live state machines, read by the DebugOverlay.</summary>
    public static class StateMachineRegistry
    {
        static readonly List<IStateMachineInfo> machines = new List<IStateMachineInfo>();

        public static IReadOnlyList<IStateMachineInfo> All => machines;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => machines.Clear();

        internal static void Register(IStateMachineInfo m) { if (!machines.Contains(m)) machines.Add(m); }
        internal static void Unregister(IStateMachineInfo m) => machines.Remove(m);
    }
}
