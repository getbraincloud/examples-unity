using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;

/// <summary>
/// A basic data struct for a hockey player.
/// </summary>
[Serializable]
public struct HockeyStatsData : IJSON
{
    public enum FieldPosition
    {
        Center,
        LeftWing,
        RightWing,
        LeftDefense,
        RightDefense
    }

    public static readonly Dictionary<FieldPosition, string> FieldPositions = new Dictionary<FieldPosition, string>
    {
        { FieldPosition.Center,      "Center" },
        { FieldPosition.LeftWing,    "Left-Winger" },        { FieldPosition.RightWing, "Right-Winger" },
        { FieldPosition.LeftDefense, "Left-Defenseperson" }, { FieldPosition.RightDefense, "Right-Defenseperson" }
    };

    public static readonly string DataType = "hockey_player_stats";

    private const string DEFAULT_NAME = "Wayne Gretzky";

    [JsonName("name")] public string Name;
    [JsonName("position")] public FieldPosition Position;
    [JsonName("goals")] public int Goals;
    [JsonName("assists")] public int Assists;

    public int GetPoints() => Goals + Assists;

    public string GetPosition() => FieldPositions[Position];

    public HockeyStatsData(string name = DEFAULT_NAME, FieldPosition position = FieldPosition.Center, int goals = 0, int assists = 0)
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
        Position = (FieldPosition)json["position"];
        Goals = (int)json["goals"];
        Assists = (int)json["assists"];
    }
}
