using Facebook.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FacebookManager
{
    public static void Initialize(Action<bool> onHideUnity)
    {
        if (!FB.IsInitialized)
        {
            FB.Init(OnInitComplete, (isGameShown) => onHideUnity(isGameShown));
        }
        else
        {
            OnInitComplete();
        }
    }

    private static void OnInitComplete()
    {
        if (FB.IsInitialized)
        {
            FB.ActivateApp();
            FB.LimitAppEventUsage = true;
        }
        else
        {
            Debug.LogError("Failed to Initialize the Facebook SDK!");
        }
    }
}
