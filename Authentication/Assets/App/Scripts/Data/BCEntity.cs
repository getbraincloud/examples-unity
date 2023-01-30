using BrainCloud;
using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;

[Serializable]
public struct BCEntity
{
    public int Version;
    public string Name;
    public string Age;
    public string EntityId;
    public string EntityType;
    public DateTime CreatedAt;
    public DateTime UpdatedAt;

    /// <summary>
    /// Access Control List<br></br>
    /// 0 = Owner Read/Write<br></br>
    /// 1 = Public Read, Owner Write<br></br>
    /// 2 = Public Read/Write
    /// </summary>
    public ACL ACL;

    public static BCEntity CreateEmpty() => new BCEntity
    {
        Version = -1, // -1 tells the server to create the latest version
        Name = "New User",
        Age = "?",
        EntityId = string.Empty,
        EntityType = "user",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        ACL = new ACL(ACL.Access.None)
    };

    public static BCEntity CreateNew(string name, string age) => new BCEntity
    {
        Version = -1, // -1 tells the server to create the latest version
        Name = name,
        Age = age,
        EntityId = string.Empty,
        EntityType = "user",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        ACL = ACL.ReadWrite()
    };

    public string EntityDataToJSON()
    {
        var entity = new Dictionary<string, object>
        {
            { "name", Name },
            { "age", Age }
        };

        return JsonWriter.Serialize(entity);
    }

    public void CreateFromPageItemJSON(Dictionary<string, object> pageItem)
    {
        EntityId = pageItem["entityId"] as string;
        EntityType = pageItem["entityType"] as string;
        Version = (int)pageItem["version"];
        CreatedAt = Util.BcTimeToDateTime((long)pageItem["createdAt"]);
        UpdatedAt = Util.BcTimeToDateTime((long)pageItem["updatedAt"]);

        Dictionary<string, object> data = pageItem["data"] as Dictionary<string, object>;
        Name = data["name"] as string;
        Age = data["age"] as string;
    }

    public void UpdateFromJSON(Dictionary<string, object> data)
    {
        EntityId = data["entityId"] as string;
        Version = (int)data["version"];
        CreatedAt = Util.BcTimeToDateTime((long)data["createdAt"]);
        UpdatedAt = Util.BcTimeToDateTime((long)data["updatedAt"]);
    }
}
