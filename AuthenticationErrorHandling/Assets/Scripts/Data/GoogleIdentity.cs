using System;
using AOT;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;

public class GoogleIdentity : MonoBehaviour
{
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

    private void InvokeCallback()
    {
        _callback(this);
    }

    public static void RefreshGoogleIdentity(Action<GoogleIdentity> callback)
    {
        get()._callback = callback;

        PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
            .RequestIdToken()
            .RequestServerAuthCode(false)
            .Build();

        PlayGamesPlatform.InitializeInstance(config);
        PlayGamesPlatform.Activate().Authenticate(success =>
        {
            get().GoogleId = PlayGamesPlatform.Instance.GetUserId();
            get().GoogleToken = PlayGamesPlatform.Instance.GetServerAuthCode();

            //Callback must happen after a second, or auth calls for GooglePlay won't work
            get().Invoke("InvokeCallback", 1);

            callback(get());
        });
    }
}