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

public class PlaybackSaver : MonoBehaviour
{
    private bool _dead;
    private bool IsDedicatedServer;

    public static PlaybackSaver Singleton { get; private set; }

    private BrainCloudWrapper _bcWrapper;
    private string profileId;

    private string createdRecordId = "";
    private int previousHighScore = -1;
    private string previousRecordId = "";
    private bool finishedAddingEvents = false;
    private int eventsAdded = 0;

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
        profileId = BrainCloudManager.Singleton.LocalUserInfo.ProfileID;
    }

    private void OnGenericFailure(int status, int reasonCode, string jsonError, object cbObject)
    {
        if (_dead) return;
        _bcWrapper.Client.ResetCommunication();
        _dead = true;

        string message = cbObject as string;
        Debug.Log($"Failure: {message} |||| JSON: {jsonError}");
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

    private IEnumerator AddEvents(string replayId, PlaybackStreamRecord record)
    {
        List<int> runLengths = new List<int>() { 0 };
        for (int ii = 1; ii < record.totalFrameCount; ii++)
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
        string summaryData = string.Concat(
            "{\"framecount\":", record.totalFrameCount,
            ",\"startpos\":", record.startPosition,
            ",\"username\":\"", record.username, "\"", END);
        int index = 0;
        string eventData = "";

        for (int ii = 0; ii < runLengths.Count; ii++)
        {
            eventData = string.Concat(
                MOVEMENT, record.frames[index].xDelta,
                SHOOT, (record.frames[index].createBullet ? 1 : 0),
                RUNLENGTH, runLengths[ii],
                ID, record.frames[index].frameID, END);
            _bcWrapper.PlaybackStreamService.AddEvent(replayId, eventData, summaryData, OnAddEventSuccess, OnGenericFailure);
            index += runLengths[ii] + 1;
            yield return new WaitUntil(() => eventsAdded == ii + 1);
        }

        finishedAddingEvents = true;
        eventsAdded = 0;
        yield break;
    }

    private void OnAddEventSuccess(string in_jsonResponse, object cbObject)
    {
        eventsAdded += 1;
    }

    public void StartSubmittingRecord(int newScore, PlaybackStreamRecord newRecord)
    {
        StartCoroutine(SubmitRecord(newScore, newRecord));
    }

    private IEnumerator SubmitRecord(int newScore, PlaybackStreamRecord newRecord)
    {
        //Get the player's previous high score and attached record ID
        if (previousHighScore == -1)
        {
            _bcWrapper.LeaderboardService.GetPlayerScore("InvaderHighScore", -1, OnGetPlayerScoreSuccess, OnGetPlayerScoreFailure);
            yield return new WaitUntil(() => previousHighScore != -1);
        }
        if (previousHighScore > newScore) yield break; //Early return if allowed

        //Start a new stream and grab its ID
        _bcWrapper.PlaybackStreamService.StartStream(profileId, false, OnStartStreamSuccess, OnGenericFailure);
        yield return new WaitUntil(() => createdRecordId != "");

        //Post the high score to the leaderboard and attach the record ID
        string createdRecordIdJson = string.Concat("{\"replay\":\"", createdRecordId, "\"}");
        _bcWrapper.LeaderboardService.PostScoreToLeaderboard("InvaderHighScore", newScore, createdRecordIdJson, null, OnGenericFailure);

        //Add the recorded data to the stream
        StartCoroutine(AddEvents(createdRecordId, newRecord));
        yield return new WaitUntil(() => finishedAddingEvents);

        //Clean up the previous stream and close the recently created stream
        if (previousRecordId != "") _bcWrapper.PlaybackStreamService.DeleteStream(previousRecordId, null, OnGenericFailure);
        _bcWrapper.PlaybackStreamService.EndStream(createdRecordId, null, OnGenericFailure);

        //Reset the variables to be used again
        createdRecordId = "";
        finishedAddingEvents = false;
        previousHighScore = newScore;
        previousRecordId = createdRecordId;
        yield break;
    }
}
