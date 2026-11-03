using Kit.Core.Debugging;
using Kit.Core.Events;
using Kit.Input;
using UnityEngine;

namespace Kit.Gameplay
{
    /// <summary>
    /// First-person CharacterController driven by KitActions.Player (Move, Jump, Sprint, Slide,
    /// Interact). Walking uses eased acceleration curves; slides and chained hops build momentum
    /// on top of that (PlayerController.Movement.cs), and walls can be run along and jumped off
    /// (PlayerController.WallRun.cs). Camera feel (head bob, landing dip, speed-based FOV, slide
    /// eye height, wall-run roll) is in PlayerController.Feel.cs.
    /// Movement is camera-relative when a main camera exists, world-relative otherwise (tests).
    /// Input is ignored until GameStarted, so the player stays put behind the main menu.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed partial class PlayerController : MonoBehaviour
    {
        [SerializeField] PlayerConfig config;
        [Tooltip("Child the camera rigs follow (eye height). Head bob moves this.")]
        [SerializeField] Transform cameraTarget;
        [Tooltip("Body renderers, switched to shadows-only in first person.")]
        [SerializeField] Renderer[] bodyRenderers = new Renderer[0];

        CharacterController controller;
        SpeedRamp ramp;
        CoyoteTimer coyote;
        Vector3 moveDirection;
        float verticalVelocity;
        bool inputEnabled;
        bool wasGrounded;
        bool sprinting;

        public PlayerConfig Config { get => config; set => config = value; }
        public Transform CameraTarget => cameraTarget != null ? cameraTarget : transform;
        public bool InputEnabled => inputEnabled;
        /// <summary>On walkable ground (a slope within the controller's slope limit). Steeper slopes count as air.</summary>
        public bool IsGrounded => onWalkableGround;
        public bool FirstPersonView { get; private set; }

        /// <summary>Horizontal speed this frame after collisions, m/s.</summary>
        public float HorizontalSpeed { get; private set; }

        /// <summary>Current vertical velocity, m/s (positive = up). For tests and debug reports.</summary>
        public float VerticalSpeed => verticalVelocity;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            // The default 0.001 m drops any per-frame step smaller than that. At high frame rates a
            // slow vertical arc (wall run apex, jump apex) moves less than 1 mm per frame and was
            // silently thrown away (seen at 5000 fps in batch mode). Unity's docs advise 0.
            controller.minMoveDistance = 0f;
            InitMovement();
            InitFeel();
        }

        void OnEnable()
        {
            EventBus.Subscribe<GameStarted>(OnGameStarted);
            EventBus.Subscribe<GameOver>(OnGameOver);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<GameStarted>(OnGameStarted);
            EventBus.Unsubscribe<GameOver>(OnGameOver);
        }

        void OnGameStarted(GameStarted _) => inputEnabled = true;
        void OnGameOver(GameOver _) => inputEnabled = false;

        /// <summary>
        /// Called by the camera rig. First person hides the body (it still casts shadows) and
        /// keeps the body facing the camera; other views face the movement direction.
        /// </summary>
        public void SetFirstPersonView(bool firstPerson)
        {
            FirstPersonView = firstPerson;
            var mode = firstPerson ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
            foreach (var r in bodyRenderers) if (r != null) r.shadowCastingMode = mode;
        }

        /// <summary>Moves the player to <paramref name="pose"/> with all momentum cleared (respawn, debug teleport).</summary>
        public void Respawn(Pose pose)
        {
            controller.enabled = false; // a CharacterController overrides direct transform writes
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            controller.enabled = true;
            ResetMotion();
            ResetFeel();
            wasGrounded = false;
        }

        void Update()
        {
            if (config == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // paused

            var actions = KitInput.Actions.Player;
            Vector2 move = inputEnabled ? Vector2.ClampMagnitude(actions.Move.ReadValue<Vector2>(), 1f) : Vector2.zero;
            bool hasInput = move.sqrMagnitude > 0.0001f;
            Vector3 wish = hasInput ? ToWorld(move) : Vector3.zero;
            sprinting = inputEnabled && hasInput && actions.Sprint.IsPressed();
            bool slideHeld = inputEnabled && actions.Slide.IsPressed();
            bool slidePressed = inputEnabled && actions.Slide.WasPressedThisFrame();
            jumpBuffer = inputEnabled && actions.Jump.WasPressedThisFrame() ? config.JumpBufferTime : Mathf.Max(0f, jumpBuffer - dt);

            bool grounded = onWalkableGround;
            coyote.Tick(grounded, config.CoyoteTime, dt);
            Simulate(wish, move.magnitude, move.y, hasInput, slideHeld, slidePressed, grounded, dt);

            float fallSpeed = -verticalVelocity;
            float yBefore = transform.position.y;
            controller.Move((horizontal + Vector3.up * verticalVelocity) * dt);
            ResolveContacts(grounded);
            bool landed = !wasGrounded && onWalkableGround;
            wasGrounded = onWalkableGround;
            if (landed) OnLanded(slideHeld, wish);

            // A step up or down on flat ground pops the body by up to a step height in one frame;
            // the camera eases that out (UpdateFeel) so stairs glide.
            float dy = transform.position.y - yBefore;
            float stepPop = grounded && onWalkableGround && GroundIsFlat && Mathf.Abs(dy) > 0.03f ? dy : 0f;

            UpdateFacing(dt);
            UpdateFeel(dt, landed ? fallSpeed : 0f, stepPop);

            if (inputEnabled && actions.Interact.WasPressedThisFrame())
                EventBus.Publish(new PlayerInteracted(CameraTarget.position, FirstPersonView ? CameraForward() : transform.forward));
            if (DebugOverlay.Visible)
                DebugOverlay.Log("Player", $"{HorizontalSpeed:0.0} m/s {(wallRunning ? "wall" : sliding ? "slide" : surfing ? "surf" : grounded ? "ground" : "air")} chain x{ChainCount} fov {CurrentFieldOfView:0}");
        }

        void UpdateFacing(float dt)
        {
            if (FirstPersonView)
            {
                var forward = CameraForward();
                if (forward.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }
            else if (HorizontalSpeed > 0.01f)
            {
                var facing = Quaternion.LookRotation(horizontal, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, config.TurnSpeed * dt);
            }
        }

        /// <summary>Main camera forward flattened onto the ground plane; world forward if there is no camera.</summary>
        static Vector3 CameraForward()
        {
            var cam = Camera.main;
            if (cam == null) return Vector3.forward;
            var forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.ProjectOnPlane(cam.transform.up, Vector3.up);
            return forward.normalized;
        }

        /// <summary>Maps stick/WASD input onto the ground plane relative to the main camera's yaw.</summary>
        static Vector3 ToWorld(Vector2 input)
        {
            var forward = CameraForward();
            var right = Vector3.Cross(Vector3.up, forward);
            return (forward * input.y + right * input.x).normalized;
        }
    }
}
