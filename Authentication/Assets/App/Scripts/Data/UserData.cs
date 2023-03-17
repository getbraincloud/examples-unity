using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;

/// <summary>
/// A basic data struct for containing user data.
/// </summary>
[Serializable]
public struct UserData : IJSON
{
    #region Consts

    public static readonly string DataType = "user";

    // JSON Properties
    private const string PROPERTY_NAME = "name";
    private const string PROPERTY_AGE  = "age";

    // Defaults
    private const string DEFAULT_NAME = "New User";
    private const string DEFAULT_AGE  = "?";

    #endregion

    [JsonName(PROPERTY_NAME)] public string Name;
    [JsonName(PROPERTY_AGE)]  public string Age;

    public UserData(string name = "", string age = "")
    {
        Name = name.IsEmpty() ? DEFAULT_NAME : name;
        Age = age.IsEmpty() ? DEFAULT_AGE : age;
    }

    #region IJSON

    public string GetDataType() => DataType;

    public Dictionary<string, object> GetDictionary() => new Dictionary<string, object>
    {
        { PROPERTY_NAME, Name }, { PROPERTY_AGE, Age }
    };

    public string Serialize() => JsonWriter.Serialize(this);

    public void Deserialize(Dictionary<string, object> json)
    {
        Name = json[PROPERTY_NAME] as string;
        Age = json[PROPERTY_AGE] as string;
    }

    #endregion
}
