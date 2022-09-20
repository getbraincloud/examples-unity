using UnityEngine;

public enum EventId {Spawn, Target, Destroy, Ids}

public class ActionReplayRecord
{
    public Vector3 position;
    public EnemyTypes troopType;
    public EventId eventID;
    public int teamID;
    public int troopID;
    public int targetID;
    public int frameID;
}
