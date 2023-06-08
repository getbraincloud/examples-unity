using BrainCloud.JSONHelper;
using System;
using System.Collections.Generic;

/// <summary>
/// Data struct for brainCloud Error Responses.
/// </summary>
[Serializable]
public readonly struct ErrorResponse : IJSON
{
    public readonly int ReasonCode;
    public readonly int Status;
    public readonly string Message;

    public ErrorResponse(int reason_code, int status, string status_message)
    {
        ReasonCode = reason_code;
        Status = status;
        Message = status_message;
    }

    public override string ToString() => this.Serialize();

    public static string CreateGeneric(string message) => new ErrorResponse(0, 0, message).Serialize();

    #region IJSON

    public string GetDataType() => "error_response";

    public Dictionary<string, object> ToJSONObject() => new()
    {
        { "reason_code",    ReasonCode }, { "status",  Status },
        { "status_message", Message }
    };

    public IJSON FromJSONObject(Dictionary<string, object> obj)
    {
        int reason_code = obj.GetValue<int>("reason_code");
        int status = obj.GetValue<int>("status");
        string status_message = obj.GetString("status_message");

        return new ErrorResponse(reason_code, status, status_message);
    }

    #endregion
}
