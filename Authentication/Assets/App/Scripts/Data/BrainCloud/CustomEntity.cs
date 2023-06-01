using BrainCloud;
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

    [JsonName(PROPERTY_VERSION)]      public int Version;
    [JsonName(PROPERTY_OWNER_ID)]     public string OwnerID;
    [JsonName(PROPERTY_ENTITY_ID)]    public string EntityID;
    [JsonName(PROPERTY_ENTITY_TYPE)]  public string EntityType;
    [JsonName(PROPERTY_CREATED_AT)]   public DateTime CreatedAt;
    [JsonName(PROPERTY_UPDATED_AT)]   public DateTime UpdatedAt;
    [JsonName(PROPERTY_TIME_TO_LIVE)] public TimeSpan TimeToLive;
    [JsonName(PROPERTY_EXPIRES_AT)]   public DateTime ExpiresAt;
    [JsonName(PROPERTY_ACL)]          public ACL ACL;
    [JsonName(PROPERTY_DATA)]         public IJSON Data;

    public bool IsOwned => OwnerID == UserHandler.ProfileID;

    public TimeSpan ExpiresIn => ExpiresAt - DateTime.UtcNow;

    public CustomEntity(IJSON data)
    {
        Version = -1; // -1 tells the server to create the latest version
        OwnerID = string.Empty;
        EntityID = string.Empty;
        EntityType = data.GetDataType();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        TimeToLive = TimeSpan.MaxValue;
        ExpiresAt = DateTime.MaxValue;
        ACL = ACL.ReadWrite();
        Data = data;
    }

    #region IJSON

    public string GetDataType() => Data != null ? Data.GetDataType() : EntityType;

    public Dictionary<string, object> ToJSONObject() => new()
    {
        { PROPERTY_VERSION,      Version },    { PROPERTY_OWNER_ID,   OwnerID },   { PROPERTY_ENTITY_ID,  EntityID },
        { PROPERTY_ENTITY_TYPE,  EntityType }, { PROPERTY_CREATED_AT, CreatedAt }, { PROPERTY_UPDATED_AT, UpdatedAt },
        { PROPERTY_TIME_TO_LIVE, TimeToLive }, { PROPERTY_EXPIRES_AT, ExpiresAt }, { PROPERTY_ACL,        ACL },
        { PROPERTY_DATA,         Data }
    };

    public IJSON FromJSONObject(Dictionary<string, object> obj)
    {
        Version = obj[PROPERTY_VERSION].ToType<int>();
        OwnerID = obj[PROPERTY_OWNER_ID].ToString();
        EntityID = obj[PROPERTY_ENTITY_ID].ToString();
        CreatedAt = Util.BcTimeToDateTime(obj[PROPERTY_CREATED_AT].ToType<long>());
        UpdatedAt = Util.BcTimeToDateTime(obj[PROPERTY_UPDATED_AT].ToType<long>());

        if (obj.ContainsKey(PROPERTY_TIME_TO_LIVE))
        {
            TimeToLive = TimeSpan.FromMilliseconds(obj[PROPERTY_TIME_TO_LIVE].ToType<long>());
            ExpiresAt = Util.BcTimeToDateTime(obj[PROPERTY_EXPIRES_AT].ToType<long>());
        }
        else
        {
            TimeToLive = TimeSpan.MaxValue;
            ExpiresAt = DateTime.MaxValue;
        }

        ACL ??= new ACL();
        ACL.ReadFromJson(obj[PROPERTY_ACL].ToJSONObject());

        if (Data == null)
        {
            EntityType = (string)obj[PROPERTY_ENTITY_TYPE];
            Data = EntityType == HockeyStatsData.DataType ? new HockeyStatsData() : new RPGData();
        }
        
        if (Data != null && obj.ContainsKey(PROPERTY_DATA))
        {
            Data.FromJSONObject(obj[PROPERTY_DATA] as Dictionary<string, object>);
        }

        EntityType = GetDataType();

        return this;
    }

    #endregion
}
