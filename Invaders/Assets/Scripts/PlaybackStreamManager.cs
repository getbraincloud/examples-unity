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
        if (!IsDedicatedServer) return;

        records.Add(GenerateFakeRecord());

        foreach (PlaybackStreamRecord record in records)
        {
            ghostInstanceRef = Instantiate(playerGhost, playerSpawnPoint.position, Quaternion.identity);
            ghostInstanceRef.GetComponent<NetworkObject>().Spawn();
            ghostInstanceRef.GetComponent<PlayerReplayControl>().StartStream(FindObjectOfType<PlayerControl>(), record);
        }
    }

    private PlaybackStreamRecord GenerateFakeRecord()
    {
        PlaybackStreamRecord output = new PlaybackStreamRecord();
        for (int ii = 0; ii < 500; ii++)
        {
            output.frames.Add(new PlaybackStreamFrame(ii));
            output.frames[ii].xDelta = (ii % 100 < 50) ? 1 : -1;
            output.frames[ii].createBullet = ii % 10 == 0;
        }
        output.totalFrameCount = 500;
        return output;
    }
}
