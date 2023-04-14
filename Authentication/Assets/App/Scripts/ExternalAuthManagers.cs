using System;
using UnityEngine;


using Facebook.Unity;

public static class BCFacebook
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
