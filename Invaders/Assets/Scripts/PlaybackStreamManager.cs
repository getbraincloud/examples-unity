using BrainCloud;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using BrainCloud.JsonFx.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Random = UnityEngine.Random;

public class PlaybackStreamManager : NetworkBehaviour
{
    private static PlaybackStreamManager _instance;
    public static PlaybackStreamManager Instance => _instance;

    private bool IsDedicatedServer;

    [SerializeField]
    private GameObject playerGhost;
    private GameObject ghostInstanceRef;
    [SerializeField]
    private Transform playerSpawnPoint;

    private List<PlaybackStreamReadData> records = new List<PlaybackStreamReadData>();

    private void Awake()
    {
        IsDedicatedServer = Application.isBatchMode && !Application.isEditor;
    }

    private void Start()
    {
        if (!IsDedicatedServer) return;

        records = PlaybackFetcher.Singleton.GetStoredRecords();
        CreateGhostsFromRecords();
    }

    private void CreateGhostsFromRecords()
    {
        foreach (PlaybackStreamReadData record in records)
        {
            InstantiateGhostServerRPC(record);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void InstantiateGhostServerRPC(PlaybackStreamReadData record)
    {
        if(!IsDedicatedServer) return;

        ghostInstanceRef = Instantiate(playerGhost, playerSpawnPoint.position, Quaternion.identity);
        ghostInstanceRef.GetComponent<PlayerReplayControl>().StartStream(record);
        ghostInstanceRef.GetComponent<NetworkObject>().Spawn();
    }
}
