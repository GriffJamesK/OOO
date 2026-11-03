using Kit.Core.Events;
using Kit.Input;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kit.Gameplay
{
    /// <summary>Which Cinemachine setup a rig prefab variant uses.</summary>
    public enum CameraRigMode
    {
        ThirdPerson,
        FirstPerson,
        TopDown
    }

    /// <summary>
    /// Glue between the player, KitActions.Look and a CinemachineCamera. Each rig variant prefab
    /// carries the matching Cinemachine body/aim components; this script pushes CameraConfig
    /// values into them and drives their axes from Look, so all input goes through KitActions.
    /// Look and cursor lock are active only while playing.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] CameraRigMode mode = CameraRigMode.ThirdPerson;
        [SerializeField] CameraConfig config;
        [SerializeField] CinemachineCamera virtualCamera;

        bool lookEnabled;
        PlayerController player;

        public CameraRigMode Mode => mode;
        public CameraConfig Config => config;

        void OnEnable()
        {
            EventBus.Subscribe<PlayerSpawned>(OnPlayerSpawned);
            EventBus.Subscribe<GameStarted>(OnGameStarted);
            EventBus.Subscribe<GamePaused>(OnGamePaused);
            EventBus.Subscribe<GameOver>(OnGameOver);
            EventBus.Subscribe<PlayerTeleported>(OnPlayerTeleported);
            ApplyConfig();

            // Rig enabled after the player already spawned (e.g. switching rigs at runtime).
            if (virtualCamera != null && virtualCamera.Follow == null)
            {
                var existing = FindAnyObjectByType<PlayerController>();
                if (existing != null) Attach(existing);
            }
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<PlayerSpawned>(OnPlayerSpawned);
            EventBus.Unsubscribe<GameStarted>(OnGameStarted);
            EventBus.Unsubscribe<GamePaused>(OnGamePaused);
            EventBus.Unsubscribe<GameOver>(OnGameOver);
            EventBus.Unsubscribe<PlayerTeleported>(OnPlayerTeleported);
            SetLook(false);
        }

        void OnPlayerSpawned(PlayerSpawned e)
        {
            var spawned = e.Root != null ? e.Root.GetComponent<PlayerController>() : null;
            if (spawned != null) Attach(spawned);
            else SetTarget(e.CameraTarget);
        }

        /// <summary>
        /// Follows a player and tells it which view it is in. In first person the FOV comes from
        /// PlayerConfig (base + sprint kick), because the spec keeps every feel number there.
        /// </summary>
        void Attach(PlayerController target)
        {
            player = target;
            player.SetFirstPersonView(mode == CameraRigMode.FirstPerson);
            SetTarget(player.CameraTarget);
        }

        void LateUpdate()
        {
            if (mode == CameraRigMode.FirstPerson && player != null && virtualCamera != null)
            {
                virtualCamera.Lens.FieldOfView = player.CurrentFieldOfView;
                virtualCamera.Lens.Dutch = player.CurrentCameraRoll;
            }
        }
        /// <summary>
        /// The player went through a threshold: turn the view by the same rotation (pan and tilt,
        /// roll dropped) and warp Cinemachine by the position jump, so the frame after a crossing
        /// looks exactly like walking on, with no sweep or damping across the level.
        /// </summary>
        void OnPlayerTeleported(PlayerTeleported e)
        {
            if (virtualCamera == null) return;
            if (virtualCamera.Follow != null) virtualCamera.OnTargetObjectWarped(virtualCamera.Follow, e.Delta);
            if (!virtualCamera.TryGetComponent(out CinemachinePanTilt panTilt)) return;

            Vector3 forward = Quaternion.Euler(panTilt.TiltAxis.Value, panTilt.PanAxis.Value, 0f) * Vector3.forward;
            Vector3 mapped = e.Map.MultiplyVector(forward).normalized;
            Vector3 flat = new Vector3(mapped.x, 0f, mapped.z);
            if (flat.sqrMagnitude < 0.0001f) flat = Vector3.ProjectOnPlane(e.Map.MultiplyVector(Quaternion.Euler(0f, panTilt.PanAxis.Value, 0f) * Vector3.forward), Vector3.up);
            if (flat.sqrMagnitude > 0.0001f)
                panTilt.PanAxis.Value = panTilt.PanAxis.ClampValue(Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg);
            float tilt = -Mathf.Asin(Mathf.Clamp(mapped.y, -1f, 1f)) * Mathf.Rad2Deg;
            if (config != null) tilt = Mathf.Clamp(tilt, -config.PitchMax, -config.PitchMin);
            panTilt.TiltAxis.Value = tilt;
        }

        void OnGameStarted(GameStarted _) => SetLook(true);
        void OnGamePaused(GamePaused e) => SetLook(!e.Paused);
        void OnGameOver(GameOver _) => SetLook(false);

        /// <summary>Points the virtual camera at a new target and snaps instead of sweeping across the level.</summary>
        public void SetTarget(Transform target)
        {
            if (virtualCamera == null) return;
            virtualCamera.Follow = target;
            virtualCamera.LookAt = mode == CameraRigMode.FirstPerson ? null : target;
            virtualCamera.PreviousStateIsValid = false;
        }

        void SetLook(bool enabled)
        {
            lookEnabled = enabled && mode != CameraRigMode.TopDown;
            Cursor.lockState = lookEnabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lookEnabled;
        }

        /// <summary>Copies CameraConfig into the Cinemachine components. Also called by the builder in edit mode.</summary>
        public void ApplyConfig()
        {
            if (virtualCamera == null || config == null) return;
            virtualCamera.Lens.FieldOfView = config.FieldOfView;
            var damping = Vector3.one * config.Damping;

            if (virtualCamera.TryGetComponent(out CinemachineOrbitalFollow orbital))
            {
                orbital.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
                orbital.Radius = config.Distance;
                orbital.TrackerSettings.PositionDamping = damping;
                orbital.VerticalAxis.Range = new Vector2(config.PitchMin, config.PitchMax);
                orbital.VerticalAxis.Value = config.StartPitch;
            }
            if (virtualCamera.TryGetComponent(out CinemachinePanTilt panTilt))
            {
                panTilt.TiltAxis.Range = new Vector2(-config.PitchMax, -config.PitchMin);
                panTilt.TiltAxis.Value = -config.StartPitch;
            }
            if (virtualCamera.TryGetComponent(out CinemachineHardLockToTarget hardLock))
                hardLock.Damping = config.Damping;
            if (virtualCamera.TryGetComponent(out CinemachineFollow follow))
            {
                float pitch = config.StartPitch * Mathf.Deg2Rad;
                follow.FollowOffset = new Vector3(0f, Mathf.Sin(pitch), -Mathf.Cos(pitch)) * config.Distance;
                follow.TrackerSettings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
                follow.TrackerSettings.PositionDamping = damping;
            }
            if (virtualCamera.TryGetComponent(out CinemachineRotationComposer composer))
                composer.Damping = new Vector2(config.Damping, config.Damping) * 0.5f;
        }

        void Update()
        {
            if (!lookEnabled || config == null || virtualCamera == null) return;

            var look = KitInput.Actions.Player.Look;
            Vector2 delta = look.ReadValue<Vector2>();
            if (delta == Vector2.zero) return;

            // Mouse reports pixels per frame; sticks report -1..1 and need scaling by time.
            bool gamepad = look.activeControl != null && look.activeControl.device is Gamepad;
            delta *= gamepad ? config.GamepadLookSpeed * Time.unscaledDeltaTime : config.MouseSensitivity;

            if (virtualCamera.TryGetComponent(out CinemachineOrbitalFollow orbital))
            {
                orbital.HorizontalAxis.Value = orbital.HorizontalAxis.ClampValue(orbital.HorizontalAxis.Value + delta.x);
                orbital.VerticalAxis.Value = Mathf.Clamp(orbital.VerticalAxis.Value - delta.y, config.PitchMin, config.PitchMax);
            }
            else if (virtualCamera.TryGetComponent(out CinemachinePanTilt panTilt))
            {
                panTilt.PanAxis.Value = panTilt.PanAxis.ClampValue(panTilt.PanAxis.Value + delta.x);
                panTilt.TiltAxis.Value = Mathf.Clamp(panTilt.TiltAxis.Value - delta.y, -config.PitchMax, -config.PitchMin);
            }
        }
    }
}
