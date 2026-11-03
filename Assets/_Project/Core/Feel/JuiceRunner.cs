using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kit.Core.Feel
{
    /// <summary>
    /// Runtime side of Juice: tracks active camera shakes and the hit-stop timer. Added to the
    /// Kit host on first use, so scenes never need it placed.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class JuiceRunner : MonoBehaviour
    {
        struct Shake
        {
            public float Amplitude, Duration, EndTime;
            public Vector3 Applied;
        }

        const float HitStopScale = 0.02f;

        readonly Dictionary<Camera, Shake> shakes = new Dictionary<Camera, Shake>();
        float hitStopEnd;
        float timeScaleBeforeHitStop = 1f;
        bool hitStopping;

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            RenderPipelineManager.endCameraRendering += OnEndCamera;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            RenderPipelineManager.endCameraRendering -= OnEndCamera;
            if (hitStopping) { Time.timeScale = timeScaleBeforeHitStop; hitStopping = false; }
        }

        internal void AddShake(Camera cam, float amplitude, float duration)
        {
            // A stronger or longer new shake wins; a weaker one does not cut an existing one short.
            shakes.TryGetValue(cam, out var s);
            s.Amplitude = Mathf.Max(amplitude, s.Amplitude);
            s.Duration = Mathf.Max(0.0001f, duration);
            s.EndTime = Mathf.Max(s.EndTime, Time.unscaledTime + duration);
            shakes[cam] = s;
        }

        internal void HitStop(float seconds)
        {
            if (!hitStopping)
            {
                timeScaleBeforeHitStop = Time.timeScale;
                hitStopping = true;
            }
            hitStopEnd = Mathf.Max(hitStopEnd, Time.unscaledTime + seconds);
            Time.timeScale = HitStopScale;
        }

        void Update()
        {
            if (hitStopping && Time.unscaledTime >= hitStopEnd)
            {
                hitStopping = false;
                Time.timeScale = timeScaleBeforeHitStop;
            }
        }

        void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
        {
            if (!shakes.TryGetValue(cam, out var s)) return;
            float remaining = s.EndTime - Time.unscaledTime;
            if (remaining <= 0f) { shakes.Remove(cam); return; }
            float falloff = Mathf.Clamp01(remaining / s.Duration);
            s.Applied = Random.insideUnitSphere * (s.Amplitude * falloff);
            cam.transform.position += s.Applied;
            shakes[cam] = s;
        }

        void OnEndCamera(ScriptableRenderContext ctx, Camera cam)
        {
            if (!shakes.TryGetValue(cam, out var s)) return;
            cam.transform.position -= s.Applied;
            s.Applied = Vector3.zero;
            shakes[cam] = s;
        }
    }
}
