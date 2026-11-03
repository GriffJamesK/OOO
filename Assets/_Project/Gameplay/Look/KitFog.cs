using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kit.Gameplay.Look
{
    /// <summary>
    /// Linear fog as a Volume override. URP fog lives in Lighting settings, not in Volumes, so a
    /// look preset could not otherwise carry its fog; LookController copies this into RenderSettings.
    /// </summary>
    [Serializable, VolumeComponentMenu("Kit/Fog")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class KitFog : VolumeComponent
    {
        public BoolParameter enabled = new BoolParameter(false);
        public MinFloatParameter start = new MinFloatParameter(20f, 0f);
        public MinFloatParameter end = new MinFloatParameter(120f, 0f);
        public ColorParameter color = new ColorParameter(new Color(0.66f, 0.73f, 0.82f), hdr: false, showAlpha: false, showEyeDropper: true);

        public bool IsActive() => active && enabled.value;
    }
}
