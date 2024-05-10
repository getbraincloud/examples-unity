using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlaybackStreamRecord : INetworkSerializable
{
    public List<PlaybackStreamFrame> frames = new List<PlaybackStreamFrame>();
    public int totalFrameCount = -2;
    public PlaybackStreamFrame GetLatestFrame() { return frames[^1]; }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref totalFrameCount);
    }
}

public class PlaybackStreamFrame : INetworkSerializable
{
    public PlaybackStreamFrame(int newFrameID)
    {
        frameID = newFrameID;
    }
    public int xDelta = 0;
    public bool createBullet = false;
    public int frameID = -2;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref xDelta);
        serializer.SerializeValue(ref createBullet);
        serializer.SerializeValue(ref frameID);
    }
}
