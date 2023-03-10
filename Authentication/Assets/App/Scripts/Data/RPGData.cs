using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A basic data struct for an RPG character.
/// </summary>
[Serializable]
public struct RPGData : IJSON
{
    public static readonly string DataType = "rpg";

    private const int MIN_LEVEL = 1;
    private const int MAX_LEVEL = 99;
    private const int MIN_HEALTH = 100;
    private const int MAX_HEALTH = 9999;
    private const int MIN_STRENGTH = 1;
    private const int MAX_STRENGTH = 99;
    private const int MIN_DEFENSE = 0;
    private const int MAX_DEFENSE = 99;
    private const string DEFAULT_NAME = "Gandalf";
    private const string DEFAULT_JOB = "wizard";

    [JsonName("name")] public string Name;
    [JsonName("job")] public string Job;
    [JsonName("level")] public int Level;
    [JsonName("health")] public int Health;
    [JsonName("strength")] public int Strength;
    [JsonName("defense")] public int Defense;

    public int GetPower()
    {
        float baseFactor = (Health / (float)MIN_HEALTH) + (Strength + Defense);
        float levelFactor = Mathf.Lerp(0.5f, 5.0f, Mathf.InverseLerp(MIN_LEVEL, MAX_LEVEL, Level));

        return Mathf.RoundToInt(baseFactor * levelFactor);
    }

    public RPGData(string name = DEFAULT_NAME, string job = DEFAULT_JOB, int level = MIN_LEVEL,
                   int health = MIN_HEALTH, int strength = MIN_STRENGTH, int defense = MIN_DEFENSE)
    {
        Name = !name.IsEmpty() ? name : DEFAULT_NAME;
        Job = !job.IsEmpty() ? job : DEFAULT_JOB;
        Level = Mathf.Clamp(level, MIN_LEVEL, MAX_LEVEL);
        Health = Mathf.Clamp(health, MIN_HEALTH, MAX_HEALTH);
        Strength = Mathf.Clamp(strength, MIN_STRENGTH, MAX_STRENGTH);
        Defense = Mathf.Clamp(defense, MIN_DEFENSE, MAX_DEFENSE);
    }

    public string GetDataType() => DataType;

    public string Serialize() => JsonWriter.Serialize(this);

    public void Deserialize(Dictionary<string, object> json)
    {
        Name = json["name"] as string;
        Job = json["job"] as string;
        Level = (int)json["level"];
        Health = (int)json["health"];
        Strength = (int)json["strength"];
        Defense = (int)json["defense"];
    }
}
