using UnityEngine;
using BrainCloud;
using System.Collections.Generic;
using BrainCloudUnity;

public class App : MonoBehaviour
{    
    public static BrainCloudWrapper Bc;

    [SerializeField] public string WrapperName;

    private void Awake()
    {
        Bc = gameObject.AddComponent<BrainCloudWrapper>(); // Create the brainCloud Wrapper
        DontDestroyOnLoad(this); // on an Object that won't be destroyed on Scene Changes

        Bc.WrapperName = WrapperName; // Optional: Add a WrapperName

        Dictionary<string, string> apps = new Dictionary<string, string>();
        apps[BrainCloudSettingsManual.Instance.GameId] = BrainCloudSettingsManual.Instance.SecretKey;
        Bc.InitWithApps(BrainCloudSettingsManual.Instance.DispatcherURL, BrainCloudSettingsManual.Instance.GameId, apps, BrainCloudSettingsManual.Instance.GameVersion);

        //If you want to add additional apps, for parents/child, add it here, the client handles switching correctly

        Bc.Client.EnableLogging(true);
    }
}

//Facebook.dll errors upon opening unity is a known on going bug in Unity but does not interfere with this code base. You can follow information regarding this bug here https://issuetracker.unity3d.com/issues/facebook-editor-throw-errors-related-to-sdk-after-facebook-support-installation