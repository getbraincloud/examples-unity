using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using System;
using System.Collections.Generic;

/// <summary>
/// A basic data struct for chat service's channel information.
/// </summary>
[Serializable]
public struct ChannelInfo : IJSON
{
    #region Consts

    // JSON Properties
    private const string PROPERTY_ID            = "id";
    private const string PROPERTY_TYPE          = "type";
    private const string PROPERTY_CODE          = "code";
    private const string PROPERTY_NAME          = "name";
    private const string PROPERTY_DESC          = "desc";
    private const string PROPERTY_STATS         = "stats";
    private const string PROPERTY_MESSAGE_COUNT = "messageCount";

    #endregion

    [JsonName(PROPERTY_ID)]    public string id;
    [JsonName(PROPERTY_TYPE)]  public string type;
    [JsonName(PROPERTY_CODE)]  public string code;
    [JsonName(PROPERTY_NAME)]  public string name;
    [JsonName(PROPERTY_DESC)]  public string desc;
    [JsonName(PROPERTY_STATS)] public Dictionary<string, object> stats;

    public readonly int messageCount
    {
        get => stats.GetValue<int>(PROPERTY_MESSAGE_COUNT);
        set => stats[PROPERTY_MESSAGE_COUNT] = value;
    }

    #region IJSON

    public readonly string GetDataType() => typeof(ChannelInfo).Name.ToLower();

    public readonly Dictionary<string, object> ToJSONObject() => new()
    {
        { PROPERTY_ID,  id }, { PROPERTY_TYPE,  type }, { PROPERTY_CODE, code },
        { PROPERTY_NAME, name }, { PROPERTY_DESC, desc }, { PROPERTY_STATS, stats },
    };

    public IJSON FromJSONObject(Dictionary<string, object> obj)
    {
        id = obj.GetString(PROPERTY_ID);
        type = obj.GetString(PROPERTY_TYPE);
        code = obj.GetString(PROPERTY_CODE);
        name = obj.GetString(PROPERTY_NAME);
        desc = obj.GetString(PROPERTY_DESC);
        stats = obj.GetJSONObject(PROPERTY_STATS);

        return this;
    }

    #endregion
}
