using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Kit.Gameplay
{
    /// <summary>
    /// Plays a scripted run on a virtual gamepad, so movement routes (slides under shutters, the
    /// surf to the cafeteria balcony, wall runs) are checked with the real input path instead of
    /// by setting velocity. Driven through MCP execute_code, e.g.
    /// <c>Kit.Gameplay.OfficeBot.Run(new[] { OfficeBot.Step.Hold(1f, Vector2.up, sprint: true, yaw: 90f) });</c>
    /// and read back with <see cref="Report"/>. Each step holds its inputs for its duration; a
    /// jump/interact "tap" is pressed for the step's first frame only. The view is set every frame
    /// from the step's yaw/pitch so mouse drift never steers a run.
    /// </summary>
    public sealed class OfficeBot : MonoBehaviour
    {
        /// <summary>One timed slice of input.</summary>
        public struct Step
        {
            public float Seconds;
            public Vector2 Move;
            public bool Sprint, Jump, Slide, Interact;
            public float Yaw, Pitch;

            /// <summary>Hold the stick (and optionally sprint/slide) for a while, facing yaw/pitch; Jump and Interact tap on the first frame.</summary>
            public static Step Hold(float seconds, Vector2 move, bool sprint = false, bool jump = false, bool slide = false,
                bool interact = false, float yaw = float.NaN, float pitch = float.NaN)
                => new Step { Seconds = seconds, Move = move, Sprint = sprint, Jump = jump, Slide = slide, Interact = interact, Yaw = yaw, Pitch = pitch };
        }

        static OfficeBot running;
        static readonly StringBuilder log = new StringBuilder();

        Gamepad pad;
        readonly Queue<Step> steps = new Queue<Step>();
        Step current;
        float left;
        bool firstFrame;
        float maxSpeed;

        /// <summary>True while a script is playing.</summary>
        public static bool Busy => running != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            running = null;
            log.Clear();
        }

        /// <summary>Starts a scripted run (stopping any running one). Needs play mode with a spawned player.</summary>
        public static string Run(IEnumerable<Step> script)
        {
            Stop();
            log.Clear();
            var go = new GameObject("[OOO Bot]");
            running = go.AddComponent<OfficeBot>();
            foreach (var s in script) running.steps.Enqueue(s);
            running.pad = InputSystem.AddDevice<Gamepad>("OfficeBotPad");
            running.Next();
            return "bot started";
        }

        /// <summary>Stops the run and removes the virtual gamepad.</summary>
        public static void Stop()
        {
            if (running != null) Destroy(running.gameObject);
            running = null;
        }

        /// <summary>The run's trace: one line per step with where the player was when it ended, plus the top speed.</summary>
        public static string Report() => (Busy ? "[running] " : "[done] ") + log;

        void Next()
        {
            if (steps.Count == 0) { Finish(); return; }
            current = steps.Dequeue();
            left = current.Seconds;
            firstFrame = true;
        }

        void Finish()
        {
            log.Append($" maxSpeed={maxSpeed:0.0}");
            if (pad != null) InputSystem.QueueStateEvent(pad, new GamepadState());
            Destroy(gameObject);
            running = null;
        }

        void Update()
        {
            if (pad == null) return;
            if (!float.IsNaN(current.Yaw)) OfficeDebug.Look(current.Yaw, float.IsNaN(current.Pitch) ? 0f : current.Pitch);

            var state = new GamepadState { leftStick = current.Move };
            if (current.Sprint) state = state.WithButton(GamepadButton.LeftStick);
            if (current.Slide) state = state.WithButton(GamepadButton.East);
            if (firstFrame && current.Jump) state = state.WithButton(GamepadButton.South);
            if (firstFrame && current.Interact) state = state.WithButton(GamepadButton.West);
            InputSystem.QueueStateEvent(pad, state);
            firstFrame = false;

            var p = FindAnyObjectByType<PlayerController>();
            if (p != null) maxSpeed = Mathf.Max(maxSpeed, p.HorizontalSpeed);

            left -= Time.deltaTime;
            if (left > 0f) return;
            if (p != null) log.Append($"| {p.transform.position:F1} v={p.HorizontalSpeed:0.0} {(p.IsSliding ? "slide" : p.IsSurfing ? "surf" : p.IsWallRunning ? "wall" : p.IsGrounded ? "ground" : "air")} area={AreaZone.Current} ");
            Next();
        }

        void OnDestroy()
        {
            if (pad != null) InputSystem.RemoveDevice(pad);
            pad = null;
        }
    }
}
