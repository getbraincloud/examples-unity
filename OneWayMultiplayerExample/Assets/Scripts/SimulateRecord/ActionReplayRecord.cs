using System;
using UnityEngine;

public enum EventId {Spawn, Target, Destroy, Ids, Defender}

[Serializable]
public class ActionReplayRecord
{
    public Vector3 position;
    public EnemyTypes troopType;
    public EventId eventID;
    public int teamID = -2;
    public int entityID = -2;
    public int targetID = -2;
    public int targetTeamID = -2;
    public int frameID = -2;
}
