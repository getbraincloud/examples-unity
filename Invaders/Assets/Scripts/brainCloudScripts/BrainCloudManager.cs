using System.Collections;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class BrainCloudManager : MonoBehaviour
{

    private BrainCloudWrapper _wrapper;
    private BrainCloudS2S _bcS2S = new BrainCloudS2S();

    public UnityTransport _unityTransport;

    public static BrainCloudManager Singleton { get; private set; }
    void Awake()
    {
        if(Singleton == null)
        {
            Singleton = this;
        }
        else
        {
            Destroy(gameObject);
        }
    
        _wrapper = GetComponent<BrainCloudWrapper>();
        _wrapper.Init();
        _unityTransport = GetComponent<UnityTransport>();
        _bcS2S.Init(BrainCloud.Plugin.Interface.AppId, "Testing", BrainCloud.Plugin.Interface.AppSecret, true);
    }

    public void AuthenticateWithBrainCloud(string in_username, string in_password)
    {
        _wrapper.AuthenticateUniversal(in_username, in_password, true, OnAuthenticateSuccess, OnFailureCallback);
    }
    
    private void OnAuthenticateSuccess(string jsonResponse, object cbObject)
    {
        MenuControl.Singleton.SwitchMenuButtons();
    }
    
    public void FindOrCreateLobby()
    {
        _wrapper.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent); 
        //_wrapper.RTTService.EnableRTT();
        _wrapper.RTTService.RequestClientConnection(OnRequestClientConnectionSuccess, OnFailureCallback);
    }
    
    void OnRequestClientConnectionSuccess(string jsonResponse, object cbObject)
    {

    }
    
    void OnLobbyEvent(string jsonResponse)
    {
        
    }
    
    private void OnFailureCallback(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        Debug.Log("Error: " + statusMessage);
    }

}
