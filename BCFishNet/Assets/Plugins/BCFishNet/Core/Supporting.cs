using BrainCloud.JsonFx.Json;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Utility.Performance;
using GameKit.Dependencies.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UIElements;

namespace BCFishNet
{
    #region Packets
    /*
    internal struct LocalPacket
    {
        public byte[] Data;
        public int Length;
        public byte Channel;

        public LocalPacket(ArraySegment<byte> data, byte channel)
        {
            // Retrieve a byte array from the pool to minimize allocations.
            Data = ByteArrayPool.Retrieve(data.Count);
            Length = data.Count;
            Buffer.BlockCopy(data.Array, data.Offset, Data, 0, Length);
            Channel = channel;
        }
    }
    */
    internal readonly struct Packet
    {
        public readonly int ConnectionId;
        public readonly int RecipientId;
        public readonly byte[] Data;
        public readonly int Length;
        public readonly byte Channel;
        public readonly bool IsLocal;


        public Packet(int sender, int recipient, ArraySegment<byte> segment, byte channel, int mtu, bool isLocal = false, bool extractChannel = false)
        {
            int dataSize = extractChannel ? segment.Count - 1 : segment.Count; // Reduce size if extracting channel

            if (dataSize < 0) throw new ArgumentException("Segment must have at least one byte to extract.");

            //Debug.Log($"Packet created from sender {sender} to recipient {recipient}");

            // Allocate buffer with at least mtu size
            int arraySize = Math.Max(dataSize, mtu);
            Data = ByteArrayPool.Retrieve(arraySize);
            ConnectionId = sender;
            RecipientId = recipient;
            Channel = channel;
            Length = dataSize;
            IsLocal = isLocal;
            // Copy data, excluding the last byte if extractChannel is true
            Buffer.BlockCopy(segment.Array, segment.Offset, Data, 0, dataSize);

            // Extract the last byte if needed
            if (extractChannel)
            {
                Channel = segment.Array[segment.Offset + segment.Count - 1];
            }
            else
            {
                Channel = channel; // Use provided channel
            }
        }

        public ArraySegment<byte> GetArraySegment()
        {
            //Debug.Log($"[Packet] GetArraySegment ID: {GetPacketId()} - Length: {Length}, Data Size: {Data.Length}");
            return new(Data, 0, Length);
        }

        public string GetReadableSegment()
        {
            ArraySegment<byte> segment = new(Data, 0, Length);
            //string hexString = BitConverter.ToString(segment.Array, segment.Offset, segment.Count);
            string textString = System.Text.Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count);

            return textString;
        }

        public string GetMD5Hash()
        {
            ArraySegment<byte> segment = new(Data, 0, Length);

            using (MD5 md5 = MD5.Create())
            {
                // Compute hash only for the valid portion of the segment
                byte[] hashBytes = md5.ComputeHash(segment.Array, segment.Offset, segment.Count);

                // Convert hash bytes to a hexadecimal string
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
        
        public string GetHexString()
        {
            ArraySegment<byte> segment = new(Data, 0, Length);
            string hexString = BitConverter.ToString(segment.Array, segment.Offset, segment.Count).Replace("-", "");
            return hexString;
        }

        public ushort GetPacketId()
        {
            ArraySegment<byte> segment = new(Data, 0, Length);
            int position = segment.Offset;

            //ushort result = 0;
            //result |= segment[position++];
            //result |= (ushort)(segment[position++] << 8);

            ushort result = BitConverter.ToUInt16(segment.Array, 4);
            /*
            List<ushort> values = ReadShortsFromSegment(segment);
            foreach (ushort value in values)
            {
                Debug.Log("short in segment: " + value);
            }
            */
            return result;
        }

        public List<ushort> ReadShortsFromSegment(ArraySegment<byte> segment)
        {
            List<ushort> values = new List<ushort>();
            int position = segment.Offset;

            while (position + 1 < segment.Offset + segment.Count)
            {
                ushort value = BitConverter.ToUInt16(segment.Array, position);
                values.Add(value);
                position += 2; // Move forward by 2 bytes (sizeof(ushort))
            }

            return values;
        }

        public void Dispose()
        {
            ByteArrayPool.Store(Data);
        }
    }

    public static class PacketUtils
    {
        public static ArraySegment<byte> TrimTrailingZeroByte(ArraySegment<byte> segment)
        {
            if (segment.Count == 0) return segment; // Return if empty

            int newSize = segment.Count;

            // Check if the last byte is 0x00 and trim it
            if (segment.Array[segment.Offset + segment.Count - 1] == 0x00)
            {
                newSize--; // Reduce size by 1
            }

            return new ArraySegment<byte>(segment.Array, segment.Offset, newSize);

        }

        public static byte[] TrimTrailingZeroByte(byte[] data)
        {
            if (data.Length == 0) return data; // Edge case: Empty array

            if (data[^1] == 0x00) // Check last byte (C# 8+ syntax)
            {
                Array.Resize(ref data, data.Length - 1);
            }

            return data;
        }
    }
    #endregion Packets

    #region Lobby Data

    /// <summary>
    /// Lobby data container
    /// </summary>
    public class Lobby
    {
        public string id { get; set; }
        public string lobbyType { get; set; }
        public string state { get; set; }
        public int rating { get; set; }

        [JsonName("desc")] // Ensure proper JSON mapping
        public string desc { get; set; } // Keeping "desc" to match API response

        public Owner owner { get; set; }  // Nested object for lobby owner
        public int numMembers { get; set; } // Matches "numMembers" from response
        public int maxMembers { get; set; } // Matches "maxMembers" from response

        public List<Member> members { get; set; }

        public LobbySettings settings { get; set; }

        // Ensure parameterless constructor
        public Lobby()
        {
            members = new List<Member>(); // Initialize empty list to prevent null reference errors
        }
    }

    public class LobbySettings
    {
        public string lobbyCreator { get; set; }
    }

    /// <summary>
    /// Owner data inside a lobby
    /// </summary>
    public class Owner
    {
        public string profileId { get; set; }
        public string name { get; set; }
        public int rating { get; set; }
        public string pic { get; set; } // Can be null in response, so keep it nullable
        public string cxId { get; set; }

        public Owner() { } // Ensure parameterless constructor
    }

    /// <summary>
    /// Lobby Memeber Data container
    /// </summary>
    public class Member
    {
        public string profileId { get; set; }
        public string name { get; set; }
        public string pic { get; set; }
        public int rating { get; set; }
        public string team { get; set; }
        public bool isReady { get; set; }
        public Dictionary<string, object> extra { get; set; }
        public string cxId { get; set; }

        public Member() { }
    }
    #endregion Lobby Data

    #region LobbyEvent Data
    /// <summary>
    /// LobbyEvent Class
    /// </summary>
    [Serializable]
    public class LobbyEvent
    {
        public string operation;
        public LobbyEventData data;

        public LobbyEvent() { }
    }

    /// <summary>
    /// LobbyEventData
    /// </summary>
    [Serializable]
    public class LobbyEventData
    {
        public string lobbyId;

        public int curStep;
        public int ofStep;
        public string msg;

        public string region;
        public ConnectData connectData;
        public string passcode;

        // Parameterless constructor
        public LobbyEventData() { }
    }

    [Serializable]
    public class ConnectData
    {
        public string address;
        public Ports ports;

        // Parameterless constructor
        public ConnectData() { }
    }

    [Serializable]
    public class Ports
    {
        public int udp;
        public int tcp;
        public int ws;

        // Parameterless constructor
        public Ports() { }
    }

    /// <summary>
    /// RelayConnectionInfo
    /// </summary>
    [Serializable]
    public class RelayConnectionInfo
    {
        public bool ssl;
        public string host;
        public int port;

        // Parameterless constructor

        public RelayConnectionInfo() { }
    }
    #endregion LobbyEvent Data

    #region RelaySystemEvents.

    [Serializable]
    public class RelaySystemEvent
    {
        public string op;
    }

    [Serializable]
    public class RelaySystemConnect : RelaySystemEvent
    {
        public string ownerCxId;
        public string cxId;
        public string netId;
        public string version;
    }

    [Serializable]
    public class RelaySystemDisconnect : RelaySystemEvent
    {
        public string cxId;
        public int netId;
    }

    [Serializable]
    public class RelaySystemMigrateOwner : RelaySystemEvent
    {
        public string cxId;
    }

    [Serializable]
    public class RelaySystemNetId : RelaySystemEvent
    {
        public string cxId;
        public int netId;
    }
    #endregion RelaySystemEvents.
}
