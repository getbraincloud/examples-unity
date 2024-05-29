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
        for(int ii = 0; ii < elements.Length; ii++)
        {
            serializer.SerializeValue(ref elements[ii]);
        }
    }
}
