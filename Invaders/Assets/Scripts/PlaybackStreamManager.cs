using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlaybackStreamManager : NetworkBehaviour
{
    private BrainCloudWrapper _bcWrapper;

    public BrainCloudWrapper Wrapper
    {
        get => _bcWrapper;
    }

    private static PlaybackStreamManager _instance;
    public static PlaybackStreamManager Instance => _instance;

    private bool _dead;

    private bool IsDedicatedServer;

    [SerializeField]
    private GameObject playerGhost;
    private GameObject ghostInstanceRef;
    [SerializeField]
    private Transform playerSpawnPoint;

    private void Awake()
    {
        IsDedicatedServer = Application.isBatchMode && !Application.isEditor;
        _bcWrapper = FindAnyObjectByType<BrainCloudWrapper>();
    }

    private void Start()
    {
        //if (!IsDedicatedServer) return;

        List<PlaybackStreamRecord> records = new List<PlaybackStreamRecord>();
        records.Add(GenerateFakeRecord());

        foreach (PlaybackStreamRecord record in records)
        {
            ghostInstanceRef = Instantiate(playerGhost, playerSpawnPoint.position, Quaternion.identity);
            //ghostInstanceRef.GetComponent<NetworkObject>().Spawn();
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

    private void OnReadStreamSuccess(string in_jsonResponse, object cbObject)
    {

    }

    private void OnFailureCallback(int status, int reasonCode, string jsonError, object cbObject)
    {
        if (_dead) return;
        _bcWrapper.Client.ResetCommunication();
        _dead = true;

        string message = cbObject as string;

        if (!SceneManager.GetActiveScene().name.Contains("Game"))
        {
            //MenuManager.Instance.AbortToSignIn($"Message: {message} |||| JSON: {jsonError}");
        }
    }
}
