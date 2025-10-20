using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using System;
using System.Collections.Generic;

/// <summary>
/// A basic data struct for brainCloud's custom entities.
/// </summary>
[Serializable]
public struct CustomEntity : IJSON
{
    #region Consts

    // JSON Properties
    private const string PROPERTY_VERSION      = "version";
    private const string PROPERTY_OWNER_ID     = "ownerId";
    private const string PROPERTY_ENTITY_ID    = "entityId";
    private const string PROPERTY_ENTITY_TYPE  = "entityType";
    private const string PROPERTY_CREATED_AT   = "createdAt";
    private const string PROPERTY_UPDATED_AT   = "updatedAt";
    private const string PROPERTY_TIME_TO_LIVE = "timeToLive";
    private const string PROPERTY_EXPIRES_AT   = "expiresAt";
    private const string PROPERTY_ACL          = "acl";
    private const string PROPERTY_DATA         = "data";

    #endregion

    [JsonName(PROPERTY_VERSION)]      public int version;
    [JsonName(PROPERTY_OWNER_ID)]     public string ownerId;
    [JsonName(PROPERTY_ENTITY_ID)]    public string entityId;
    [JsonName(PROPERTY_ENTITY_TYPE)]  public string entityType;
    [JsonName(PROPERTY_CREATED_AT)]   public DateTime createdAt;
    [JsonName(PROPERTY_UPDATED_AT)]   public DateTime updatedAt;
    [JsonName(PROPERTY_TIME_TO_LIVE)] public TimeSpan timeToLive;
    [JsonName(PROPERTY_EXPIRES_AT)]   public DateTime expiresAt;
    [JsonName(PROPERTY_ACL)]          public ACL acl;
    [JsonName(PROPERTY_DATA)]         public IJSON data;

    public bool IsOwned => ownerId == UserHandler.ProfileID;

    public TimeSpan ExpiresIn => expiresAt - DateTime.UtcNow;

    public CustomEntity(IJSON data)
    {
        version = -1; // -1 tells the server to create the latest version
        ownerId = string.Empty;
        entityId = string.Empty;
        entityType = data.GetDataType();
        createdAt = DateTime.UtcNow;
        updatedAt = DateTime.UtcNow;
        timeToLive = TimeSpan.MaxValue;
        expiresAt = DateTime.MaxValue;
        acl = ACL.None();
        this.data = data;
    }

    #region IJSON

    public string GetDataType() => data is not null and IJSON ? ((IJSON)data).GetDataType() : entityType;

    public Dictionary<string, object> ToJSONObject() => new()
    {
        { PROPERTY_VERSION,      version },    { PROPERTY_OWNER_ID,   ownerId },   { PROPERTY_ENTITY_ID,  entityId },
        { PROPERTY_ENTITY_TYPE,  entityType }, { PROPERTY_CREATED_AT, createdAt }, { PROPERTY_UPDATED_AT, updatedAt },
        { PROPERTY_TIME_TO_LIVE, timeToLive }, { PROPERTY_EXPIRES_AT, expiresAt }, { PROPERTY_ACL,        acl },
        { PROPERTY_DATA,         data.ToJSONObject() }
    };

    public IJSON FromJSONObject(Dictionary<string, object> obj)
    {
        version = obj.GetValue<int>(PROPERTY_VERSION);
        ownerId = obj.GetString(PROPERTY_OWNER_ID);
        entityId = obj.GetString(PROPERTY_ENTITY_ID);
        entityType = obj.GetString(PROPERTY_ENTITY_TYPE);
        createdAt = obj.GetDateTime(PROPERTY_CREATED_AT);
        updatedAt = obj.GetDateTime(PROPERTY_UPDATED_AT);
        timeToLive = obj.GetTimeSpan(PROPERTY_TIME_TO_LIVE);
        expiresAt = obj.GetDateTime(PROPERTY_EXPIRES_AT);
        acl = obj.GetACL(PROPERTY_ACL);
        data = entityType == HockeyStatsData.DataType ? obj.GetJSONObject<HockeyStatsData>(PROPERTY_DATA)
                                                      : obj.GetJSONObject<RPGData>(PROPERTY_DATA);

        return this;
    }

    #endregion
}
