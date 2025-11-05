using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace FusionExamples.FusionHelpers
{
	/// <summary>
	/// Example of a Fusion Object Pool.
	/// The pool keeps a list of available instances by prefab and also a list of which pool each instance belongs to.
	/// </summary>

	public class FusionObjectPoolRoot : NetworkObjectProviderDefault
	{
		private Dictionary<NetworkObjectTypeId, FusionObjectPool> _poolsByPrefab = new Dictionary<NetworkObjectTypeId, FusionObjectPool>();

		public FusionObjectPool GetPool<T>(T prefab) where T : NetworkObject
		{
			FusionObjectPool pool;
			if (!_poolsByPrefab.TryGetValue(prefab.NetworkTypeId, out pool))
			{
				pool = new FusionObjectPool();
				_poolsByPrefab[prefab.NetworkTypeId] = pool;
			}

			return pool;
		}

		public void ClearPools()
		{
			foreach (FusionObjectPool pool in _poolsByPrefab.Values)
			{
				pool.Clear();
			}
			

			_poolsByPrefab = new Dictionary<NetworkObjectTypeId, FusionObjectPool>();
		}
		
		protected override NetworkObject InstantiatePrefab(NetworkRunner runner, NetworkObject prefab)
		{
			FusionObjectPool pool = GetPool(prefab);
			NetworkObject newt = pool.GetFromPool(Vector3.zero, Quaternion.identity);

			if (newt == null)
			{
				newt = Instantiate(prefab, Vector3.zero, Quaternion.identity);
			}

			newt.gameObject.SetActive(true);
			return newt;
		}

		protected override void DestroyPrefabInstance(NetworkRunner runner, NetworkPrefabId prefabId, NetworkObject instance)
		{
			Debug.Log($"Releasing {instance} instance, isSceneObject={instance.NetworkTypeId.IsSceneObject}");
			if (instance != null)
			{
				FusionObjectPool pool;
				if (_poolsByPrefab.TryGetValue(prefabId, out pool))
				{
					pool.ReturnToPool(instance);
					instance.gameObject.SetActive(false); // Should always disable before re-parenting, or we will dirty it twice
					instance.transform.SetParent(transform, false);
				}
				else
				{
					instance.gameObject.SetActive(false); // Should always disable before re-parenting, or we will dirty it twice
					instance.transform.SetParent(null, false);
					Destroy(instance.gameObject);
				}
			}
		}
	}
}