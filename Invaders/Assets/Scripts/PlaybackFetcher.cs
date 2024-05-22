using BrainCloud;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class PlaybackFetcher : MonoBehaviour
{
    private bool _dead;
    private bool IsDedicatedServer;

    private BrainCloudWrapper _bcWrapper;

    public BrainCloudWrapper Wrapper
    {
        get => _bcWrapper;
    }

    public BrainCloudS2S S2SWrapper
    {
        get => BrainCloudManager.Singleton.S2SWrapper;
    }

    private string createdRecordId = "";
    private int previousHighScore = -1;
    private string previousRecordId = "";
    private bool finishedAddingEvents = false;
    private int eventsAdded = 0;

    private List<PlaybackStreamReadData> storedRecords = new List<PlaybackStreamReadData>();

    private void Awake()
    {
        _bcWrapper = GetComponent<BrainCloudWrapper>();
        IsDedicatedServer = Application.isBatchMode && !Application.isEditor;
    }

    private void Start()
    {
        
    }

    public void AddRecordsFromUsers(List<string> userIds)
    {
        //This function will quit the session if the list is empty...
        if (userIds.Count > 0)
            _bcWrapper.LeaderboardService.GetPlayersSocialLeaderboard("InvaderHighScore", userIds, OnGetPlayerSocialLeaderboardSuccess, OnGenericFailure);
    }

    private void AddReplayFromId(string replayId)
    {
        if (IsDedicatedServer)
        {
            var requestJson = new Dictionary<string, object>();
            requestJson["service"] = "playbackStream";
            requestJson["operation"] = "READ_STREAM";

            var requestDataJson = new Dictionary<string, object>();
            requestDataJson["playbackStreamId"] = replayId;

            requestJson["data"] = requestDataJson;

            string jsonString = JsonWriter.Serialize(requestJson);
            S2SWrapper.Request(jsonString, OnServerReadStream);
        }
        else
        {
            _bcWrapper.PlaybackStreamService.ReadStream(replayId, OnReadStreamSuccess, OnGenericFailure);
        }
    }

    private void OnGenericFailure(int status, int reasonCode, string jsonError, object cbObject)
    {
        if (_dead) return;
        _bcWrapper.Client.ResetCommunication();
        _dead = true;

        string message = cbObject as string;
        Debug.Log($"Failure: {message} |||| JSON: {jsonError}");
    }

    private void OnServerReadStream(string response)
    {
        Dictionary<string, object> responseJson = JsonReader.Deserialize<Dictionary<string, object>>(response);
        Dictionary<string, object> jsonData = responseJson["data"] as Dictionary<string, object>;

        if ((int)responseJson["status"] == 200)
        {

        }
        else
        {

        }
    }

    private void OnGetPlayerSocialLeaderboardSuccess(string in_jsonResponse, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(in_jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;
        Dictionary<string, object>[] leaderboard = data["leaderboard"] as Dictionary<string, object>[];
        Dictionary<string, object> userData;
        Dictionary<string, object> playbackId;

        foreach (Dictionary<string, object> ii in leaderboard)
        {
            userData = ii;
            playbackId = userData["data"] as Dictionary<string, object>;
            AddReplayFromId((string)playbackId["replay"]);
        }
    }

    private void OnReadStreamSuccess(string in_jsonResponse, object cbObject)
    {
        PlaybackStreamRecord output = new PlaybackStreamRecord();
        Dictionary<string, object> response = JsonReader.Deserialize(in_jsonResponse) as Dictionary<string, object>;
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
        output.startPosition = summary["startpos"] as float? ?? 0.0f;

        foreach (Dictionary<string, object> eventObj in events)
        {
            for(int ii = 0; ii <= (int)eventObj["runlength"]; ii++)
            {
                output.frames.Add(new PlaybackStreamFrame(
                    (int)eventObj["movement"], 
                    (int)eventObj["shoot"] == 1 && ii == 0,
                    (int)eventObj["id"] + ii)
                    );
            }
        }

        storedRecords.Add(new PlaybackStreamReadData(output));
    }

    private void OnStartStreamSuccess(string in_jsonResponse, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(in_jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;
        createdRecordId = (string)data["playbackStreamId"];
    }

    private void OnGetPlayerScoreSuccess(string in_jsonResponse, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(in_jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;
        Dictionary<string, object> score = data["score"] as Dictionary<string, object>;
        Dictionary<string, object> scoreData = score["data"] as Dictionary<string, object>;
        previousHighScore = (int)score["score"];
        previousRecordId = scoreData["replay"] as string;
    }

    private void OnGetPlayerScoreFailure(int status, int reasonCode, string jsonError, object cbObject)
    {
        previousHighScore = 0;
    }

    private void OnAddEventSuccess(string in_jsonResponse, object cbObject)
    {
        eventsAdded += 1;
    }

    private IEnumerator AddEvents(string replayId, PlaybackStreamRecord record)
    {
        List<int> runLengths = new List<int>() { 0 };
        for(int ii = 1; ii < record.totalFrameCount; ii++)
        {
            if (record.frames[ii].createBullet) runLengths.Add(0);
            else if (record.frames[ii].xDelta != record.frames[ii - 1].xDelta) runLengths.Add(0);
            else runLengths[^1] += 1;
        }

        const string MOVEMENT = "{\"movement\":";
        const string SHOOT = ",\"shoot\":";
        const string RUNLENGTH = ",\"runlength\":";
        const string ID = ",\"id\":";
        const string END = "}";
        string summaryData = "{\"framecount\":" + record.totalFrameCount + ",\"startpos\":" + 0 + END;
        int index = 0;
        string eventData = "";

        for(int ii = 0; ii < runLengths.Count; ii++)
        {
            eventData = MOVEMENT + record.frames[index].xDelta + SHOOT + (record.frames[index].createBullet ? 1 : 0) + 
                RUNLENGTH + runLengths[ii] + ID + record.frames[index].frameID + END;
            _bcWrapper.PlaybackStreamService.AddEvent(replayId, eventData, summaryData, OnAddEventSuccess, OnGenericFailure);
            index += runLengths[ii] + 1;
            yield return new WaitUntil(() => eventsAdded == ii + 1);
        }

        finishedAddingEvents = true;
        eventsAdded = 0;
        yield break;
    }

    public void StartSubmittingRecord(int newScore, PlaybackStreamRecord newRecord)
    {
        StartCoroutine(SubmitRecord(newScore, newRecord));
    }

    private IEnumerator SubmitRecord(int newScore, PlaybackStreamRecord newRecord)
    {
        if(previousHighScore == -1)
        {
            _bcWrapper.LeaderboardService.GetPlayerScore("InvaderHighScore", -1, OnGetPlayerScoreSuccess, OnGetPlayerScoreFailure);
            yield return new WaitUntil(() => previousHighScore != -1);
        }

        if (previousHighScore > newScore) yield break;
        _bcWrapper.PlaybackStreamService.StartStream(BrainCloudManager.Singleton.LocalUserInfo.ProfileID, false, OnStartStreamSuccess, OnGenericFailure);
        yield return new WaitUntil(() => createdRecordId != "");

        string createdRecordIdJson = string.Concat("{\"replay\":\"", createdRecordId, "\"}");
        _bcWrapper.LeaderboardService.PostScoreToLeaderboard("InvaderHighScore", newScore, createdRecordIdJson, null, OnGenericFailure);
        StartCoroutine(AddEvents(createdRecordId, newRecord));
        yield return new WaitUntil(() => finishedAddingEvents);

        if(previousRecordId != "") _bcWrapper.PlaybackStreamService.DeleteStream(previousRecordId, null, OnGenericFailure);
        _bcWrapper.PlaybackStreamService.EndStream(createdRecordId, null, OnGenericFailure);
        createdRecordId = "";
        finishedAddingEvents = false;
        previousHighScore = newScore;
        previousRecordId = createdRecordId;
        yield break;
    }

    public List<PlaybackStreamReadData> GetStoredRecords()
    {
        List<PlaybackStreamReadData> output = new List<PlaybackStreamReadData>();
        foreach(PlaybackStreamReadData ii in storedRecords) output.Add(ii);
        storedRecords.Clear();
        return output;
    }
}
