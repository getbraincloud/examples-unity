using BrainCloud;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using BrainCloud.JsonFx.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Random = UnityEngine.Random;

public class GhostSpawner : NetworkBehaviour
{
    private bool IsDedicatedServer;

    [SerializeField]
    private GameObject playerGhost;
    private GameObject ghostInstanceRef;
    [SerializeField]
    private Transform playerSpawnPoint;

    private int frameCountSinceStart = 0;

    private void Awake()
    {
        IsDedicatedServer = Application.isBatchMode && !Application.isEditor;
    }

    private void FixedUpdate()
    {
        frameCountSinceStart += 1;
    }

    public void InstantiateGhost(PlaybackStreamRecord record)
    {
        if (!IsDedicatedServer) return;

        ghostInstanceRef = Instantiate(playerGhost, playerSpawnPoint.position, Quaternion.identity);
        ghostInstanceRef.GetComponent<PlayerReplayControl>().StartStream(record, frameCountSinceStart);
        ghostInstanceRef.GetComponent<NetworkObject>().Spawn();
    }
}
