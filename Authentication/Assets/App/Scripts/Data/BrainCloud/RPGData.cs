using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A basic data struct for an RPG character.
/// </summary>
[Serializable]
public struct RPGData : IJSON
{
    public static readonly string DataType = "rpg_character";

    #region Consts

    // Public
    public const int MIN_LEVEL    = 1;
    public const int MAX_LEVEL    = 99;
    public const int MIN_HEALTH   = 100;
    public const int MAX_HEALTH   = 9999;
    public const int MIN_STRENGTH = 1;
    public const int MAX_STRENGTH = 99;
    public const int MIN_DEFENSE  = 0;
    public const int MAX_DEFENSE  = 99;

    // JSON Properties
    private const string PROPERTY_NAME     = "name";
    private const string PROPERTY_JOB      = "job";
    private const string PROPERTY_LEVEL    = "level";
    private const string PROPERTY_HEALTH   = "health";
    private const string PROPERTY_STRENGTH = "strength";
    private const string PROPERTY_DEFENSE  = "defense";

    // Defaults
    private const string DEFAULT_NAME = "Gandalf";
    private const string DEFAULT_JOB  = "wizard";

    #endregion

    [JsonName(PROPERTY_NAME)]     public string Name;
    [JsonName(PROPERTY_JOB)]      public string Job;
    [JsonName(PROPERTY_LEVEL)]    public int Level;
    [JsonName(PROPERTY_HEALTH)]   public int Health;
    [JsonName(PROPERTY_STRENGTH)] public int Strength;
    [JsonName(PROPERTY_DEFENSE)]  public int Defense;

    public RPGData(string name = DEFAULT_NAME, string job = DEFAULT_JOB, int level = MIN_LEVEL,
                   int health = MIN_HEALTH, int strength = MIN_STRENGTH, int defense = MIN_DEFENSE)
    {
        Name = !name.IsEmpty() ? name : DEFAULT_NAME;
        Job = !job.IsEmpty() ? job.ToLower() : DEFAULT_JOB;
        Level = Mathf.Clamp(level, MIN_LEVEL, MAX_LEVEL);
        Health = Mathf.Clamp(health, MIN_HEALTH, MAX_HEALTH);
        Strength = Mathf.Clamp(strength, MIN_STRENGTH, MAX_STRENGTH);
        Defense = Mathf.Clamp(defense, MIN_DEFENSE, MAX_DEFENSE);
    }

    public int GetPower()
    {
        float baseFactor = (Health / (float)MIN_HEALTH) + (Strength + Defense);
        float levelFactor = Mathf.Lerp(0.5f, 5.0f, Mathf.InverseLerp(MIN_LEVEL, MAX_LEVEL, Level));

        return Mathf.RoundToInt(baseFactor * levelFactor);
    }

    #region IJSON

    public string GetDataType() => DataType;

    public Dictionary<string, object> ToJSONObject() => new()
    {
        { PROPERTY_NAME,   Name },   { PROPERTY_JOB,      Job },      { PROPERTY_LEVEL,   Level },
        { PROPERTY_HEALTH, Health }, { PROPERTY_STRENGTH, Strength }, { PROPERTY_DEFENSE, Defense }
    };

    public IJSON FromJSONObject(Dictionary<string, object> obj)
    {
        Name = obj[PROPERTY_NAME].ToString();
        Job = obj[PROPERTY_JOB].ToString().ToLower();
        Level = obj[PROPERTY_LEVEL].ToType<int>();
        Health = obj[PROPERTY_HEALTH].ToType<int>();
        Strength = obj[PROPERTY_STRENGTH].ToType<int>();
        Defense = obj[PROPERTY_DEFENSE].ToType<int>();

        return this;
    }

    #endregion
}
