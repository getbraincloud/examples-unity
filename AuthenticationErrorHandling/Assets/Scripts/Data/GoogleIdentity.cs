using System;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;

public class GoogleIdentity : MonoBehaviour
{
    #if UNITY_ANDROID
    
    private string m_googleId = "";
    private string m_googleToken = "";
    
    private Action<GoogleIdentity> _callback;

    private static GoogleIdentity _googleIdentity;

    public string GoogleId
    {
        get { return m_googleId; }
        set { m_googleId = value; }
    }
    
    public string GoogleToken
    {
        get { return m_googleToken; }
        set { m_googleToken = value; }
    }

    private static GoogleIdentity get()
    {
        if (_googleIdentity == null)
        {
            GameObject go = new GameObject();
            go.AddComponent<GoogleIdentity>();

            _googleIdentity = go.GetComponent<GoogleIdentity>();
        }

        return _googleIdentity;
    }

    public static string GetGoogleToken()
    {
        return get().GoogleToken;
    }
    
    private void InvokeCallback()
    {
        GoogleId = PlayGamesPlatform.Instance.GetUserId();
            
        // Google Token can only be used once. Refresh it on Every API request
        GoogleToken = PlayGamesPlatform.Instance.GetServerAuthCode();
        
        _callback(this);
    }
    
    public static void RefreshGoogleIdentity(Action<GoogleIdentity> callback)
    {
        get()._callback = callback;

        
        PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
            .RequestIdToken()
            //forceRefresh token needs to be set to true; however, this appears to be broken...
            //TODO Find a fix, or wait for one to be made
            .RequestServerAuthCode(false)
            .Build();

        PlayGamesPlatform.InitializeInstance(config);
        PlayGamesPlatform.Activate().Authenticate(success =>
        {
            //Callback must happen after a second, or auth calls for GooglePlay won't work
            //Getting the Token also won't work.
            get().Invoke("InvokeCallback", 3);
        });
    }
    
    #endif
    
}