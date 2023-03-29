using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data struct for brainCloud Error Responses.
/// </summary>
[Serializable]
public readonly struct ErrorResponse
{
    public readonly int ReasonCode;
    public readonly int Status;
    public readonly string Message;

    public ErrorResponse(int reasonCode, int status, string message)
    {
        ReasonCode = reasonCode;
        Status = status;
        Message = message;
    }

    public ErrorResponse(string jsonError)
    {
        Dictionary<string, object> json = JsonReader.Deserialize(jsonError) as Dictionary<string, object>;
        ReasonCode = (int)json["reason_code"];
        Status = (int)json["status"];
        Message = (string)json["status_message"];
    }
}
