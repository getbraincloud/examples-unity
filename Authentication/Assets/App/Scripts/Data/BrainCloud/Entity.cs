using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
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

    [JsonName(PROPERTY_VERSION)]     public int version;
    [JsonName(PROPERTY_ENTITY_ID)]   public string entityId;
    [JsonName(PROPERTY_ENTITY_TYPE)] public string entityType;
    [JsonName(PROPERTY_CREATED_AT)]  public DateTime createdAt;
    [JsonName(PROPERTY_UPDATED_AT)]  public DateTime updatedAt;
    [JsonName(PROPERTY_ACL)]         public ACL acl;
    [JsonName(PROPERTY_DATA)]        public IJSON data;

    public Entity(IJSON data)
    {
        version = -1; // -1 tells the server to create the latest version
        entityId = string.Empty;
        entityType = data.GetDataType();
        createdAt = DateTime.UtcNow;
        updatedAt = DateTime.UtcNow;
        acl = ACL.None();
        this.data = data;
    }

    #region IJSON

    public string GetDataType() => data != null ? data.GetDataType() : entityType;

    public Dictionary<string, object> ToJSONObject() => new()
    {
        { PROPERTY_VERSION,    version },   { PROPERTY_ENTITY_ID,  entityId },  { PROPERTY_ENTITY_TYPE, entityType },
        { PROPERTY_CREATED_AT, createdAt }, { PROPERTY_UPDATED_AT, updatedAt }, { PROPERTY_ACL,         acl },
        { PROPERTY_DATA,       data.ToJSONObject() }
    };

    public IJSON FromJSONObject(Dictionary<string, object> obj)
    {
        version = obj.GetValue<int>(PROPERTY_VERSION);
        entityId = obj.GetString(PROPERTY_ENTITY_ID);
        entityType = obj.GetString(PROPERTY_ENTITY_TYPE);
        createdAt = obj.GetDateTime(PROPERTY_CREATED_AT);
        updatedAt = obj.GetDateTime(PROPERTY_UPDATED_AT);
        acl = obj.GetACL(PROPERTY_ACL);
        data = obj.GetJSONObject<UserData>(PROPERTY_DATA);

        return this;
    }

    #endregion
}
