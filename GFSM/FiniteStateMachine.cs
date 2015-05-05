﻿using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;

namespace GFSM {
    /// <summary>
    /// Describes a Deterministic Finite State Machine
    /// </summary>
    /// <typeparam name="T">The type of States this FSM will contain</typeparam>
    public abstract class FiniteStateMachine<T> where T : State<T> {
        #region Properties

        public List<T> States { get; } = new List<T>();

        public IEnumerable<string> Tokens => Transitions.Select(t => t.Token).Distinct();

        public List<Transition<T>> Transitions { get; } = new List<Transition<T>>();

        public T CurrentState => Stack.Count > 0 ? Stack[0] : null;

        private List<T> Stack { get; } = new List<T>();

        #endregion Properties

        #region Events

        /// <summary>
        /// Is fired when a transition successfully completes.
        /// </summary>
        public event Action<Transition<T>> Transitioned;

        #endregion Events

        #region Methods

        /// <summary>
        /// Attempt to trigger a transition with the specified token
        /// </summary>
        /// <param name="token">The token to trigger</param>
        /// <exception cref="ArgumentOutOfRangeException">Token does not exist</exception>
        /// <exception cref="ArgumentOutOfRangeException">No valid transition exists</exception>
        /// <exception cref="ArgumentException">Cannot perform a Pop</exception>
        public void Transition(string token) {
            // First check that we have that token defined
            if (!Tokens.Contains(token))
                throw new ArgumentOutOfRangeException(nameof(token), token, "Given token is not defined.");

            // And that we have a transition for this token
            if (!Transitions.Any(t => t.Token == token && t.From == CurrentState))
                throw new ArgumentOutOfRangeException(nameof(token), token, $"No transition exists for {CurrentState} + {token}.");

            // Get list of possible transitions...
            var possible = Transitions.Where(t => t.From == CurrentState && t.Token == token).ToList();


            // Check validity of transition
            do {
                var transition = possible.MinBy(t =>
                {
                    if (t.TransitionMode != Mode.Pop) return 0;
                    var distanceTo = Stack.IndexOf((T)t.To);
                    return distanceTo == -1 ? int.MaxValue : distanceTo;
                });
                switch (transition.TransitionMode) {
                    case Mode.Pop:
                        var pop = Stack.IndexOf((T)transition.To);
                        if (pop == -1) {
                            possible.Remove(transition);
                            continue;
                        }
                        for (var i = 0; i < pop; i++)
                            Stack.RemoveAt(0);
                        break;

                    case Mode.Push:
                        transition = Transitions.First(t => t.Token == token && t.From == CurrentState);
                        Stack.Insert(0, (T)transition.To);
                        break;

                    case Mode.PushPop:
                        transition = Transitions.First(t => t.Token == token && t.From == CurrentState);
                        if (Stack.Count > 0) Stack.RemoveAt(0);
                        Stack.Insert(0, (T)transition.To);
                        break;
                }

                CurrentState?.Enter();
                Transitioned?.Invoke(transition);
                return;
            } while (Transitions.Count > 0);
        }

        /// <summary>
        /// Add a transition to this FSM
        /// </summary>
        /// <param name="transition">The transition to add, where States includes To and From, and there is no existing transition with the given From and Token</param>
        /// <exception cref="ArgumentException">Thrown if the required States are not present.</exception>
        /// <exception cref="InvalidTransitionException">Thrown if a transition for the given Token and FromState already exists</exception>
        public void AddTransition(Transition<T> transition) {
            // First check that we have the appropriate states
            if ((transition.To != null && !States.Contains(transition.To)) || (transition.From != null && !States.Contains(transition.From)))
                throw new ArgumentException($"Parameter {nameof(transition)} is invalid: {nameof(States)} does not contain the required States.", nameof(transition));

            // Check that the specified transition is not already registered (this is a deterministic FSM)
            //var existingTransition = Transitions.FirstOrDefault(t => t.From == transition.From && t.Token == transition.Token);
            //if (existingTransition != null)
            //    throw new InvalidTransitionException($"Parameter {nameof(transition)} describes an existing transition. ({existingTransition})");

            // All systems are go!
            Transitions.Add(transition);
        }

        #endregion Methods
    }
}