using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;

/// <summary>
/// Data struct for brainCloud Error Responses.
/// </summary>
[Serializable]
public readonly struct ErrorResponse
{
    #region Consts

    // JSON Properties
    private const string PROPERTY_REASON_CODE = "reason_code";
    private const string PROPERTY_STATUS = "status";
    private const string PROPERTY_MESSAGE = "status_message";

    #endregion

    [JsonName(PROPERTY_REASON_CODE)] public readonly int ReasonCode;
    [JsonName(PROPERTY_STATUS)] public readonly int Status;
    [JsonName(PROPERTY_MESSAGE)] public readonly string Message;

    public ErrorResponse(int reasonCode, int status, string message)
    {
        ReasonCode = reasonCode;
        Status = status;
        Message = message;
    }

    public ErrorResponse(string jsonError)
    {
        Dictionary<string, object> json = JsonReader.Deserialize(jsonError) as Dictionary<string, object>;
        ReasonCode = (int)json[PROPERTY_REASON_CODE];
        Status = (int)json[PROPERTY_STATUS];
        Message = (string)json[PROPERTY_MESSAGE];
    }

    public string Serialize() => JsonWriter.Serialize(this);
}
