using BrainCloud;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

public class PlaybackFetcher : NetworkBehaviour
{
    private bool _dead;
    private bool IsDedicatedServer;

    public static PlaybackFetcher Singleton { get; private set; }

    private BrainCloudWrapper _bcWrapper;
    private GhostSpawner ghostSpawner;

    public BrainCloudS2S S2SWrapper
    {
        get => BrainCloudManager.Singleton.S2SWrapper;
    }

    private NetworkStringArray storedIds;

    private void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this.gameObject);
        }
        IsDedicatedServer = Application.isBatchMode && !Application.isEditor;
    }

    private void Start()
    {
        _bcWrapper = BrainCloudManager.Singleton.GetComponent<BrainCloudWrapper>();
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
    }

    private void OnGenericFailure(int status, int reasonCode, string jsonError, object cbObject)
    {
        if (_dead) return;
        _bcWrapper.Client.ResetCommunication();
        _dead = true;

        string message = cbObject as string;
        Debug.Log($"Failure: {message} |||| JSON: {jsonError}");
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (IsDedicatedServer) return;
        if (storedIds.elements == null) return;

        if (storedIds.elements.Length > 0)
        {
            AddAllReplaysFromIdsServerRPC(storedIds);
        }
    }

    public void AddRecordsFromUsers(List<string> userIds)
    {
        if (userIds.Count == 0) return;
        _bcWrapper.LeaderboardService.GetPlayersSocialLeaderboard("InvaderHighScore", userIds, OnGetPlayerSocialLeaderboardSuccess, OnGenericFailure);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddAllReplaysFromIdsServerRPC(NetworkStringArray ids)
    {
        foreach (string id in ids.elements)
        {
            AddReplayFromId(id);
        }
    }

    private void AddReplayFromId(string replayId)
    {
        var requestDataJson = new Dictionary<string, object>();
        requestDataJson["playbackStreamId"] = replayId;
        requestDataJson["ccCall"] = false;

        var requestJson = new Dictionary<string, object>();
        requestJson["service"] = "playbackStream";
        requestJson["operation"] = "SYS_READ_STREAM";
        requestJson["data"] = requestDataJson;

        string jsonString = JsonWriter.Serialize(requestJson);
        S2SWrapper.Request(jsonString, OnServerReadStream);
    }

    private void OnServerReadStream(string responseJson)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(responseJson) as Dictionary<string, object>;
        int status = (int)response["status"];

        if (status == 200) ParseStreamData(responseJson);
    }

    private void OnGetPlayerSocialLeaderboardSuccess(string in_jsonResponse, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(in_jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;
        Dictionary<string, object>[] leaderboard = data["leaderboard"] as Dictionary<string, object>[];
        Dictionary<string, object> playbackId;
        List<string> ids = new List<string>();

        foreach (Dictionary<string, object> ii in leaderboard)
        {
            playbackId = ii["data"] as Dictionary<string, object>;
            ids.Add((string)playbackId["replay"]);
        }

        storedIds = new NetworkStringArray(ids);
    }

    private void ParseStreamData(string jsonResponse)
    {
        PlaybackStreamRecord output = new PlaybackStreamRecord();
        Dictionary<string, object> response = JsonReader.Deserialize(jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;
        Dictionary<string, object>[] events = data["events"] as Dictionary<string, object>[];
        Dictionary<string, object> summary = data["summary"] as Dictionary<string, object>;

        if (events == null || events.Length == 0)
        {
            Debug.LogWarning("No events were retrieved...");
            return;
        }
        if (summary == null || summary.Count == 0)
        {
            Debug.LogWarning("No summary was retrieved...");
            return;
        }

        output.totalFrameCount = summary["framecount"] as int? ?? -2;
        output.startPosition = Convert.ToSingle(summary["startpos"]);
        if (summary.ContainsKey("username")) output.username = summary["username"] as string;
        else output.username = string.Empty;

        foreach (Dictionary<string, object> eventObj in events)
        {
            for (int ii = 0; ii <= (int)eventObj["runlength"]; ii++)
            {
                output.frames.Add(new PlaybackStreamFrame(
                    (int)eventObj["movement"],
                    (int)eventObj["shoot"] == 1 && ii == 0,
                    (int)eventObj["id"] + ii)
                    );
            }
        }

        InstantiatePlaybackGhost(output);
    }

    private void InstantiatePlaybackGhost(PlaybackStreamRecord record)
    {
        if (ghostSpawner == null) ghostSpawner = FindAnyObjectByType<GhostSpawner>();
        ghostSpawner.InstantiateGhost(record);
    }
}
