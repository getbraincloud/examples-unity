using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

public class PlaybackFetcher : MonoBehaviour
{
    private bool _dead;

    private BrainCloudWrapper _bcWrapper;

    public BrainCloudWrapper Wrapper
    {
        get => _bcWrapper;
    }

    private string createdRecordId = "";
    private int previousHighScore = -1;
    private string previousRecordId = "";
    private bool finishedAddingEvents = false;
    private int eventsAdded = 0;

    private void Awake()
    {
        _bcWrapper = GetComponent<BrainCloudWrapper>();
    }

    private void OnFailureCallback(int status, int reasonCode, string jsonError, object cbObject)
    {
        if (_dead) return;
        _bcWrapper.Client.ResetCommunication();
        _dead = true;

        string message = cbObject as string;
        Debug.Log($"Failure: {message} |||| JSON: {jsonError}");
    }

    private void OnReadStreamSuccess(string in_jsonResponse, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(in_jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;
        Dictionary<string, object>[] events = data["events"] as Dictionary<string, object>[];
        Dictionary<string, object> summary = data["summary"] as Dictionary<string, object>;
        if (events == null || events.Length == 0)
        {
            Debug.LogWarning("No events were retrieved...");
            return;
        }
        if (summary != null && summary.Count > 0)
        {

        }

        //records.Add(GenerateFakeRecord());
    }

    private void OnStartStreamSuccess(string in_jsonResponse, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(in_jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;
        createdRecordId = (string)data["playbackStreamId"];
        Debug.Log(createdRecordId);
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
        List<int> runLengths = new List<int>() { 1 };
        for(int ii = 1; ii < record.totalFrameCount; ii++)
        {
            if (record.frames[ii].createBullet) runLengths.Add(1);
            else if (Mathf.Abs(record.frames[ii].xDelta - record.frames[ii - 1].xDelta) > 0.05f) runLengths.Add(1);
            else runLengths[^1] += 1;
        }

        const string MOVEMENT = "{\"movement\":";
        const string SHOOT = ",\"shoot\":";
        const string RUNLENGTH = ",\"runlength\":";
        const string ID = ",\"id\":";
        const string END = "}";
        string summaryData = "{\"framecount\":" + record.totalFrameCount + END;
        int index = 0;
        string eventData = "";

        Debug.Log(summaryData + record.frames[index].xDelta);
        for(int ii = 0; ii < runLengths.Count; ii++)
        {
            eventData = MOVEMENT + record.frames[index].xDelta + SHOOT + (record.frames[index].createBullet ? 1 : 0) + 
                RUNLENGTH + runLengths[ii] + ID + record.frames[index].frameID + END;
            _bcWrapper.PlaybackStreamService.AddEvent(replayId, eventData, summaryData, OnAddEventSuccess, OnFailureCallback);
            index += runLengths[ii];
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
        _bcWrapper.PlaybackStreamService.StartStream(BrainCloudManager.Singleton.LocalUserInfo.ProfileID, false, OnStartStreamSuccess, OnFailureCallback);
        yield return new WaitUntil(() => createdRecordId != "");

        string createdRecordIdJson = string.Concat("{\"replay\":\"", createdRecordId, "\"}");
        _bcWrapper.LeaderboardService.PostScoreToLeaderboard("InvaderHighScore", newScore, createdRecordIdJson, null, OnFailureCallback);
        StartCoroutine(AddEvents(createdRecordId, newRecord));
        yield return new WaitUntil(() => finishedAddingEvents);

        _bcWrapper.PlaybackStreamService.DeleteStream(previousRecordId, null, OnFailureCallback);
        _bcWrapper.PlaybackStreamService.EndStream(createdRecordId, null, OnFailureCallback);
        createdRecordId = "";
        finishedAddingEvents = false;
        previousHighScore = newScore;
        previousRecordId = createdRecordId;
        yield break;
    }
}
