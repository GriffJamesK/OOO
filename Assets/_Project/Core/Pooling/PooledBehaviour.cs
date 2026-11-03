using System;
using UnityEngine;

namespace Kit.Core.Pooling
{
    /// <summary>
    /// Base for anything spawned from ObjectPool.ForPrefab. Pooled objects are never Awake'd or
    /// Start'ed again, so reset state in OnSpawned instead, and call Despawn() rather than Destroy.
    /// </summary>
    public abstract class PooledBehaviour : MonoBehaviour
    {
        Action<PooledBehaviour> returnToPool;

        internal void Bind(Action<PooledBehaviour> release) => returnToPool = release;

        /// <summary>Called each time the instance leaves the pool. Reset health, velocity, timers here.</summary>
        public virtual void OnSpawned() { }

        /// <summary>Called each time the instance goes back into the pool, before it is deactivated.</summary>
        public virtual void OnDespawned() { }

        /// <summary>Returns this instance to its pool, or destroys it if it was not spawned from one.</summary>
        public void Despawn()
        {
            if (returnToPool != null) returnToPool(this);
            else Destroy(gameObject);
        }
    }
}
