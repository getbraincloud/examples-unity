using System.Collections.Generic;
using UnityEngine;

namespace Gameframework
{
    public class GPoolManager : SingletonBehaviour<GPoolManager>
    {
        #region Private Vars
        private Dictionary<string, GObjectPool> _pools = new Dictionary<string, GObjectPool>();
        #endregion

        #region Public
        /// <summary>
        /// Gets an object from the pool
        /// If the pool does not exist it will be created
        /// </summary>
        /// <param name="template">Object to get</param>
        /// <returns>A clone of the object requested</returns>
        public GameObject Get(GameObject template, bool setActive = true)
        {
            GObjectPool pool = GetOrCreatePool(template);
            GameObject obj = pool.Get(setActive);
            return obj;
        }

        /// <summary>
        /// Gets an object from the pool
        /// If the pool does not exist it will be created
        /// </summary>
        /// <param name="template">Object to get</param>
        /// <param name="position">Position of object</param>
        /// <param name="rotation">Rotation of object</param>
        /// <returns>A clone of the object requested</returns>
        public GameObject Get(GameObject template, Vector3 position, Quaternion rotation, bool setActive = true)
        {
            GObjectPool pool = GetOrCreatePool(template);
            GameObject obj = pool.Get(position, rotation, setActive);
            return obj;
        }

        /// <summary>
        /// Gets an object from the pool
        /// If the pool does not exist it will be created
        /// </summary>
        /// <param name="template">Object to get</param>
        /// <param name="position">Position of object in local space</param>
        /// <param name="rotation">Rotation of object in local space</param>
        /// <param name="parent">Object to set as the parent</param>
        /// <returns>A clone of the object requested</returns>
        public GameObject Get(GameObject template, Vector3 position, Quaternion rotation, Transform parent, bool setActive = true)
        {
            GObjectPool pool = GetOrCreatePool(template);
            GameObject obj = pool.Get(position, rotation, parent, setActive);
            return obj;
        }

        /// <summary>
        /// Fills a pool with the specified number of objects
        /// </summary>
        /// <param name="template">Object to pool</param>
        /// <param name="numToAllocate">Number of objects to allocate</param>
        public void PreallocateObjects(GameObject template, int numToAllocate)
        {
            GObjectPool pool = GetOrCreatePool(template);
            pool.Preallocate(numToAllocate);
        }

        /// <summary>
        /// Destroys any inactive objects in a pool
        /// </summary>
        /// <param name="template">Object who's pool should be cleaned</param>
        public void CleanPool(GameObject template)
        {
            GObjectPool pool;
            if (_pools.TryGetValue(template.name, out pool))
            {
                pool.CleanPool();
            }
        }

        /// <summary>
        /// Destroys all objects in the pool regardless of active state
        /// and destroys the pool
        /// </summary>
        /// <param name="template">Object who's pool should be destroyed</param>
        public void DestroyPool(GameObject template)
        {
            GObjectPool pool;
            if (_pools.TryGetValue(template.name, out pool))
            {
                pool.DestroyPool();
                _pools.Remove(template.name);
            }
        }

        /// <summary>
        /// Destroys all objects in all pools regardless of active state
        /// and destroys the pools
        /// </summary>
        public void DestroyAllPools()
        {
            foreach (var pool in _pools)
            {
                pool.Value.DestroyPool();
            }
            _pools.Clear();
        }
        #endregion

        #region Private
        private GObjectPool GetOrCreatePool(GameObject template)
        {
            GObjectPool pool;
            if (_pools.TryGetValue(template.name, out pool))
                return pool;

            pool = new GObjectPool(this, template);
            _pools[pool.Type] = pool;
            return pool;
        }
        #endregion

        #region Internal Use Public
        /// <summary>
        /// Internal, used by GObjectPool.  Call Get() to retrieve an object from the pool.
        /// </summary>
        public GameObject CreateObject(GameObject template, Vector3 position, Quaternion rotation, Transform parent)
        {
            GameObject obj;

            if (parent)
                obj = Instantiate(template, position, rotation, parent);
            else
                obj = Instantiate(template, position, rotation);

            return obj;
        }

        public void DestroyObject(GameObject obj)
        {
            Destroy(obj);
        }
        #endregion
    }
}