namespace Kit.Core.Events
{
    // Prebuilt game-flow events. They live in Core so UI (which references only Core)
    // and Gameplay can both use them without referencing each other.

    /// <summary>Gameplay has begun (first Play, or after a Restart).</summary>
    public readonly struct GameStarted { }

    /// <summary>Pause state changed. UI shows or hides the pause menu from this.</summary>
    public readonly struct GamePaused
    {
        public readonly bool Paused;
        public GamePaused(bool paused) { Paused = paused; }
    }

    /// <summary>The run ended; Won tells the game-over panel which message to show.</summary>
    public readonly struct GameOver
    {
        public readonly bool Won;
        public GameOver(bool won) { Won = won; }
    }

    /// <summary>The player took damage. Amount is in the game's own health units.</summary>
    public readonly struct PlayerDamaged
    {
        public readonly float Amount;
        public PlayerDamaged(float amount) { Amount = amount; }
    }

    /// <summary>The score changed; Score is the new total, not the delta.</summary>
    public readonly struct ScoreChanged
    {
        public readonly int Score;
        public ScoreChanged(int score) { Score = score; }
    }

    // Requests flow the other way: UI asks, the GameManager decides.

    /// <summary>UI asked to start playing from the main menu.</summary>
    public readonly struct PlayRequested { }

    /// <summary>UI asked to reload the play scene and start over.</summary>
    public readonly struct RestartRequested { }

    /// <summary>UI asked to toggle pause (the Resume button) without going through the keyboard.</summary>
    public readonly struct PauseToggleRequested { }

    /// <summary>UI asked to quit the application (exits play mode in the Editor).</summary>
    public readonly struct QuitRequested { }
}
