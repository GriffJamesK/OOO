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

        /// <summary>The registered asset of type T, or a shared default instance.</summary>
        public static T Get<T>() where T : ConfigAsset
        {
            if (registered.TryGetValue(typeof(T), out var found) && found != null) return (T)found;
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
