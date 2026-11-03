using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kit.Core.Pooling
{
    /// <summary>
    /// Reuses instances instead of Instantiate/Destroy for anything spawned often (bullets,
    /// particles, pickups). Instantiate spikes GC and frame time; a prewarmed pool does not.
    /// Generic over any class so the bookkeeping is testable without GameObjects.
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        readonly Stack<T> inactive = new Stack<T>();
        readonly Func<T> create;
        readonly Action<T> onGet;
        readonly Action<T> onRelease;

        /// <summary>Instances currently handed out.</summary>
        public int CountActive { get; private set; }

        /// <summary>Instances parked in the pool, ready for Get.</summary>
        public int CountInactive => inactive.Count;

        /// <param name="create">Builds a new instance when the pool is empty.</param>
        /// <param name="prewarm">How many to build up front, so the first spawns cost nothing.</param>
        public ObjectPool(Func<T> create, int prewarm = 0, Action<T> onGet = null, Action<T> onRelease = null)
        {
            this.create = create ?? throw new ArgumentNullException(nameof(create));
            this.onGet = onGet;
            this.onRelease = onRelease;
            for (int i = 0; i < prewarm; i++)
            {
                var item = create();
                onRelease?.Invoke(item);
                inactive.Push(item);
            }
        }

        /// <summary>Takes an instance from the pool, creating one if it is empty.</summary>
        public T Get()
        {
            var item = inactive.Count > 0 ? inactive.Pop() : create();
            CountActive++;
            onGet?.Invoke(item);
            return item;
        }

        /// <summary>Returns an instance to the pool. Do not touch it afterwards.</summary>
        public void Release(T item)
        {
            if (item == null) return;
            onRelease?.Invoke(item);
            inactive.Push(item);
            CountActive = Mathf.Max(0, CountActive - 1);
        }
    }

    /// <summary>Factory helpers for the common case: a pool of prefab instances.</summary>
    public static class ObjectPool
    {
        /// <summary>
        /// Creates a pool of prefab copies parented under <paramref name="parent"/>. Instances are
        /// deactivated while pooled, and PooledBehaviour callbacks fire on spawn/despawn.
        /// </summary>
        public static ObjectPool<T> ForPrefab<T>(T prefab, int prewarm = 0, Transform parent = null) where T : Component
        {
            ObjectPool<T> pool = null;
            pool = new ObjectPool<T>(
                () =>
                {
                    var instance = UnityEngine.Object.Instantiate(prefab, parent);
                    if (instance is PooledBehaviour pooled) pooled.Bind(o => pool.Release((T)(Component)o));
                    return instance;
                },
                prewarm,
                onGet: c =>
                {
                    c.gameObject.SetActive(true);
                    if (c is PooledBehaviour pooled) pooled.OnSpawned();
                },
                onRelease: c =>
                {
                    if (c is PooledBehaviour pooled) pooled.OnDespawned();
                    c.gameObject.SetActive(false);
                });
            return pool;
        }
    }
}
