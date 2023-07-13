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

    #endregion

    [JsonName(PROPERTY_NAME)]     public string name;
    [JsonName(PROPERTY_JOB)]      public string job;
    [JsonName(PROPERTY_LEVEL)]    public int level;
    [JsonName(PROPERTY_HEALTH)]   public int health;
    [JsonName(PROPERTY_STRENGTH)] public int strength;
    [JsonName(PROPERTY_DEFENSE)]  public int defense;

    public RPGData(string name, string job, int level, int health, int strength, int defense)
    {
        this.name = name;
        this.job = job.ToLower();
        this.level = Mathf.Clamp(level, MIN_LEVEL, MAX_LEVEL);
        this.health = Mathf.Clamp(health, MIN_HEALTH, MAX_HEALTH);
        this.strength = Mathf.Clamp(strength, MIN_STRENGTH, MAX_STRENGTH);
        this.defense = Mathf.Clamp(defense, MIN_DEFENSE, MAX_DEFENSE);
    }

    public int GetPower()
    {
        float baseFactor = (health / (float)MIN_HEALTH) + (strength + defense);
        float levelFactor = Mathf.Lerp(0.5f, 5.0f, Mathf.InverseLerp(MIN_LEVEL, MAX_LEVEL, level));

        return Mathf.RoundToInt(baseFactor * levelFactor);
    }

    #region IJSON

    public string GetDataType() => DataType;

    public Dictionary<string, object> ToJSONObject() => new()
    {
        { PROPERTY_NAME,   name },   { PROPERTY_JOB,      job.ToLower() }, { PROPERTY_LEVEL,   level },
        { PROPERTY_HEALTH, health }, { PROPERTY_STRENGTH, strength      }, { PROPERTY_DEFENSE, defense }
    };

    public IJSON FromJSONObject(Dictionary<string, object> obj)
    {
        name = obj.GetString(PROPERTY_NAME);
        job = obj.GetString(PROPERTY_JOB).ToLower();
        level = obj.GetValue<int>(PROPERTY_LEVEL);
        health = obj.GetValue<int>(PROPERTY_HEALTH);
        strength = obj.GetValue<int>(PROPERTY_STRENGTH);
        defense = obj.GetValue<int>(PROPERTY_DEFENSE);

        return this;
    }

    #endregion
}
