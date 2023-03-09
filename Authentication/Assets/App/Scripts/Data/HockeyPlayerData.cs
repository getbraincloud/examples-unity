using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;

/// <summary>
/// A basic data struct for a hockey player.
/// </summary>
[Serializable]
public struct HockeyPlayerData : IJSON
{
    private const string DEFAULT_NAME = "Wayne Gretzky";
    private const string DEFAULT_POSITION = "centre";

    [JsonName("name")] public string Name;
    [JsonName("position")] public string Position;
    [JsonName("goals")] public int Goals;
    [JsonName("assists")] public int Assists;

    public HockeyPlayerData(string name = "", string position = "", int goals = 0, int assists = 0)
    {
        Name = !name.IsEmpty() ? name : DEFAULT_NAME;
        Position = !position.IsEmpty() ? position : DEFAULT_POSITION;
        Goals = goals >= 0 ? goals : 0;
        Assists = assists >= 0 ? assists : 0;
    }

    public string Serialize() => JsonWriter.Serialize(this);

    public void Deserialize(Dictionary<string, object> json)
    {
        Name = json["name"] as string;
        Position = json["position"] as string;
        Goals = (int)json["goals"];
        Assists = (int)json["assists"];
    }
}
