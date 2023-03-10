using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;

/// <summary>
/// A basic data struct for a hockey player.
/// </summary>
[Serializable]
public struct HockeyStatsData : IJSON
{
    public enum SkaterPosition
    {
        Center,
        LeftWing,
        RightWing,
        LeftDefense,
        RightDefense
    }

    public static readonly string DataType = "hockey_player";

    private const string DEFAULT_NAME = "Wayne Gretzky";

    private static readonly Dictionary<SkaterPosition, string> POSITIONS = new Dictionary<SkaterPosition, string>
    {
        { SkaterPosition.Center, "Center" },
        { SkaterPosition.LeftWing, "Left-Winger" }, { SkaterPosition.RightWing, "Right-Winger" },
        { SkaterPosition.LeftDefense, "Left-Defenseperson" }, { SkaterPosition.RightDefense, "Right-Defenseperson" }
    };

    [JsonName("name")] public string Name;
    [JsonName("position")] public SkaterPosition Position;
    [JsonName("goals")] public int Goals;
    [JsonName("assists")] public int Assists;

    public int GetPoints() => Goals + Assists;

    public string GetPosition() => POSITIONS[Position];

    public HockeyStatsData(string name = DEFAULT_NAME, SkaterPosition position = SkaterPosition.Center, int goals = 0, int assists = 0)
    {
        Name = !name.IsEmpty() ? name : DEFAULT_NAME;
        Position = position;
        Goals = goals >= 0 ? goals : 0;
        Assists = assists >= 0 ? assists : 0;
    }

    public string GetDataType() => DataType;

    public string Serialize() => JsonWriter.Serialize(this);

    public void Deserialize(Dictionary<string, object> json)
    {
        Name = json["name"] as string;
        Position = (SkaterPosition)json["position"];
        Goals = (int)json["goals"];
        Assists = (int)json["assists"];
    }
}
