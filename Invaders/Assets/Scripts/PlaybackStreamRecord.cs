using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlaybackStreamRecord
{
    public List<PlaybackStreamFrame> frames = new List<PlaybackStreamFrame>();
    public int totalFrameCount = -2;
    public float startPosition = 0;
    public string username = string.Empty;
    public PlaybackStreamFrame GetLatestFrame() { return frames[^1]; }
}

public struct PlaybackStreamFrame
{
    public int xDelta;
    public bool createBullet;
    public int frameID;

    public PlaybackStreamFrame(int newFrameID)
    {
        xDelta = 0;
        createBullet = false;
        frameID = newFrameID;
    }

    public PlaybackStreamFrame(int newxDelta, bool newCreateBullet, int newFrameID)
    {
        xDelta = newxDelta;
        createBullet = newCreateBullet;
        frameID = newFrameID;
    }
}
