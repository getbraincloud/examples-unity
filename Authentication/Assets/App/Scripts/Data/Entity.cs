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
    [JsonName("version")]public int Version;
    [JsonName("entityId")]public string EntityID;
    [JsonName("createdAt")]public DateTime CreatedAt;
    [JsonName("updatedAt")]public DateTime UpdatedAt;
    [JsonName("acl")]public ACL ACL;
    [JsonName("data")]public IJSON Data;

    [JsonName("entityType")] public string EntityType => GetDataType();

    public Entity(IJSON data)
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
        EntityID = json["entityId"] as string;
        CreatedAt = Util.BcTimeToDateTime((long)json["createdAt"]);
        UpdatedAt = Util.BcTimeToDateTime((long)json["updatedAt"]);
        ACL.ReadFromJson(json["acl"] as Dictionary<string, object>);

        if (Data == null && (string)json["entityType"] == UserData.DataType)
        {
            Data = new UserData();
        }

        if (Data != null && json["data"] != null)
        {
            Data.Deserialize(json["data"] as Dictionary<string, object>);
        }
    }

    private static Entity Create() => new Entity()
    {
        Version = -1, // -1 tells the server to create the latest version
        EntityID = string.Empty,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        ACL = ACL.ReadWrite()
    };
}
