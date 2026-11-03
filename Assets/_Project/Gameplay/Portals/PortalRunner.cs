using System.Collections.Generic;
using Kit.Core.Events;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Kit.Gameplay.Portals
{
    /// <summary>
    /// Lives on the hidden Kit host and forwards Unity's per-frame and render callbacks to the
    /// static PortalSystem, so nothing is ever placed in a scene for portals.
    /// </summary>
    [AddComponentMenu("")]
    [DefaultExecutionOrder(10000)] // after CinemachineBrain.LateUpdate (order 0), so selection sees this frame's camera pose
    public sealed class PortalRunner : MonoBehaviour
    {
        void OnEnable()
        {
            PortalSystem.OnHostEnabled();
            EventBus.Subscribe<ThresholdsRepaired>(OnRepaired);
            EventBus.Subscribe<PlayerTeleported>(OnTeleported);
            SceneManager.sceneLoaded += OnSceneLoaded;
            RenderPipelineManager.beginContextRendering += OnBeginContext;
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<ThresholdsRepaired>(OnRepaired);
            EventBus.Unsubscribe<PlayerTeleported>(OnTeleported);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RenderPipelineManager.beginContextRendering -= OnBeginContext;
        }

        void OnDestroy() => PortalSystem.OnHostDestroyed();

        void LateUpdate() => PortalSystem.Tick();

        static void OnRepaired(ThresholdsRepaired e) => PortalSystem.MarkDirty();
        static void OnTeleported(PlayerTeleported e) => PortalSystem.ForgetApproachSides();
        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => PortalSystem.MarkDirty();
        static void OnBeginContext(ScriptableRenderContext context, List<Camera> cameras) => PortalSystem.PlaceViews();
    }
}
