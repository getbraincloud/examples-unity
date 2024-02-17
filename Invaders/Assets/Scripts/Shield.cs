using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

public class Shield : MonoBehaviour
{
    public bool IsDedicatedServer
    {
        get => BrainCloudManager.Singleton.IsDedicatedServer;
    }

    protected void Start()
    {
        if (IsDedicatedServer) InvadersGame.Singleton.RegisterSpawnableObject(InvadersObjectType.Shield, gameObject);
    }

    protected void OnDestroy()
    {
        if (IsDedicatedServer) InvadersGame.Singleton.UnregisterSpawnableObject(InvadersObjectType.Shield, gameObject);
    }
}
