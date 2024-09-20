using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public struct NetworkStringArray : INetworkSerializable
{
    public string[] elements;

    public NetworkStringArray(List<string> copyList)
    {
        elements = copyList.ToArray();
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        var length = 0;
        if (!serializer.IsReader)
            length = elements.Length;

        serializer.SerializeValue(ref length);

        if (serializer.IsReader)
            elements = new string[length];

        for (var n = 0; n < length; ++n)
            serializer.SerializeValue(ref elements[n]);
    }
}
