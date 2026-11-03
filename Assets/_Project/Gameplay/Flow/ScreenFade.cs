using System;

namespace Kit.Gameplay.Flow
{
    /// <summary>
    /// Full-screen fade to and from black that is not UI (the game has no HUD and UI never
    /// references Gameplay). Self-installing on first use. Used by respawns and the ending.
    /// </summary>
    public static class ScreenFade
    {
        /// <summary>Current darkness, 0 = clear, 1 = black.</summary>
        public static float Alpha => 0f; // STUB

        /// <summary>Fades to <paramref name="alpha"/> over <paramref name="duration"/> seconds (unscaled), then calls <paramref name="onDone"/>.</summary>
        public static void FadeTo(float alpha, float duration, Action onDone = null)
        {
            // STUB: implemented by the Flow worker.
            onDone?.Invoke();
        }
    }
}
