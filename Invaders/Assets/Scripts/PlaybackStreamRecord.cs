using System;
using System.Collections.Generic;
using UnityEngine;

public class PlaybackStreamRecord
{
    public List<PlaybackStreamFrame> frames = new List<PlaybackStreamFrame>();
    public int totalFrameCount = -2;
}

public class PlaybackStreamFrame
{
    public float xDelta = 0f;
    public bool createBullet = false;
    public int frameID = -2;
}
