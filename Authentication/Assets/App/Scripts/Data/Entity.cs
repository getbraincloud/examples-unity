using BrainCloud;
using BrainCloud.Common;
using System;
using System.Collections.Generic;

[Serializable]
public struct Entity
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
    /// 0 = Owner can Read/Write<br></br>
    /// 1 = Public Read, Owner can Write<br></br>
    /// 2 = Public Read/Write
    /// </summary>
    public ACL ACL;

    public static Entity CreateEmpty() => new Entity
    {
        Version = -1, // -1 tells the server to create the latest version
        Name = "New User",
        Age = string.Empty,
        EntityId = string.Empty,
        EntityType = "user",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        ACL = new ACL(ACL.Access.None)
    };

    public static Entity CreateNew(string name, string age) => new Entity
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

    public Dictionary<string, object> EntityDataToJSON() => new Dictionary<string, object>
    {
        { "name", Name },
        { "age", Age }
    };

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
