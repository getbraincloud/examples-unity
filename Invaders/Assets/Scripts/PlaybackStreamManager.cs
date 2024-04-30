using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaybackStreamManager : MonoBehaviour
{
    private static PlaybackStreamManager _instance;
    public static PlaybackStreamManager Instance => _instance;

    [SerializeField]
    private GameObject playerGhost;
    private GameObject ghostInstanceRef;

    private void Start()
    {
        List<PlaybackStreamRecord> records = new List<PlaybackStreamRecord>();
        records.Add(GenerateFakeRecord());

        foreach (PlaybackStreamRecord record in records)
        {
            ghostInstanceRef = Instantiate(playerGhost, transform);
            ghostInstanceRef.GetComponent<PlayerReplayControl>().StartStream(FindObjectOfType<PlayerControl>(), record);
        }
    }

    private PlaybackStreamRecord GenerateFakeRecord()
    {
        PlaybackStreamRecord output = new PlaybackStreamRecord();
        for (int ii = 0; ii < 500; ii++)
        {
            output.frames.Add(new PlaybackStreamFrame());
            output.frames[ii].xDelta = ii % 100 < 50 ? 1 : -1;
            output.frames[ii].createBullet = true;
        }
        output.totalFrameCount = 500;
        return output;
    }
}
