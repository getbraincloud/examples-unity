using BrainCloud;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using BrainCloud.JsonFx.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

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

    private List<PlaybackStreamRecord> records = new List<PlaybackStreamRecord>();

    private void Awake()
    {
        IsDedicatedServer = Application.isBatchMode && !Application.isEditor;
    }

    private void Start()
    {
        if (IsDedicatedServer) return;

        leadingPlayer = FindObjectOfType<PlayerControl>();

        FindRecords();
        CreateGhostsFromRecords();
    }

    private void FindRecords()
    {
        records = NetworkManager.Singleton.GetComponent<PlaybackFetcher>().GetStoredRecords();
        records.Add(GenerateFakeRecord());
    }

    private void CreateGhostsFromRecords()
    {
        foreach (PlaybackStreamRecord record in records)
        {
            Debug.Log("Creating ghost from record!!!");
            InstantiateGhostServerRPC(record);
        }
    }

    [ServerRpc]
    private void InstantiateGhostServerRPC(PlaybackStreamRecord record)
    {
        ghostInstanceRef = Instantiate(playerGhost, playerSpawnPoint.position, Quaternion.identity);
        ghostInstanceRef.GetComponent<PlayerReplayControl>().StartStream(leadingPlayer, record);
        ghostInstanceRef.GetComponent<NetworkObject>().Spawn();
    }

    private PlaybackStreamRecord GenerateFakeRecord()
    {
        PlaybackStreamRecord output = new PlaybackStreamRecord();
        for (int ii = 0; ii < 150; ii++)
        {
            output.frames.Add(new PlaybackStreamFrame(ii));
            output.GetLatestFrame().xDelta = 0;
        }
        for (int ii = 0; ii < 600; ii++)
        {
            output.frames.Add(new PlaybackStreamFrame(ii));
            output.GetLatestFrame().xDelta = (ii % 100 < 50) ? 1 : -1;
            output.GetLatestFrame().createBullet = ii % 60 == 0;
        }
        output.totalFrameCount = 750;
        return output;
    }
}
