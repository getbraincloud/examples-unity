using UnityEngine;
using System.Collections.Generic;

public class App : MonoBehaviour
{    
    public static BrainCloudWrapper Bc;
    string serverUrl = "https://internal.braincloudservers.com/dispatcherv2";//slightly changed
    string secret = "b59b4dc9-f7d2-46cf-a6ee-352dc69cd788";
    string appId = "22901";
    Dictionary<string, string> secretMap = new Dictionary<string, string>();
    string version = "1.0.0";

    [SerializeField] public string WrapperName;

    private void Awake()
    {
        secretMap.Add(appId, secret);
        Bc = gameObject.AddComponent<BrainCloudWrapper>(); // Create the brainCloud Wrapper
        DontDestroyOnLoad(this); // on an Object that won't be destroyed on Scene Changes

        Bc.WrapperName = WrapperName; // Optional: Add a WrapperName
        Bc.InitWithApps(serverUrl, appId, secretMap, version);

        //Bc.InitWithApps();//if used without params extra data, such as: _appId, _secret and _appVersion, is taken from the brainCloud Unity Plugin.

        Bc.Client.EnableLogging(true);
    }
}