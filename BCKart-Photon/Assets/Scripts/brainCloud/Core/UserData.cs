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

    #endregion

    [JsonName(PROPERTY_NAME)] public string name;
    [JsonName(PROPERTY_AGE)]  public string age;

    public UserData(string name, string age)
    {
        this.name = name;
        this.age = age;
    }

    #region IJSON

    public string GetDataType() => DataType;

    public Dictionary<string, object> ToJSONObject() => new()
    {
        { PROPERTY_NAME, name }, { PROPERTY_AGE, age }
    };

    public IJSON FromJSONObject(Dictionary<string, object> obj)
    {
        name = obj.GetString(PROPERTY_NAME);
        age = obj.GetString(PROPERTY_AGE);

        return this;
    }

    #endregion
}
