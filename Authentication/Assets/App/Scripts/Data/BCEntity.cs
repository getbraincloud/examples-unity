using BrainCloud;
using BrainCloud.Common;
using System;
using System.Collections.Generic;

/// <summary>
/// A basic data struct for brainCloud's user entities.
/// </summary>
[Serializable]
public struct BCEntity
{
    public int Version;
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

    public static string DEFAULT_NAME = "New User";
    public static string DEFAULT_AGE = "?";

    public string Name;
    public string Age;

    public static BCEntity Create(string entityType, string name = "", string age = "") => new BCEntity
    {
        Version = -1, // -1 tells the server to create the latest version
        Name = !name.IsEmpty() ? name : DEFAULT_NAME,
        Age = !age.IsEmpty() ? age : DEFAULT_AGE,
        EntityId = string.Empty,
        EntityType = entityType,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        ACL = ACL.ReadWrite()
    };

    public void Update(string name, string age, string entityType = "")
    {
        Name = !name.IsEmpty() ? name : DEFAULT_NAME;
        Age = !age.IsEmpty() ? age : DEFAULT_AGE;
        EntityType = !entityType.IsEmpty() ? entityType : EntityType;
    }

    public Dictionary<string, object> DataToJson() => new Dictionary<string, object>
    {
        { "name", Name },
        { "age", Age }
    };

    public void CreateFromJson(Dictionary<string, object> data)
    {
        Version = (int)data["version"];
        EntityId = data["entityId"] as string;
        EntityType = data["entityType"] as string;        
        CreatedAt = Util.BcTimeToDateTime((long)data["createdAt"]);
        UpdatedAt = Util.BcTimeToDateTime((long)data["updatedAt"]);
        ACL.ReadFromJson(data["acl"] as Dictionary<string, object>);

        if (data["data"] is Dictionary<string, object> entityData && entityData.Count > 0)
        {
            Name = entityData["name"] as string;
            Age = entityData["age"] as string;
        }
    }

    public void UpdateFromJSON(Dictionary<string, object> data)
    {
        EntityId = data["entityId"] as string;
        Version = (int)data["version"];
        CreatedAt = Util.BcTimeToDateTime((long)data["createdAt"]);
        UpdatedAt = Util.BcTimeToDateTime((long)data["updatedAt"]);
        ACL.ReadFromJson(data["acl"] as Dictionary<string, object>);

        if (data["data"] is Dictionary<string, object> entityData && entityData.Count > 0)
        {
            Name = entityData["name"] as string;
            Age = entityData["age"] as string;
        }
    }
}
