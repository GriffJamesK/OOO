using UnityEngine;

namespace Kit.Core.Config
{
    /// <summary>
    /// Base for every tunable. Numbers live in ConfigAsset subclasses (saved under Config/),
    /// never as serialized fields on MonoBehaviours, so feel can be tuned in one place, diffed
    /// in git, and swapped per variant without touching prefabs or code.
    /// </summary>
    public abstract class ConfigAsset : ScriptableObject
    {
        /// <summary>Override to clamp values into a sane range. Runs whenever the asset is edited.</summary>
        protected virtual void Sanitize() { }

        /// <summary>Re-applies Sanitize; exposed so tests and code-created configs can enforce the same rules.</summary>
        public void Validate() => Sanitize();

        void OnValidate() => Sanitize();
    }
}
