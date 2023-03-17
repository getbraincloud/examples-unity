using BrainCloud;
using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;

/// <summary>
/// A basic data struct for brainCloud's user entities.
/// </summary>
[Serializable]
public struct Entity : IJSON
{
    #region Consts

    // JSON Properties
    private const string PROPERTY_VERSION     = "version";
    private const string PROPERTY_ENTITY_ID   = "entityId";
    private const string PROPERTY_ENTITY_TYPE = "entityType";
    private const string PROPERTY_CREATED_AT  = "createdAt";
    private const string PROPERTY_UPDATED_AT  = "updatedAt";
    private const string PROPERTY_ACL         = "acl";
    private const string PROPERTY_DATA        = "data";

    #endregion

    [JsonName(PROPERTY_VERSION)]     public int Version;
    [JsonName(PROPERTY_ENTITY_ID)]   public string EntityID;
    [JsonName(PROPERTY_ENTITY_TYPE)] public string EntityType;
    [JsonName(PROPERTY_CREATED_AT)]  public DateTime CreatedAt;
    [JsonName(PROPERTY_UPDATED_AT)]  public DateTime UpdatedAt;
    [JsonName(PROPERTY_ACL)]         public ACL ACL;
    [JsonName(PROPERTY_DATA)]        public IJSON Data;

    public Entity(IJSON data)
    {
        Version = -1; // -1 tells the server to create the latest version
        EntityID = string.Empty;
        EntityType = data.GetDataType();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
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
        { PROPERTY_VERSION,    Version },   { PROPERTY_ENTITY_ID,  EntityID },  { PROPERTY_ENTITY_TYPE, EntityType },
        { PROPERTY_CREATED_AT, CreatedAt }, { PROPERTY_UPDATED_AT, UpdatedAt }, { PROPERTY_ACL,         ACL },
        { PROPERTY_DATA,       Data }
    };

    public string Serialize() => JsonWriter.Serialize(this);

    public void Deserialize(Dictionary<string, object> json)
    {
        Version = (int)json[PROPERTY_VERSION];
        EntityID = json[PROPERTY_ENTITY_ID] as string;
        EntityType = json[PROPERTY_ENTITY_TYPE] as string;
        CreatedAt = Util.BcTimeToDateTime((long)json[PROPERTY_CREATED_AT]);
        UpdatedAt = Util.BcTimeToDateTime((long)json[PROPERTY_UPDATED_AT]);
        ACL.ReadFromJson(json[PROPERTY_ACL] as Dictionary<string, object>);

        if (Data == null && EntityType == UserData.DataType)
        {
            Data = new UserData();
        }

        if (Data != null && json.ContainsKey(PROPERTY_DATA))
        {
            Data.Deserialize(json[PROPERTY_DATA] as Dictionary<string, object>);
        }

        EntityType = GetDataType();
    }

    #endregion
}
