using UnityEngine;

public enum EventId {Spawn, Target, Destroy, Ids, Defender}

public class ActionReplayRecord
{
    public Vector3 position;
    public EnemyTypes troopType;
    public EventId eventID;
    public int teamID = -1;
    public int entityID = -1;
    public int targetID = -1;
    public int targetTeamID = -1;
    public int frameID = -1;
}
