using UnityEngine;

namespace Kit.Gameplay.Feel
{
    /// <summary>
    /// Marker component on the hidden "[Kit Runtime]" host (KitHost). FeelSystem parents its
    /// particle template and pooled bursts under it, so they survive scene loads and no scene
    /// needs a plumbing object placed. Each burst despawns itself, so the runner needs no Update.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class FeelRunner : MonoBehaviour
    {
    }
}
