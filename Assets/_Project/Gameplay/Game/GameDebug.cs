using UnityEngine;

namespace Kit.Gameplay
{
    /// <summary>
    /// Test hooks for the game flow, meant to be called through MCP execute_code, e.g.
    /// <c>return Kit.Gameplay.GameDebug.Report();</c>. Stateless: finds the live GameManager each call.
    /// </summary>
    public static class GameDebug
    {
        static GameManager Manager => Object.FindAnyObjectByType<GameManager>();

        /// <summary>One-line summary of the game flow state, for checking from outside the Editor.</summary>
        public static string Report()
        {
            var gm = Manager;
            if (gm == null) return "GameManager: none (is Boot loaded and in play mode?)";
            var p = gm.Player;
            return $"state={gm.State} timeScale={Time.timeScale:0.00} player={(p != null ? p.transform.position.ToString("F2") : "none")}";
        }

        /// <summary>Simulates the PlayRequested button press.</summary>
        public static void Play() => Kit.Core.Events.EventBus.Publish(new Kit.Core.Events.PlayRequested());

        /// <summary>Forces a win or loss so the game-over panel and restart path can be checked.</summary>
        public static void ForceGameOver(bool won) => Manager?.EndGame(won);

        /// <summary>Same path as the Restart button.</summary>
        public static void Restart() => Manager?.Restart();
    }
}
