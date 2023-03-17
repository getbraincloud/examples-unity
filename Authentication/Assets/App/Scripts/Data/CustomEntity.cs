using BrainCloud;
using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
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
        EntityType = string.Empty;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        TimeToLive = TimeSpan.MaxValue;
        ExpiresAt = DateTime.MaxValue;
        ACL = ACL.ReadWrite();
        Data = data;
    }

    public T GetData<T>() where T : IJSON
        => (T)(Data is T ? Data : default);

    public void SetData<T>(T data) where T : IJSON
    {
        if (Data is T)
        {
            Data = data;
        }
    }

    #region IJSON

    public string GetDataType() => Data != null ? Data.GetDataType() : EntityType;

    public Dictionary<string, object> GetDictionary() => new Dictionary<string, object>
    {
        { PROPERTY_VERSION,      Version },    { PROPERTY_OWNER_ID,   OwnerID },   { PROPERTY_ENTITY_ID,  EntityID },
        { PROPERTY_ENTITY_TYPE,  EntityType }, { PROPERTY_CREATED_AT, CreatedAt }, { PROPERTY_UPDATED_AT, UpdatedAt },
        { PROPERTY_TIME_TO_LIVE, TimeToLive }, { PROPERTY_EXPIRES_AT, ExpiresAt }, { PROPERTY_ACL,        ACL },
        { PROPERTY_DATA,         Data }
    };

    public string Serialize() => JsonWriter.Serialize(this);

    public void Deserialize(Dictionary<string, object> json)
    {
        Version = (int)json[PROPERTY_VERSION];
        OwnerID = json[PROPERTY_OWNER_ID] as string;
        EntityID = json[PROPERTY_ENTITY_ID] as string;
        CreatedAt = Util.BcTimeToDateTime((long)json[PROPERTY_CREATED_AT]);
        UpdatedAt = Util.BcTimeToDateTime((long)json[PROPERTY_UPDATED_AT]);

        if (json.ContainsValue(json[PROPERTY_TIME_TO_LIVE]))
        {
            TimeToLive = json[PROPERTY_TIME_TO_LIVE].GetType() == typeof(long) ? TimeSpan.FromMilliseconds((long)json[PROPERTY_TIME_TO_LIVE])
                                                                               : TimeSpan.FromMilliseconds((int)json[PROPERTY_TIME_TO_LIVE]);

            ExpiresAt = Util.BcTimeToDateTime((long)json[PROPERTY_EXPIRES_AT]);
        }
        else
        {
            TimeToLive = TimeSpan.MaxValue;
            ExpiresAt = DateTime.MaxValue;
        }

        ACL ??= new ACL();
        ACL.ReadFromJson(json[PROPERTY_ACL] as Dictionary<string, object>);

        if (Data == null)
        {
            EntityType = json[PROPERTY_ENTITY_TYPE] as string;
            Data = EntityType == HockeyStatsData.DataType ? new HockeyStatsData() : new RPGData();
        }

        if (Data != null && json.ContainsKey(PROPERTY_DATA))
        {
            Data.Deserialize(json[PROPERTY_DATA] as Dictionary<string, object>);
        }

        EntityType = GetDataType();
    }

    #endregion
}
