using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using System;
using System.Collections.Generic;

/// <summary>
/// A basic data struct for containing user data.
/// </summary>
[Serializable]
public struct UserData : IJSON
{
    public static readonly string DataType = "user";

    #region Consts

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

    public Dictionary<string, object> GetDictionary() => new()
    {
        { PROPERTY_NAME, Name }, { PROPERTY_AGE, Age }
    };

    public string Serialize() => JsonWriter.Serialize(this);

    public IJSON Deserialize(string json)
    {
        throw new NotImplementedException();
    }

    public IJSON Deserialize(Dictionary<string, object> json)
    {
        Name = (string)json[PROPERTY_NAME];
        Age = (string)json[PROPERTY_AGE];

        return this;
    }

    #endregion
}
