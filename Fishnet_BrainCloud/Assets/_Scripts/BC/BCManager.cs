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
        private BrainCloudWrapper _bc;

        public BrainCloudWrapper bc
        {
            get
            {
                return _bc;
            }
        }
        // Start is called before the first frame update
        private bool _bcInitialized = false;

        public static BCManager Instance { get; private set; }

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
            var algo = new Dictionary<string, object>();
            algo["strategy"] = "ranged-absolute";
            algo["alignment"] = "center";
            List<int> ranges = new List<int>();
            ranges.Add(1000);
            algo["ranges"] = ranges;

            var extra = new Dictionary<string, object>();

            //
            var filters = new Dictionary<string, object>();

            //
            var settings = new Dictionary<string, object>();

            SuccessCallback success = (in_response, cbObject) =>
            {
                Dictionary<string, object> response = JsonReader.Deserialize<Dictionary<string, object>>(in_response);
                Dictionary<string, object> data = response["data"] as Dictionary<string, object>;
                string entryId = data["entryId"] as string;

                OnEntryId(entryId);
            };

            _bc.LobbyService.FindLobby("CursorPartyV2_Ire", 0, 1, algo, filters, false, extra, "all", null, success);
        }

        public void CreateLobby(Action<string> OnSuccess)
        {
            var extra = new Dictionary<string, object>();
            var settings = new Dictionary<string, object>();

            SuccessCallback success = (in_response, cbObject) =>
            {
                OnSuccess(in_response);
            };

            LobbyOwnerId = bc.Client.ProfileId;

            _bc.LobbyService.CreateLobby("CursorPartyV2_Ire", 0, false, extra, "all", settings, null, success);
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

                //fishyBrainCloud.SetClientAddress(RoomAddress);
                //fishyBrainCloud.SetPort(RoomPort);

                if (fishyBrainCloud.GetConnectionState(isServer) == LocalConnectionState.Stopped)
                {
                    fishyBrainCloud.SetServerBindAddress(RoomAddress, IPAddressType.IPv4);
                    //fishyBrainCloud.SetPort(RoomPort);
                    fishyBrainCloud.StartConnection(isServer);
                }
                else
                {
                    //fishyBrainCloud.SetClientAddress(RoomAddress);
                    fishyBrainCloud.StartConnection(isServer);//Start Client
                }
            }

        }

    }
