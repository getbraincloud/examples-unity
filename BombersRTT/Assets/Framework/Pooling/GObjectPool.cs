using System.Collections.Generic;
using UnityEngine;

namespace Gameframework
{
    public class GObjectPool
    {
        public string Type { get; private set; }

        #region Private Vars
        private GPoolManager _manager;
        private GameObject _template;
        private List<GameObject> _objects = new List<GameObject>();
        #endregion

        #region Constructor
        public GObjectPool(GPoolManager manager, GameObject template)
        {
            _manager = manager;
            _template = template;
            Type = template.name;
        }
        #endregion

        #region Public
        public void Preallocate(int numToAllocate)
        {
            for (int i = 0; i < numToAllocate; ++i)
            {
                GameObject obj = _manager.CreateObject(_template, Vector3.zero, Quaternion.identity, null);
                obj.SetActive(false);
                _objects.Add(obj);
            }
        }

        public GameObject Get(bool setActive)
        {
            return GetOrCreateObject(Vector3.zero, Quaternion.identity, null, setActive);
        }

        public GameObject Get(Vector3 position, Quaternion rotation, bool setActive)
        {
            return Get(position, rotation, null, setActive);
        }

        public GameObject Get(Vector3 position, Quaternion rotation, Transform parent, bool setActive)
        {
            GameObject obj = GetInactiveObject();

            if (obj == null)
            {
                obj = _manager.CreateObject(_template, position, rotation, parent);
                _objects.Add(obj);
            }

            var trans = obj.transform;
            if (parent)
            {
                trans.SetParent(parent);
                trans.localPosition = position;
                trans.localRotation = rotation;
            }
            else
            {
                trans.position = position;
                trans.rotation = rotation;
            }

            if (setActive) obj.SetActive(true);

            return obj;
        }

        public void CleanPool()
        {
            for (int i = 0; i < _objects.Count; ++i)
            {
                if (_objects[i] != null && !_objects[i].activeInHierarchy)
                {
                    _manager.DestroyObject(_objects[i]);
                    _objects[i] = null;
                }
            }
        }

        public void DestroyPool()
        {
            for (int i = 0; i < _objects.Count; ++i)
            {
                if (_objects[i] != null)
                    _manager.DestroyObject(_objects[i]);
            }
            _objects.Clear();

            _template = null;
            Type = null;
        }
        #endregion

        #region Private
        private GameObject GetOrCreateObject(Vector3 position, Quaternion rotation, Transform parent, bool setActive)
        {
            GameObject obj = GetInactiveObject();

            if (obj == null)
            {
                obj = _manager.CreateObject(_template, position, rotation, parent);
                _objects.Add(obj);
            }

            if (setActive) obj.SetActive(true);

            return obj;
        }

        private GameObject GetInactiveObject()
        {
            for (int i = 0; i < _objects.Count; ++i)
            {
                if (_objects[i] != null && !_objects[i].activeInHierarchy)
                    return _objects[i];
            }
            return null;
        }
        #endregion
    }
}
