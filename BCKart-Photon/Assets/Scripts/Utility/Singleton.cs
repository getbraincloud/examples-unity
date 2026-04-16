using System;
using UnityEngine;

namespace FusionExamples.Utility
{
	/// <summary>
	/// Be aware this will not prevent a non singleton constructor
	///   such as `T myT = new T();`
	/// To prevent that, add `protected T () {}` to your singleton class.
	/// 
	/// As a note, this is made as MonoBehaviour because we need Coroutines.
	/// </summary>
	public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		private static T _instance;

		private static readonly object _lock = new object();

		public static T Instance
		{
			get
			{
				if (applicationIsQuitting)
				{
					Debug.LogWarning("[Singleton] Instance '" + typeof(T) + "' already destroyed on application quit. Won't create again - returning null.");
					return null;
				}

				lock (_lock)
				{
					if (_instance == null)
					{
						var all = FindObjectsOfType<T>();
						_instance = all != null && all.Length > 0 ? all[0] : null;

						if (all != null && all.Length > 1)
						{
							Debug.LogWarning("[Singleton] There are " + all.Length + " instances of " + typeof(T) + "... This may happen if your singleton is also a prefab, in which case there is nothing to worry about.");
						}

						if (_instance == null)
						{
							GameObject singleton = new GameObject();
							_instance = singleton.AddComponent<T>();
							singleton.name = "(singleton) " + typeof(T).ToString();

							Debug.Log("[Singleton] An instance of " + typeof(T) + " is needed in the scene, so '" + singleton + "' was created with DontDestroyOnLoad.");
						}
						else
						{
							Debug.Log("[Singleton] Using instance already created: " + _instance.gameObject.name);
						}

						if (Application.isPlaying)
							DontDestroyOnLoad(_instance.gameObject);
					}

					return _instance;
				}
			}
		}

		private static bool applicationIsQuitting = false;

		/// <summary>
		/// When Unity quits, it destroys objects in a random order.
		/// In principle, a Singleton is only destroyed when application quits.
		/// If any script calls Instance after it have been destroyed, 
		///   it will create a buggy ghost object that will stay on the Editor scene
		///   even after stopping playing the Application. Really bad!
		/// So, this was made to be sure we're not creating that buggy ghost object.
		/// </summary>
		public void OnDestroy()
		{
			applicationIsQuitting = true;
		}
	}
}