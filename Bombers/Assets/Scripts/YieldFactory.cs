using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores static wait variables to avoid allocating new ones
/// </summary>
public static class YieldFactory {

    private static Dictionary<float, WaitForSeconds> _waitForSeconds = new Dictionary<float, WaitForSeconds>();
    private static WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();
    private static WaitForFixedUpdate _waitForFixedUpdate = new WaitForFixedUpdate();

    public static WaitForSeconds GetWaitForSeconds(float seconds)
    {
        WaitForSeconds wait = null;
        if(!_waitForSeconds.TryGetValue(seconds, out wait))
        {
            wait = new WaitForSeconds(seconds);
            _waitForSeconds.Add(seconds, wait);
        }
        return wait;
    }

    public static WaitForEndOfFrame GetWaitForEndOfFrame()
    {
        return _waitForEndOfFrame;
    }

    public static WaitForFixedUpdate GetWaitForFixedUpdate()
    {
        return _waitForFixedUpdate;
    }
}
