using FishNet.Managing;
using FishNet.Transporting;
using BrainCloud;
using BrainCloud.JsonFx.Json;
using FishNet;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Connection;
using FishNet.Utility.Performance;
using System;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BCFishNet
{
    public class BCFishNetTransport : Transport
    {
        #region Serialized Fields.
        [SerializeField] private string _serverBindAddress = "127.0.0.1";
        [SerializeField] private ushort _port = 7770;
        [SerializeField] private ushort _maximumClients = 10;
        [SerializeField] private string _clientAddress = string.Empty;
        [SerializeField] private string _roomAddress = string.Empty;
        [SerializeField] private string _roomPort = string.Empty;
        #endregion

        #region Private Fields.
        private BrainCloudWrapper _brainCloud = null;
        private bool _isServer = false;
        private Transport _transport;

        private int hostId = INVALID_HOST_ID;
        private int localClientId = 0;
        private string hostCxId = string.Empty;

        private bool _shutdownCalled = false;
        private int[] _mtus;
        private const int MTU = 1012;

        private const int INVALID_HOST_ID = 40;

        private List<int> _connectedClients = new List<int>();

        private ConcurrentQueue<Packet> _incomingPackets = new();
        private ConcurrentQueue<Packet> _incomingLocalPackets = new();
        private ConcurrentQueue<Packet> _incomingServerPackets = new();
        private Queue<Packet> _outgoingPackets = new();

        #endregion

        #region Events.
        public override event Action<ClientConnectionStateArgs> OnClientConnectionState;
        public override event Action<ServerConnectionStateArgs> OnServerConnectionState;
        public override event Action<RemoteConnectionStateArgs> OnRemoteConnectionState;
        public override event Action<ClientReceivedDataArgs> OnClientReceivedData;
        public override event Action<ServerReceivedDataArgs> OnServerReceivedData;
        #endregion

        #region Initialization.
        public void Config(BrainCloudWrapper bcWrapper, string address, string relayPasscode, string currentLobbyId, ushort port)
        {
            _brainCloud = bcWrapper;
            SetClientAddress(address);
            SetRelayPasscode(relayPasscode);
            SetCurrentLobbyId(currentLobbyId);
            SetPort(port);

            _brainCloud.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent);
        }

        public override void Initialize(NetworkManager networkManager, int transportIndex)
        {
            base.Initialize(networkManager, transportIndex);
            _transport = this;
            Debug.Log("FBCT: Initialize");

            _connectedClients.Clear();

            CreateChannelData();
        }


        private void OnDestroy() => Shutdown();
        #endregion

        #region Setup Functions.
        private void CreateChannelData() => _mtus = new int[4] { 1020, 1020, 1020, 1020 };

        public bool IsNetworkAccessAvailable() => _brainCloud != null && _brainCloud.Client.IsAuthenticated();
        #endregion

        #region Configuration.
        public override int GetMaximumClients() => _maximumClients;
        public override void SetMaximumClients(int value) => _maximumClients = (ushort)Mathf.Clamp(value, 1, ushort.MaxValue);
        public override void SetClientAddress(string address)
        {
            _clientAddress = address;
        }
        private string _relayPasscode = string.Empty;
        private string _currentLobbyId = string.Empty;
        public void SetRelayPasscode(string relayPasscode) => _relayPasscode = relayPasscode;
        public void SetCurrentLobbyId(string currentLobbyId) => _currentLobbyId = currentLobbyId;
        public override void SetServerBindAddress(string address, IPAddressType addressType) => _serverBindAddress = address;
        public override void SetPort(ushort port) => _port = port;


        #endregion

        #region Connection Management.
        public override string GetConnectionAddress(int connectionId)
        {
            return _clientAddress;
        }

        public override LocalConnectionState GetConnectionState(bool server)
        {
            if (_brainCloud != null && _brainCloud.RelayService.IsConnected())
            {
                return LocalConnectionState.Started;
            }
            else
            {
                return LocalConnectionState.Stopped;
            }
        }

        public override RemoteConnectionState GetConnectionState(int connectionId)
        {
            if (_connectedClients.Contains(connectionId))
            {
                return RemoteConnectionState.Started;
            }
            else
            {
                return RemoteConnectionState.Stopped;
            }
        }

        public override void HandleClientConnectionState(ClientConnectionStateArgs args)
        {
            OnClientConnectionState?.Invoke(args);
        }

        public override void HandleServerConnectionState(ServerConnectionStateArgs args)
        {
            Debug.Log($"[BCFishNet] HandleServerConnectionState - connection state changed: State: {args.ConnectionState}");
            OnServerConnectionState?.Invoke(args);
        }

        public override void HandleRemoteConnectionState(RemoteConnectionStateArgs args)
        {
            Debug.Log($"[BCFishNet] HandleRemoteConnectionState - Remote connection state changed for ConnectionId: {args.ConnectionId}, State: {args.ConnectionState}");
            OnRemoteConnectionState?.Invoke(args);
        }

        #endregion

        #region Iteration.
        public override void IterateIncoming(bool server)
        {
            if (_brainCloud == null || !_brainCloud.RelayService.IsConnected() || GetConnectionState(server) != LocalConnectionState.Started)
                return;

            //Not yet started, cannot continue.
            LocalConnectionState localState = GetConnectionState(server);
            if (localState != LocalConnectionState.Started)
            {
                ResetQueues();
                //ResetQueues();
                //If stopped try to kill task.
                if (localState == LocalConnectionState.Stopped)
                {
                    return;
                }
            }

            if (server)
            {
                //parse packets sent from host to server first
                while (_incomingServerPackets.TryDequeue(out Packet incoming))
                {
                    ArraySegment<byte> segment = incoming.GetArraySegment();
                    //Debug.Log($"[BCFishNet] IterateIncoming SERVER packetId: {incoming.GetPacketId()} Length: {incoming.Length} HEX: {incoming.GetHexString()}");

                    ServerReceivedDataArgs dataArgs = new(
                    incoming.GetArraySegment(),
                    (Channel)incoming.Channel,
                    incoming.ConnectionId,
                    Index);

                    HandleServerReceivedDataArgs(dataArgs);

                    incoming.Dispose();
                }

                //parse local host packets after
                while (_incomingLocalPackets.TryDequeue(out Packet incoming))
                {
                    ArraySegment<byte> segment = incoming.GetArraySegment();
                    //Debug.Log($"[BCFishNet] IterateIncoming LOCAL packetId: {incoming.GetPacketId()} Length: {incoming.Length} HEX: {incoming.GetHexString()}");

                    ClientReceivedDataArgs dataArgs = new(
                    incoming.GetArraySegment(),
                    (Channel)incoming.Channel, Index);

                    HandleClientReceivedDataArgs(dataArgs);

                    incoming.Dispose();
                }
            }
            else
            {
                //client packets
                while (_incomingPackets.TryDequeue(out Packet incoming))
                {
                    ArraySegment<byte> segment = incoming.GetArraySegment();
                    //Debug.Log($"[BCFishNet] IterateIncoming packetId: {incoming.GetPacketId()} Length: {incoming.Length} HEX: {incoming.GetHexString()}");

                    ClientReceivedDataArgs dataArgs = new(
                    incoming.GetArraySegment(),
                    (Channel)incoming.Channel, Index);

                    //Debug.Log($"[BCFishNet] IterateIncoming Client packetId: {incoming.GetPacketId()} Length: {incoming.Length} HEX: {incoming.GetHexString()}");
                    //Debug.Log($"[BCFishNet] IterateIncoming Client Segment Count: {segment.Count}, Channel: {incoming.Channel}, Index: {Index}");
                    HandleClientReceivedDataArgs(dataArgs);

                    incoming.Dispose();
                }
            }
        }

        public override void IterateOutgoing(bool server)
        {
            //if server send to clients, if not send to server
            if (GetConnectionState(server) != LocalConnectionState.Started)
            {
                //Not started, clear outgoing.
                ClearPacketQueue(ref _outgoingPackets);
            }
            else
            {
                // send them once we have enough data
                if (hostId == INVALID_HOST_ID)
                    return;

                int count = _outgoingPackets.Count;
                for (int i = 0; i < count; i++)
                {
                    Packet outgoing = _outgoingPackets.Dequeue();
                    int recipientId = outgoing.RecipientId;

                    // update all those to the host id if they were meant for the host
                    if (recipientId == INVALID_HOST_ID)
                        recipientId = hostId;

                    ArraySegment<byte> segment = outgoing.GetArraySegment();

                    //Send to all clients.
                    if (recipientId == NetworkConnection.UNSET_CLIENTID_VALUE)// -1 to sendToAll
                    {
                        SendToAll(segment, outgoing.Channel);
                    }
                    //Send to one client.
                    else
                    {
                        Send(recipientId, segment, outgoing.Channel);
                    }
                }
            }

        }

        private void ResetQueues()
        {
            ClearPacketQueue(ref _incomingPackets);
            ClearPacketQueue(ref _incomingLocalPackets);
            ClearPacketQueue(ref _incomingServerPackets);
            ClearPacketQueue(ref _outgoingPackets);
        }

        internal void ClearPacketQueue(ref Queue<Packet> queue)
        {
            int count = queue.Count;
            for (int i = 0; i < count; i++)
            {
                Packet p = queue.Dequeue();
                p.Dispose();
            }
        }
        internal void ClearPacketQueue(ref ConcurrentQueue<Packet> queue)
        {
            while (queue.TryDequeue(out Packet p))
                p.Dispose();
        }
        #endregion

        #region Data Handling.

        private void OnLobbyEvent(string json)
        {
            if (_isServer)
            {
                Debug.Log("OnLobbyEvent : " + json);

                Dictionary<string, object> response = JsonReader.Deserialize<Dictionary<string, object>>(json);

                if (response.ContainsKey("data") && response["data"] is Dictionary<string, object> jsonData)
                {
                    if (response.ContainsKey("operation"))
                    {
                        var operation = response["operation"] as string;

                        switch (operation)
                        {
                            case "MEMBER_LEFT":
                                {
                                    if (jsonData.ContainsKey("member") && jsonData["member"] is Dictionary<string, object> memberData)
                                    {
                                        if (memberData.ContainsKey("cxId") && memberData["cxId"] is string cxid)
                                        {
                                            Debug.Log("MEMBER_LEFT: " + cxid);

                                            int disconnectedNetId = _brainCloud.RelayService.GetNetIdForCxId(cxid);

                                            Debug.Log("STOPPING: " + cxid);
                                            StopClient(disconnectedNetId, true);
                                        }
                                    }
                                }
                                break;

                            case "SIGNAL":
                                {
                                    if (jsonData.ContainsKey("signalData") && jsonData["signalData"] is Dictionary<string, object> signalData)
                                    {
                                        if (signalData.TryGetValue(REMOTE_CLIENT_ID, out object remoteClientIdObj) && remoteClientIdObj is int remoteClientId)
                                        {
                                            Debug.Log("remoteClientId: " + remoteClientId);

                                            // Example usage
                                            HandleRemoteConnectionState(new RemoteConnectionStateArgs(RemoteConnectionState.Started,
                                                remoteClientId, Index));
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
            }

        }
        public override void HandleClientReceivedDataArgs(ClientReceivedDataArgs args)
        {
            if (args.Data.Count == 0)
                return;

            OnClientReceivedData?.Invoke(args);
        }

        public override void HandleServerReceivedDataArgs(ServerReceivedDataArgs args)
        {
            //Debug.Log($"[BCFishNet] HandleServerReceivedDataArgs From Connection: {args.ConnectionId}");

            OnServerReceivedData?.Invoke(args);
        }
        #endregion

        #region Sending.
        public override void SendToServer(byte channelId, ArraySegment<byte> segment)
        {
            if (_brainCloud == null || GetConnectionState(_isServer) != LocalConnectionState.Started || !_brainCloud.Client.IsAuthenticated())
                return;
            
            Packet packet = new Packet(localClientId, hostId, segment, channelId, MTU);
            //Debug.Log($"[BCFishNet] Sending packetId {packet.GetPacketId()} HEX: {packet.GetHexString()} to Server ");
            if (_isServer)
            {
                //we are the client host sending this packet to ourselves, so just process it instead of actually sending over the network
                _incomingServerPackets.Enqueue(packet);
            }
            else
            {
                _outgoingPackets.Enqueue(packet);
            }
        }

        public override void SendToClient(byte channelId, ArraySegment<byte> segment, int connectionId)
        {
            if (_isServer && GetConnectionState(true) == LocalConnectionState.Started)
            {
                //we only send packets to clients if we are the host
                Packet packet = new Packet(localClientId, connectionId, segment, channelId, MTU);
                //Debug.Log($"[BCFishNet] Sending packetId {packet.GetPacketId()} HEX: {packet.GetHexString()} to Client {packet.RecipientId}");
                if (connectionId == hostId)
                {
                    //we are trying to send to the client host as the server so just process it
                    _incomingLocalPackets.Enqueue(packet);
                }
                else
                {
                    //sending to other connected client
                    _outgoingPackets.Enqueue(packet);
                }
            }
        }

        const bool ORDERED_BIT = false;
        void Send(int recipientId, ArraySegment<byte> segment, byte channelId)
        {
            int newSize = Math.Min(segment.Count + 1, MTU);
            byte[] data = segment.Array.Skip(segment.Offset).Take(newSize).ToArray();

            data[newSize - 1] = channelId;
            bool isReliable = channelId == 0 ? false : true;

            _brainCloud.RelayService.Send(
                in_data: data,
                to_netId: (ulong)recipientId,
                in_reliable: isReliable,
                in_ordered: ORDERED_BIT,
                in_channel: 0
            );
        }

        public void SendToAll(ArraySegment<byte> segment, byte channelId)
        {
            int newSize = Math.Min(segment.Count + 1, MTU);
            byte[] data = segment.Array.Skip(segment.Offset).Take(newSize).ToArray();

            data[newSize - 1] = channelId;
            bool isReliable = channelId == 0 ? false : true;

            // Broadcast the message to all clients
            _brainCloud.RelayService.SendToAll(
                data,               // The message data as a byte array
                isReliable,         // Reliable transmission
                ORDERED_BIT,        // Ordered delivery
                0                   // Channel (0-3); use 0 for high priority
            );
        }

        #endregion

        #region Start and Stop.
        public override bool StartConnection(bool server)
        {
            Debug.Log("[BCFishNet] StartConnection IsServer: " + server);
            _isServer = server;

            if (_brainCloud != null && !_brainCloud.Client.IsAuthenticated())
            {
                Debug.Log("[BCFishNet] Waiting for authentication...");
                return false;
            }

            ResetQueues();

            _brainCloud.RelayService.RegisterRelayCallback(OnRelayCallback);
            _brainCloud.RelayService.RegisterSystemCallback(OnRelaySystemCallback);


            RelayConnectOptions connectionOptions = new RelayConnectOptions(true,
                _clientAddress, // should this be room address?
                _port,
                _relayPasscode,
                _currentLobbyId
            );

            SuccessCallback successCallback = (responseData, cbObject) =>
            {
                bool isRelayConnected = _brainCloud.RelayService.IsConnected();
                if (_isServer)
                {
                    HandleServerConnectionState(new ServerConnectionStateArgs(LocalConnectionState.Started, Index));
                }

                // connect as a client
                HandleClientConnectionState(new ClientConnectionStateArgs(LocalConnectionState.Started, Index));
            };

            FailureCallback failureCallback = (statusMessage, code, error, cbObject) =>
            {
                Debug.LogWarning($"RelayService Connection Failure - statusMessage: {statusMessage} code: {code} error: {error}");

                // 
                // there was a failure lets go back to the main screen and closing things down
                Debug.LogWarning($"Going Back to the main menu to allow rejoining - Display connection error message");
                SceneManager.LoadScene("Main");
            };

            _brainCloud.RelayService.Connect(RelayConnectionType.UDP, connectionOptions, successCallback, failureCallback);

            return true;
        }

        private void OnRelayCallback(short netId, byte[] data)
        {
            if (data == null || data.Length == 0) return;

            string hexString = BitConverter.ToString(data, 0, data.Length).Replace("-", "");
            //Debug.Log($"[BCFishNet] RECEIVED PACKET HEX: {hexString}");

            ArraySegment<byte> segment = new ArraySegment<byte>(data);

            Packet packet = new Packet(netId, localClientId, segment, 0, MTU, false, true);

            if (_isServer && netId != hostId)
            {
                //if we are the server and we received remote packets from remote clients, process the packets on the server side
                _incomingServerPackets.Enqueue(packet);
            }
            else
            {
                _incomingPackets.Enqueue(packet);
            }

        }

        private void OnRelaySystemCallback(string json)
        {
            Debug.Log($"[BCFishNet] OnSystemCallback: {json}");

            // Parse the base event to check the operation type
            var systemEvent = JsonUtility.FromJson<RelaySystemEvent>(json);
            bool isHost, isLocal;
            string localProfileId = _brainCloud.Client.ProfileId;
            string ServerOrClient = _isServer ? "[Host]" : "[Client]";

            switch (systemEvent.op)
            {
                case "CONNECT":
                    {
                        var connectEvent = JsonUtility.FromJson<RelaySystemConnect>(json);
                        //Debug.Log($"OnSystemCallback connectEvent: {connectEvent.op} ownerCxId: {connectEvent.ownerCxId} cxId: {connectEvent.cxId} netId:{connectEvent.netId} version: {connectEvent.version}");

                        int connectedNetId = int.Parse(connectEvent.netId);
                        string connectedProfileId = _brainCloud.RelayService.GetProfileIdForNetId((short)connectedNetId);
                        hostCxId = connectEvent.ownerCxId;

                        isHost = hostCxId == connectEvent.cxId;
                        if (isHost)
                        {
                            hostId = _brainCloud.RelayService.GetNetIdForCxId(hostCxId);
                            Debug.Log($"OnSystemCallback hostCxId: {hostCxId} hostId: {hostId} ");
                        }

                        isLocal = connectedProfileId == localProfileId;

                        if (isLocal) localClientId = connectedNetId;

                        AddConnectionHelper(connectedNetId, isLocal);
                    }
                    break;
                case "DISCONNECT":
                    {
                        var disconnectEvent = JsonUtility.FromJson<RelaySystemDisconnect>(json);

                        string disconnectedProfileId = disconnectEvent.cxId.Split(':')[1];

                        isHost = hostCxId == disconnectEvent.cxId;

                        isLocal = localProfileId == disconnectedProfileId;

                        int disconnectedNetId = _brainCloud.RelayService.GetNetIdForCxId(disconnectEvent.cxId);

                        StopClient(disconnectedNetId, true);
                    }
                    break;

                case "MIGRATE_OWNER":
                    {
                        //receiving this as the server socket means the client host has disconnected and the host status has been given to the next connected client
                        var migrateEvent = JsonUtility.FromJson<RelaySystemMigrateOwner>(json);
                        Debug.Log($"[BCFishNet] Received request to migrate owner to {migrateEvent.cxId}");

                        int previousHostId = hostId;
                        string newHostProfileId = migrateEvent.cxId.Split(':')[1];

                        isLocal = localProfileId == newHostProfileId;
                        hostId = _brainCloud.RelayService.GetNetIdForCxId(migrateEvent.cxId);
                        hostCxId = migrateEvent.cxId;
                        Debug.Log($"[BCFishNet] New Host netId: {hostId}");

                        _isServer = isLocal;

                        // Start a coroutine to handle the shutdown + promotion
                        StartCoroutine(HandleMigrateOwnerCoroutine(_isServer, hostId));
                    }
                    break;
                case "NET_ID":
                    {
                        //Debug.Log($"ServerSocket - OnSystemCallback NET_ID: {json}");
                        var netIdEvent = JsonUtility.FromJson<RelaySystemNetId>(json);
                        hostId = _brainCloud.RelayService.GetNetIdForCxId(hostCxId);

                        string connectedProfileId = _brainCloud.RelayService.GetProfileIdForNetId((short)netIdEvent.netId);

                        isLocal = connectedProfileId == localProfileId;
                        Debug.Log($"[BCFishNet] NET_ID: {netIdEvent.netId} connectedProfileId: {connectedProfileId} isLocal: {isLocal}");

                        AddConnectionHelper(netIdEvent.netId, isLocal);
                    }
                    break;
                default:
                    {
                        Debug.LogError("[BCFishNet]  - OnSystemCallback Unknown system event: " + systemEvent.op);
                    }
                    break;
            }
        }
        private IEnumerator HandleMigrateOwnerCoroutine(bool isNowServer, int newHostId)
        {
            // stop previous connect
            HandleClientConnectionState(new ClientConnectionStateArgs(LocalConnectionState.Stopped, Index));

            yield return null;

            if (isNowServer)
            {
                Debug.Log("[BCFishNet] This client is now becoming the server host");
                // start server with the new host
                HandleServerConnectionState(new ServerConnectionStateArgs(LocalConnectionState.Started, Index));
                // tell the client and this remote connected
                HandleClientConnectionState(new ClientConnectionStateArgs(LocalConnectionState.Started, Index));

                HandleRemoteConnectionState(new RemoteConnectionStateArgs(RemoteConnectionState.Started, newHostId, Index));
            }
            else
            {
                Debug.Log("[BCFishNet] This client is reconnecting to the new host " + newHostId + " as " + localClientId);
                HandleClientConnectionState(new ClientConnectionStateArgs(LocalConnectionState.Started, Index));

                Dictionary<string, object> signalData = new Dictionary<string, object>();
                signalData[REMOTE_CLIENT_ID] = localClientId;
                _brainCloud.LobbyService.SendSignal(_currentLobbyId, signalData);
            }
            ResyncPlayerListItems();
        }

        private void ResyncPlayerListItems()
        {
            PlayerListEvents.RaiseResyncPlayerList();
        }

        private const string REMOTE_CLIENT_ID = "remoteClientId";
        private void AddConnectionHelper(int connectedNetId, bool isLocal)
        {
            Debug.Log($"[BCFishNet] AddConnectionHelper netId: {connectedNetId}, isLocal: {isLocal}");
            if (AddConnection(connectedNetId))
            {
                if (_isServer)
                {
                    Debug.Log($"[BCFishNet] CONNECTING Remote {connectedNetId}");
                    HandleRemoteConnectionState(new RemoteConnectionStateArgs(RemoteConnectionState.Started, connectedNetId, Index));
                }
            }
        }

        public override bool StopConnection(bool server)
        {
            if (server)
            {
                return StopServer();
            }
            else
            {
                return StopClient();
            }
        }

        public override bool StopConnection(int connectionId, bool immediately)
        {
            return StopClient(connectionId, immediately);
        }

        /// <summary>
        /// Stops a remote client on the server.
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="immediately">True to abrutly stp the client socket without waiting socket thread.</param>
        private bool StopClient(int connectionId, bool immediately)
        {
            if (RemoveConnection(connectionId))
            {
                string connectedProfileId = _brainCloud.RelayService.GetProfileIdForNetId((short)connectionId);

                if (connectedProfileId == _brainCloud.Client.ProfileId)
                {
                    Debug.Log($"[BCFishNet] StopClient local {connectionId}");
                    _brainCloud.RelayService.DeregisterRelayCallback();
                    _brainCloud.RelayService.Disconnect();
                    _brainCloud.LobbyService.LeaveLobby(_currentLobbyId);

                    _brainCloud.RTTService.DeregisterRTTLobbyCallback();

                    HandleClientConnectionState(new ClientConnectionStateArgs(LocalConnectionState.Stopped, Index));
                    PlayerListEvents.RaiseClearAllPlayerList();

                    _connectedClients.Clear();
                }

                if (_isServer)
                {
                    Debug.Log($"[BCFishNet] StopClient server {connectionId}");
                    HandleRemoteConnectionState(new RemoteConnectionStateArgs(RemoteConnectionState.Stopped, connectionId, Index));
                }
            }

            return true;
        }

        private bool StopServer()
        {
            Debug.Log($"[BCFishNet] StopServer called");

            _brainCloud.RelayService.DeregisterRelayCallback();
            _brainCloud.RelayService.Disconnect();
            _connectedClients.Clear();
            HandleServerConnectionState(new ServerConnectionStateArgs(LocalConnectionState.Stopped, Index));
            return true;
        }

        /// <summary>
        /// Stops the client.
        /// </summary>
        private bool StopClient()
        {
            return StopClient(localClientId, true);
        }

        private bool AddConnection(int netId)
        {
            Debug.Log($"[BCFishNet] AddConnection {netId}");
            if (netId == INVALID_HOST_ID)
            {
                Debug.LogWarning("[BCFishNet] Attempted to add an invalid NET ID.");
                return false;
            }
            bool connectionAdded = false;
            if (!_connectedClients.Contains(netId))
            {
                _connectedClients.Add(netId);
                connectionAdded = true;
            }
            return connectionAdded;
        }

        private bool RemoveConnection(int netId)
        {
            bool connectionRemoved = false;
            if (_connectedClients.Contains(netId))
            {
                _connectedClients.Remove(netId);
                connectionRemoved = true;
            }
            Debug.Log($"[BCFishNet] RemoveConnection {netId}, {connectionRemoved}");

            if (NetworkManager.ServerManager.Clients.TryGetValue(netId, out NetworkConnection conn))
            {
                Debug.Log($"Kicking client {netId}");
                conn.Disconnect(true); // Disconnect gracefully
            }
            else
            {
                Debug.LogWarning($"Client {netId} not found.");
            }

            return connectionRemoved;
        }

        public override void Shutdown()
        {
            Debug.Log($"[BCFishNet] Shutdown called");
            if (_shutdownCalled) return;
            _shutdownCalled = true;
            StopConnection(false);

            // no one else left, shut it down
            if (_connectedClients.Count == 0)
            {
                StopConnection(true);
            }
        }
        #endregion

        public override int GetMTU(byte channel) => _mtus[channel];

        private void OnApplicationQuit()
        {
            if (_brainCloud.RelayService.IsConnected())
            {
                _brainCloud.RelayService.Disconnect();
            }

            StopClient();
        }
    }
}
