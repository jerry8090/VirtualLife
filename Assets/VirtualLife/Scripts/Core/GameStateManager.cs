using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualLife.Core
{
    /// <summary>
    /// Plain C# state machine (not a MonoBehaviour, so it cannot be duplicated by scene loads).
    /// Owned by CoreServices. Invalid transitions are rejected and logged.
    /// </summary>
    public sealed class GameStateManager
    {
        private static readonly Dictionary<GameState, GameState[]> Allowed = new()
        {
            { GameState.Initializing, new[] { GameState.MainMenu } },
            { GameState.MainMenu,     new[] { GameState.LoadingWorld } },
            { GameState.LoadingWorld, new[] { GameState.Playing, GameState.MainMenu } },
            { GameState.Playing,      new[] { GameState.Paused, GameState.MainMenu } },
            { GameState.Paused,       new[] { GameState.Playing, GameState.MainMenu } },
        };

        public GameState Current { get; private set; } = GameState.Initializing;

        /// <summary>Arguments: previous state, new state.</summary>
        public event Action<GameState, GameState> StateChanged;

        public bool CanTransitionTo(GameState next)
        {
            return next == Current || Array.IndexOf(Allowed[Current], next) >= 0;
        }

        public bool TryTransition(GameState next)
        {
            if (next == Current) return true;
            if (!CanTransitionTo(next))
            {
                Debug.LogWarning($"[GameState] Invalid transition {Current} -> {next} rejected.");
                return false;
            }

            GameState previous = Current;
            Current = next;
            StateChanged?.Invoke(previous, next);
            return true;
        }
    }
}
