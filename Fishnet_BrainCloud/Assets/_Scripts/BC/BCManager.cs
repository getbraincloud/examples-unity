using BrainCloud;
using BrainCloud.JsonFx.Json;
using FishNet.Managing.Timing;
using FishNet.Transporting;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using FishyBrainCloud;

    public class BCManager : MonoBehaviour
    {
        private static BCManager _instance;
        public static BCManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Look for an existing instance
                    _instance = FindObjectOfType<BCManager>();

                    if (_instance == null)
                    {
                        // Create new GameObject to attach the manager to
                        GameObject managerObject = new GameObject("BCManager");
                        _instance = managerObject.AddComponent<BCManager>();
                        DontDestroyOnLoad(managerObject);
                    }
                }

                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

    private BrainCloudWrapper _bc;
    public BrainCloudWrapper bc => _bc;

    private bool _bcInitialized = false;

        public string RelayPasscode;
        public string CurrentLobbyId;

        public string RoomAddress;
        public ushort RoomPort;

        public string LobbyOwnerId;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            Instance = this;

            _bc = gameObject.AddComponent<BrainCloudWrapper>();
            _bc.Init();

            Debug.Log("BrainCloud client version: " + _bc.Client.BrainCloudClientVersion);
            _bcInitialized = true;
        }

        public void AuthenticateUser(string username, string password, Action<bool> callback)
        {
            SuccessCallback success = (responseData, cbObject) =>
            {
                //on auth success
                callback(true);
            };

            FailureCallback failure = (statusMessage, code, error, cbObject) =>
            {
                callback(false);
            };

            _bc.AuthenticateUniversal(username, password, true, success, failure);
        }

        public void EnableRTT(bool enabled, Action OnSuccess)
        {
            if (enabled)
            {
                SuccessCallback success = (responseData, cbObject) =>
                {
                    OnSuccess();
                };
                _bc.RTTService.EnableRTT(success, OnRTTFailed);
            }
            else
            {
                _bc.RTTService.DisableRTT();
            }
        }

        private void OnRTTFailed(int statusCode, int reasonCode, string statusMessage, object cbObject)
        {
            Debug.LogError($"RTT disconnected: {statusMessage} (Status Code: {statusCode}, Reason Code: {reasonCode})");
            // Error handling logic
        }

        public void FindLobby(Action<string> OnEntryId)
        {
            var lobbyParams = CreateLobbyParams(OnEntryId);

            _bc.LobbyService.FindLobby(
                "CursorPartyV2_Ire",
                0,
                1,
                lobbyParams.algo,
                lobbyParams.filters,
                false,
                lobbyParams.extra,
                "all",
                null,
                lobbyParams.success
            );
        }

        public void QuickFindLobby(Action<string> OnEntryId)
        {
            var lobbyParams = CreateLobbyParams(OnEntryId);
            
            _bc.LobbyService.FindOrCreateLobby(
                "CursorPartyV2_Ire",
                0,
                1,
                lobbyParams.algo,
                lobbyParams.filters,
                false,
                lobbyParams.extra,
                "all",
                lobbyParams.settings,
                null,
                lobbyParams.success
            );
            
        }

        private LobbyParams CreateLobbyParams(Action<string> OnEntryId)
        {
            var algo = new Dictionary<string, object>
            {
                ["strategy"] = "ranged-absolute",
                ["alignment"] = "center",
                ["ranges"] = new List<int> { 1000 }
            };

            var filters = new Dictionary<string, object>();
            var extra = new Dictionary<string, object>();
            var settings = new Dictionary<string, object>();

            SuccessCallback success = (in_response, cbObject) =>
            {
                var response = JsonReader.Deserialize<Dictionary<string, object>>(in_response);
                var data = response["data"] as Dictionary<string, object>;
                if (data.ContainsKey("entryId"))
                {
                    var entryId = data["entryId"] as string;

                    OnEntryId?.Invoke(entryId);
                }
            };

            return new LobbyParams
            {
                algo = algo,
                filters = filters,
                extra = extra,
                success = success,
                settings = settings
            };
        }

        public void CreateLobby(Action<string> OnSuccess)
        {
            var lobbyParams = CreateLobbyParams(OnSuccess);

            _bc.LobbyService.CreateLobby("CursorPartyV2_Ire", 0, false, lobbyParams.extra, "all", lobbyParams.settings, null, lobbyParams.success);
        }

        public void OnGameSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode loadMode)
        {
            StartCoroutine(DelayedConnect());
            
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnGameSceneLoaded;
        }

        private IEnumerator DelayedConnect()
        {
            yield return new WaitForSeconds(0.05f);

            FishyBrainCloudTransport fishyBrainCloud = FindObjectOfType<FishyBrainCloudTransport>();
            fishyBrainCloud.Config(_bc, RoomAddress, RelayPasscode, CurrentLobbyId, RoomPort);

            if (fishyBrainCloud != null)
            {
                Debug.Log("Found FishyBrainCloudTransport");
                bool isServer = LobbyOwnerId == bc.Client.ProfileId;

                if (fishyBrainCloud.GetConnectionState(isServer) == LocalConnectionState.Stopped)
                {
                    fishyBrainCloud.SetServerBindAddress(RoomAddress, IPAddressType.IPv4);
                }
                
                fishyBrainCloud.StartConnection(isServer);//Start Client
            }

        }
    }

    public class LobbyParams
    {
        public Dictionary<string, object> algo;
        public Dictionary<string, object> filters;
        public Dictionary<string, object> extra;
        public SuccessCallback success;
        public Dictionary<string, object> settings;
    }
