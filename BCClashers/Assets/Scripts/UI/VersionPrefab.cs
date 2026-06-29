
using BrainCloud.JsonFx.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VersionPrefab : MonoBehaviour
{
    public static VersionPrefab Instance;

    [SerializeField]
    private BrainCloudWrapper _BCWrapper;

    [SerializeField] private TMP_Text _appIDText, _appVersionText, _brainCloudVersionText, _serverVersionText, _envText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    
    void Start()
    {
        _appVersionText.text = Application.version;
        _brainCloudVersionText.text = _BCWrapper.Client.BrainCloudClientVersion;

        _appIDText.text = BrainCloud.Plugin.Interface.AppId;
        _appVersionText.text = Application.version;
        _brainCloudVersionText.text = _BCWrapper.Client.BrainCloudClientVersion;
        _BCWrapper.Client.GetAuthenticationService().getServerVersion(
            (string jsonResponse, object cbObj) =>
            {
                var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
                var data = response["data"] as Dictionary<string, object>;

                _serverVersionText.text = data["serverVersion"] as string;
            });

        string env = BrainCloud.Plugin.Interface.DispatcherURL.Split('.')[1];
        if (env == "braincloudservers") env = "prod";
        _envText.text = env;
    }
}