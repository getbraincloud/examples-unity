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
    private PlayerControl leadingPlayer;

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
        if (IsDedicatedServer) return;

        leadingPlayer = FindObjectOfType<PlayerControl>();

        records = NetworkManager.Singleton.GetComponent<PlaybackFetcher>().GetStoredRecords();
        CreateGhostsFromRecords();
    }

    private void CreateGhostsFromRecords()
    {
        foreach (PlaybackStreamReadData record in records)
        {
            Debug.Log("Creating ghost from record!!!");
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

    private PlaybackStreamReadData GenerateFakeRecord()
    {
        PlaybackStreamRecord output = new PlaybackStreamRecord();
        for (int ii = 0; ii < 150; ii++)
        {
            output.frames.Add(new PlaybackStreamFrame(0, false, ii));
        }
        for (int ii = 0; ii < 600; ii++)
        {
            output.frames.Add(new PlaybackStreamFrame((ii % 100 < 50) ? 1 : -1, ii % 60 == 0, ii));
        }
        output.totalFrameCount = 750;
        return new PlaybackStreamReadData(output);
    }
}
