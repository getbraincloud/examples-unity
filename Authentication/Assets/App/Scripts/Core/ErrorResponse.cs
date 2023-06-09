using BrainCloud;
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

    /// <summary>
    /// Create a generic serialized <see cref="ErrorResponse"/> for in-app client errors.
    /// </summary>
    /// <param name="message">The status message that provides context for the error.</param>
    /// <returns>The serialized JSON string that represents a brainCloud <b>jsonError</b>.
    /// <br>• reason_code will be <see cref="ReasonCodes.INVALID_REQUEST"/>.</br>
    /// <br>• status will be <see cref="StatusCodes.BAD_REQUEST"/>.</br>
    /// </returns>
    public static string CreateGeneric(string message) => new Dictionary<string, object>
    {
        { "reason_code",    ReasonCodes.INVALID_REQUEST }, { "status",        StatusCodes.BAD_REQUEST },
        { "status_message", message },                     { "x_stack_trace", string.Empty }
    }
    .Serialize();

    #region IJSON

    public string GetDataType() => "error_response";

    public Dictionary<string, object> ToJSONObject() => new()
    {
        { "reason_code",    ReasonCode }, { "status",        Status },
        { "status_message", Message },    { "x_stack_trace", string.Empty }
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
