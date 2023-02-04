using BrainCloud;
using BrainCloud.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;

[Serializable]
public struct BCCustomEntity
{
    // Custom Entity
    public bool IsOwned;
    public int Version;
    public int TimeToLive;
    public string OwnerId;
    public string EntityId;
    public string EntityType;
    public DateTime CreatedAt;
    public DateTime UpdatedAt;

    /// <summary>
    /// Access Control List<br></br>
    /// 0 = Owner Read/Write<br></br>
    /// 1 = Public Read, Owner Write<br></br>
    /// 2 = Public Read/Write<br></br>
    /// Default is 2.
    /// </summary>
    public ACL ACL;

    // Custom Entity Data
    private const string DEFAULT_NAME = "John Smith";
    private const string DEFAULT_POSITION = "forward";

    public string Name;
    public string Position;
    public int Goals;
    public int Assists;

    public static BCCustomEntity Create(string entityType, string name = "", string position = "",
                                        int goals = 0, int assists = 0) => new BCCustomEntity
    {
        IsOwned = true,
        Version = -1, // -1 tells the server to create the latest version
        TimeToLive = -1,
        OwnerId = string.Empty,
        EntityId = string.Empty,
        EntityType = entityType,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        ACL = ACL.ReadWrite(),
        Name = !name.IsNullOrEmpty() ? name : DEFAULT_NAME,
        Position = !position.IsNullOrEmpty() ? position : DEFAULT_POSITION,
        Goals = goals,
        Assists = assists
    };

    public void Update(string entityType = "", string name = "", string position = "", int goals = -1, int assists = -1)
    {
        EntityType = !string.IsNullOrEmpty(entityType) ? entityType : EntityType;
        Name = !name.IsNullOrEmpty() ? name : name;
        Position = !position.IsNullOrEmpty() ? position : Position;
        Goals = goals >= 0 ? goals : Goals;
        Assists = assists >= 0 ? assists : Assists;
    }

    public Dictionary<string, object> DataToJson() => new Dictionary<string, object>
    {
        { "name", Name },
        { "position", Position },
        { "goals", Goals },
        { "assists", Assists }
    };

    public void CreateFromJson(bool isOwned, Dictionary<string, object> data)
    {
        IsOwned = isOwned;
        Version = (int)data["version"];
        TimeToLive = data.ContainsValue("timeToLive") ? (int)data["timeToLive"] : -1;
        OwnerId = data["ownerId"] as string;
        EntityId = data["entityId"] as string;
        EntityType = data["entityType"] as string;
        CreatedAt = Util.BcTimeToDateTime((long)data["createdAt"]);
        UpdatedAt = Util.BcTimeToDateTime((long)data["updatedAt"]);
        ACL.ReadFromJson(data["acl"] as Dictionary<string, object>);

        if (data["data"] is Dictionary<string, object> customData && customData.Count > 0)
        {
            Name = customData["name"] as string;
            Position = customData["position"] as string;
            Goals = (int)customData["goals"];
            Assists = (int)customData["assists"];
        }
    }

    public void UpdateFromJSON(bool isOwned, Dictionary<string, object> data)
    {
        IsOwned = isOwned;
        Version = (int)data["version"];
        TimeToLive = data.ContainsValue("timeToLive") ? (int)data["timeToLive"] : 0;
        OwnerId = data["ownerId"] as string;
        EntityId = data["entityId"] as string;
        CreatedAt = Util.BcTimeToDateTime((long)data["createdAt"]);
        UpdatedAt = Util.BcTimeToDateTime((long)data["updatedAt"]);
        ACL.ReadFromJson(data["acl"] as Dictionary<string, object>);

        if (data["data"] is Dictionary<string, object> customData && customData.Count > 0)
        {
            Name = customData["name"] as string;
            Position = customData["position"] as string;
            Goals = (int)customData["goals"];
            Assists = (int)customData["assists"];
        }
    }
}
