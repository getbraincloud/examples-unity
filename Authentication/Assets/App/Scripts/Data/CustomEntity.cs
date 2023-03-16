using BrainCloud;
using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;

/// <summary>
/// A basic data struct for brainCloud's custom entities.
/// </summary>
[Serializable]
public struct CustomEntity
{
    public bool IsOwned => OwnerID == UserHandler.ProfileID;

    [JsonName("version")] public int Version;
    [JsonName("ownerId")] public string OwnerID;
    [JsonName("entityId")] public string EntityID;
    [JsonName("createdAt")] public DateTime CreatedAt;
    [JsonName("updatedAt")] public DateTime UpdatedAt;
    [JsonName("timeToLive")] public TimeSpan TimeToLive;
    [JsonName("expiresAt")] public DateTime ExpiresAt;
    [JsonName("acl")] public ACL ACL;
    [JsonName("data")] public IJSON Data;

    [JsonName("entityType")] public string EntityType => GetDataType();

    public TimeSpan ExpiresIn => ExpiresAt - DateTime.UtcNow;

    public CustomEntity(IJSON data)
    {
        this = Create();
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

    public string GetDataType() => Data != null ? Data.GetDataType() : "undefined";

    public string Serialize() => JsonWriter.Serialize(this);

    public void Deserialize(Dictionary<string, object> json)
    {
        Version = (int)json["version"];
        OwnerID = json["ownerId"] as string;
        EntityID = json["entityId"] as string;
        CreatedAt = Util.BcTimeToDateTime((long)json["createdAt"]);
        UpdatedAt = Util.BcTimeToDateTime((long)json["updatedAt"]);

        if (json.ContainsValue(json["timeToLive"]))
        {
            if (json["timeToLive"].GetType() == typeof(long))
            {
                TimeToLive = TimeSpan.FromMilliseconds((long)json["timeToLive"]);
            }
            else
            {
                TimeToLive = TimeSpan.FromMilliseconds((int)json["timeToLive"]);
            }

            ExpiresAt = Util.BcTimeToDateTime((long)json["expiresAt"]);
        }
        else
        {
            TimeToLive = TimeSpan.MaxValue;
            ExpiresAt = DateTime.MaxValue;
        }

        ACL ??= new ACL();
        ACL.ReadFromJson(json["acl"] as Dictionary<string, object>);

        if (Data == null)
        {
            string entityType = (string)json["entityType"];
            if (entityType == HockeyStatsData.DataType)
            {
                Data = new HockeyStatsData();
            }
            else if (entityType == RPGData.DataType)
            {
                Data = new RPGData();
            }
        }

        if (Data != null && json.ContainsKey("data"))
        {
            Data.Deserialize(json["data"] as Dictionary<string, object>);
        }
    }

    private static CustomEntity Create() => new CustomEntity()
    {
        Version = -1, // -1 tells the server to create the latest version
        OwnerID = string.Empty,
        EntityID = string.Empty,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        TimeToLive = TimeSpan.MaxValue,
        ExpiresAt = DateTime.MaxValue,
        ACL = ACL.ReadWrite()
    };
}
