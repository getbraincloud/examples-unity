using UnityEngine;

public enum EventId {Spawn, Attack, Target, Destroy, Ids}

public class ActionReplayRecord
{
    public Vector3 position;
    public int targetID;
    public EnemyTypes troopType;
    public int troopID;
    public EventId eventId;
    public int frameId;
}
