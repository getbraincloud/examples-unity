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

    #endregion

    [JsonName(PROPERTY_NAME)]     public string name;
    [JsonName(PROPERTY_POSITION)] public int position;
    [JsonName(PROPERTY_GOALS)]    public int goals;
    [JsonName(PROPERTY_ASSISTS)]  public int assists;

    public FieldPosition PlayerPosition => (FieldPosition)position;

    public HockeyStatsData(string name, FieldPosition position, int goals, int assists)
    {
        this.name = name;
        this.position = (int)position;
        this.goals = goals >= 0 ? goals : 0;
        this.assists = assists >= 0 ? assists : 0;
    }

    public int GetPoints() => goals + assists;

    public string GetPosition() => FieldPositions[PlayerPosition];

    #region IJSON

    public string GetDataType() => DataType;

    public Dictionary<string, object> ToJSONObject() => new()
    {
        { PROPERTY_NAME,  name },  { PROPERTY_POSITION, position },
        { PROPERTY_GOALS, goals }, { PROPERTY_ASSISTS,  assists }
    };

    public IJSON FromJSONObject(Dictionary<string, object> obj)
    {
        name = obj.GetString(PROPERTY_NAME);
        position = obj.GetValue<int>(PROPERTY_POSITION);
        goals = obj.GetValue<int>(PROPERTY_GOALS);
        assists = obj.GetValue<int>(PROPERTY_ASSISTS);

        return this;
    }

    #endregion
}
