using UnityEngine;
using System.Collections;
public static class UIContainerCache
{
    private static Transform _cursorContainer;

    public static Transform GetCursorContainer()
    {
        if (_cursorContainer == null)
        {
            GameObject containerGO = GameObject.Find("CursorContainer");
            if (containerGO != null)
                _cursorContainer = containerGO.transform;
            else
                Debug.LogWarning("[UIContainerCache] CursorContainer not found in scene.");
        }

        return _cursorContainer;
    }
}