using System;
using System.Collections.Generic;
using UnityEngine;

public class PlaybackStreamRecord
{
    public List<PlaybackStreamFrame> frames = new List<PlaybackStreamFrame>();
    public int totalFrameCount = -2;
    public PlaybackStreamFrame GetLatestFrame() { return frames[^1]; }
}

public class PlaybackStreamFrame
{
    public PlaybackStreamFrame(int newFrameID)
    {
        frameID = newFrameID;
    }
    public int xDelta = 0;
    public bool createBullet = false;
    public int frameID = -2;
}
