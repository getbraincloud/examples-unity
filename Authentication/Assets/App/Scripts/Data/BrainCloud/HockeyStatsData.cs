using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using System;
using System.Collections.Generic;

/// <summary>
/// A basic data struct for a hockey player.
/// </summary>
[Serializable]
public struct HockeyStatsData : IJSON
{
    public static readonly string DataType = "hockey_player_stats";

    public enum FieldPosition
    {
        Center,
        LeftWing,
        RightWing,
        LeftDefense,
        RightDefense
    }

    public static readonly Dictionary<FieldPosition, string> FieldPositions = new()
    {
        { FieldPosition.Center,      "Center" },
        { FieldPosition.LeftWing,    "Left-Winger" },        { FieldPosition.RightWing, "Right-Winger" },
        { FieldPosition.LeftDefense, "Left-Defenseperson" }, { FieldPosition.RightDefense, "Right-Defenseperson" }
    };

    #region Consts

    // JSON Properties
    private const string PROPERTY_NAME     = "name";
    private const string PROPERTY_POSITION = "position";
    private const string PROPERTY_GOALS    = "goals";
    private const string PROPERTY_ASSISTS  = "assists";

    // Defaults
    private const string DEFAULT_NAME = "John Smith";

    #endregion

    [JsonName(PROPERTY_NAME)]     public string Name;
    [JsonName(PROPERTY_POSITION)] public int PositionValue;
    [JsonName(PROPERTY_GOALS)]    public int Goals;
    [JsonName(PROPERTY_ASSISTS)]  public int Assists;

    public FieldPosition Position => (FieldPosition)PositionValue;

    public HockeyStatsData(string name = DEFAULT_NAME, FieldPosition position = FieldPosition.Center, int goals = 0, int assists = 0)
    {
        Name = !name.IsEmpty() ? name : DEFAULT_NAME;
        PositionValue = (int)position;
        Goals = goals >= 0 ? goals : 0;
        Assists = assists >= 0 ? assists : 0;
    }

    public int GetPoints() => Goals + Assists;

    public string GetPosition() => FieldPositions[Position];

    #region IJSON

    public string GetDataType() => DataType;

    public Dictionary<string, object> ToJSONObject() => new()
    {
        { PROPERTY_NAME,  Name },  { PROPERTY_POSITION, PositionValue },
        { PROPERTY_GOALS, Goals }, { PROPERTY_ASSISTS,  Assists }
    };

    public IJSON FromJSONObject(Dictionary<string, object> obj)
    {
        Name = obj[PROPERTY_NAME].ToString();
        PositionValue = obj[PROPERTY_POSITION].ToType<int>();
        Goals = obj[PROPERTY_GOALS].ToType<int>();
        Assists = obj[PROPERTY_ASSISTS].ToType<int>();

        return this;
    }

    #endregion
}
