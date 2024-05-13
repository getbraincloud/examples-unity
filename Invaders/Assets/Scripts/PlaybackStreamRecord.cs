using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlaybackStreamRecord
{
    public List<PlaybackStreamFrame> frames = new List<PlaybackStreamFrame>();
    public int totalFrameCount = -2;
    public PlaybackStreamFrame GetLatestFrame() { return frames[^1]; }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref totalFrameCount);
        foreach(PlaybackStreamFrame ii in frames)
        {
            ii.NetworkSerialize(serializer);
        }
    }
}

public struct PlaybackStreamReadData : INetworkSerializable
{
    public PlaybackStreamFrame[] frames;
    public int totalFrameCount;

    public PlaybackStreamReadData(PlaybackStreamFrame[] newFrames, int newFrameCount)
    {
        frames = newFrames;
        totalFrameCount = newFrameCount;
    }

    public PlaybackStreamReadData(PlaybackStreamRecord copyRecord)
    {
        frames = copyRecord.frames.ToArray();
        totalFrameCount = copyRecord.totalFrameCount;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref totalFrameCount);
        foreach (PlaybackStreamFrame ii in frames)
        {
            ii.NetworkSerialize(serializer);
        }
    }
}

public struct PlaybackStreamFrame : INetworkSerializable
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

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref xDelta);
        serializer.SerializeValue(ref createBullet);
        serializer.SerializeValue(ref frameID);
    }
}
