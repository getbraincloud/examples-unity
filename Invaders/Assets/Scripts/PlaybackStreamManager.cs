using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlaybackStreamManager : MonoBehaviour
{
    private static PlaybackStreamManager _instance;
    public static PlaybackStreamManager Instance => _instance;

    private bool IsDedicatedServer;

    [SerializeField]
    private GameObject playerGhost;
    private GameObject ghostInstanceRef;

    private void Awake()
    {
        IsDedicatedServer = Application.isBatchMode && !Application.isEditor;
    }

    private void Start()
    {
        //if (!IsDedicatedServer) return;

        List<PlaybackStreamRecord> records = new List<PlaybackStreamRecord>();
        records.Add(GenerateFakeRecord());

        foreach (PlaybackStreamRecord record in records)
        {
            ghostInstanceRef = Instantiate(playerGhost, transform.parent);
            ghostInstanceRef.GetComponent<NetworkObject>().Spawn();
            ghostInstanceRef.transform.position = Vector3.zero;
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
            output.frames[ii].createBullet = true;
        }
        output.totalFrameCount = 500;
        return output;
    }
}
