using UnityEngine;
using System.Collections.Generic;

public class ResourcesManager : GameScene
{

    #region Public
    public GameObject CreateResourceAtPath(string in_path, Transform in_parent = null, bool in_overridePos = true)
    {
        Object tempToReturn = GetResourceAtPath(in_path);
        GameObject toReturn = null;
        if (tempToReturn != null)
        {
            toReturn = Instantiate(tempToReturn, in_parent) as GameObject;
            if (toReturn != null && in_overridePos) toReturn.transform.localPosition = Vector3.zero;
        }

        return toReturn;
    }

    public Object GetResourceAtPath(string in_path)
    {
        Object tempItem = (Object)LazyLoadResourceAtPath(in_path);
        if (tempItem != null)
        {
            return tempItem;
        }
        return null;
    }
    #endregion

    #region Private
    private Object LazyLoadResourceAtPath(string in_toLoad)
    {
        Object toReturn = null;
        bool bContainsKey = m_factoryLookup.ContainsKey(in_toLoad);
        if (!bContainsKey || m_factoryLookup[in_toLoad] == null)
        {
            m_factoryLookup[in_toLoad] = Resources.Load(in_toLoad);
            toReturn = m_factoryLookup[in_toLoad];
        }
        else if (bContainsKey)
        {
            toReturn = m_factoryLookup[in_toLoad];
        }
        return (Object)toReturn;
    }

    private Dictionary<string, Object> m_factoryLookup = new Dictionary<string, Object>();
    #endregion
}
