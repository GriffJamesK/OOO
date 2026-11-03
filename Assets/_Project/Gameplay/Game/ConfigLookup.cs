using System;
using System.Collections.Generic;
using Kit.Core.Config;
using UnityEngine;

namespace Kit.Gameplay
{
    /// <summary>
    /// Runtime access to system configs for self-installing systems, which have no inspector
    /// field to receive an asset. GameManager registers GameConfig.Systems on Awake; a system then
    /// asks for its own type. Without a registered asset (edit-mode and isolated play-mode tests)
    /// a default instance is created once, so systems always get the values written in their class.
    /// </summary>
    public static class ConfigLookup
    {
        static readonly Dictionary<Type, ConfigAsset> registered = new Dictionary<Type, ConfigAsset>();
        static readonly Dictionary<Type, ConfigAsset> defaults = new Dictionary<Type, ConfigAsset>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            registered.Clear();
            defaults.Clear();
        }

        /// <summary>Makes these assets the answer for their types (later registrations win).</summary>
        public static void Register(IEnumerable<ConfigAsset> configs)
        {
            if (configs == null) return;
            foreach (var c in configs) if (c != null) registered[c.GetType()] = c;
        }

        /// <summary>
        /// The registered asset of type T; in the Editor, the project's Config/&lt;TypeName&gt;.asset
        /// (so builders running in edit mode get real materials and values); otherwise a shared
        /// default instance.
        /// </summary>
        public static T Get<T>() where T : ConfigAsset
        {
            // Registrations come from play mode (GameManager); with domain reload off they outlive it,
            // so outside play mode they could point at assets a builder has since replaced. Edit mode
            // therefore always reads the asset on disk.
            if (Application.isPlaying && registered.TryGetValue(typeof(T), out var found) && found != null) return (T)found;
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>($"Assets/_Project/Config/{typeof(T).Name}.asset");
            if (asset != null) return asset;
#endif
            if (registered.TryGetValue(typeof(T), out var registeredAsset) && registeredAsset != null) return (T)registeredAsset;
            if (!defaults.TryGetValue(typeof(T), out var fallback) || fallback == null)
            {
                fallback = ScriptableObject.CreateInstance<T>();
                fallback.hideFlags = HideFlags.DontSave;
                fallback.Validate();
                defaults[typeof(T)] = fallback;
            }
            return (T)fallback;
        }
    }
}
