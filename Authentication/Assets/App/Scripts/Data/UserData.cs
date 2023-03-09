using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;

/// <summary>
/// A basic data struct for containing user data.
/// </summary>
[Serializable]
public struct UserData : IJSON
{
    private const string DEFAULT_NAME = "New User";
    private const string DEFAULT_AGE = "?";

    [JsonName("name")] public string Name;
    [JsonName("age")] public string Age;

    public UserData(string name = "", string age = "")
    {
        Name = name.IsEmpty() ? DEFAULT_NAME : name;
        Age = age.IsEmpty() ? DEFAULT_AGE : age;
    }

    public string Serialize() => JsonWriter.Serialize(this);

    public void Deserialize(Dictionary<string, object> json)
    {
        Name = json["name"] as string;
        Age = json["age"] as string;
    }
}
