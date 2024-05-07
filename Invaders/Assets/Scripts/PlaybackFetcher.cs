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

    private void AddEvents(string replayId, int duration, PlaybackStreamRecord record)
    {
        string eventData = "{\"movement\":0}";
        string summaryData = "{\"framecount\":" + duration + "}";
        _bcWrapper.PlaybackStreamService.AddEvent(replayId, eventData, summaryData, null, OnFailureCallback);
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

        Debug.Log(previousHighScore + " previous score");
        if (previousHighScore > newScore) yield break;
        _bcWrapper.PlaybackStreamService.StartStream(BrainCloudManager.Singleton.LocalUserInfo.ProfileID, false, OnStartStreamSuccess, OnFailureCallback);
        yield return new WaitUntil(() => createdRecordId != "");

        string createdRecordIdJson = string.Concat("{\"replay\":\"", createdRecordId, "\"}");
        Debug.Log(createdRecordIdJson + " ["+ createdRecordId + "]" + (createdRecordId != ""));
        _bcWrapper.LeaderboardService.PostScoreToLeaderboard("InvaderHighScore", newScore, createdRecordIdJson, null, OnFailureCallback);
        Debug.Log(newScore + " new score");
        yield return new WaitForSeconds(2f);

        _bcWrapper.PlaybackStreamService.DeleteStream(previousRecordId, null, OnFailureCallback);
        _bcWrapper.PlaybackStreamService.EndStream(createdRecordId, null, OnFailureCallback);
        createdRecordId = "";
        previousHighScore = newScore;
        previousRecordId = createdRecordId;
        yield break;
    }
}
