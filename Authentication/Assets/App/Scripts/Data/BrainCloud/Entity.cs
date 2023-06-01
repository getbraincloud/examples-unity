using BrainCloud;
using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using System;
using System.Collections.Generic;

/// <summary>
/// A basic data struct for brainCloud's user entities.
/// </summary>
[Serializable]
public struct Entity<T> : IJSON where T : IJSON
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
    [JsonName(PROPERTY_DATA)]        public T Data;

    public Entity(T data)
    {
        Version = -1; // -1 tells the server to create the latest version
        EntityID = string.Empty;
        EntityType = data.GetDataType();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        ACL = ACL.ReadWrite();
        Data = data;
    }

    #region IJSON

    public string GetDataType() => Data != null ? Data.GetDataType() : EntityType;

    public Dictionary<string, object> ToJSONObject() => new()
    {
        { PROPERTY_VERSION,    Version },   { PROPERTY_ENTITY_ID,  EntityID },  { PROPERTY_ENTITY_TYPE, EntityType },
        { PROPERTY_CREATED_AT, CreatedAt }, { PROPERTY_UPDATED_AT, UpdatedAt }, { PROPERTY_ACL,         ACL },
        { PROPERTY_DATA,       Data }
    };

    public IJSON FromJSONObject(Dictionary<string, object> obj)
    {
        Version = obj[PROPERTY_VERSION].ToType<int>();
        EntityID = obj[PROPERTY_ENTITY_ID].ToString();
        EntityType = obj[PROPERTY_ENTITY_TYPE].ToString();
        CreatedAt = Util.BcTimeToDateTime(obj[PROPERTY_CREATED_AT].ToType<long>());
        UpdatedAt = Util.BcTimeToDateTime(obj[PROPERTY_UPDATED_AT].ToType<long>());
        ACL ??= new ACL();
        ACL.ReadFromJson(obj[PROPERTY_ACL].ToJSONObject());
        Data = obj[PROPERTY_DATA].ToJSONType<T>();

        return this;
    }

    #endregion
}
