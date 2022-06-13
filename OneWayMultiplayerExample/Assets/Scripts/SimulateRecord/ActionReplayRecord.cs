using UnityEngine;

public enum EventId {Spawn, Attack, Destroy}

public class ActionReplayRecord
{
    public Vector3 position;
    public Quaternion rotation;
    public EnemyTypes troopType;
    public TroopStates currentState;
    public EventId eventId;
    public int frameId;
}
