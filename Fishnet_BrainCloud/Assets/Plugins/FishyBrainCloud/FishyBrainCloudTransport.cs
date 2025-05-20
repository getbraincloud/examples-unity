using FishNet.Managing;
using FishNet.Transporting;
using BrainCloud;
using System;
using UnityEngine;
using FishNet.Serializing;
using FishNet.Connection;
using FishNet.Utility.Performance;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using UnityEditor.MemoryProfiler;
using System.Text;

namespace FishyBrainCloud
{
    public class FishyBrainCloudTransport : Transport
    {
        #region Serialized Fields.
        [SerializeField] private string _serverBindAddress = "127.0.0.1";
        [SerializeField] private ushort _port = 7770;
        [SerializeField] private ushort _maximumClients = 10;
        [SerializeField] private bool _peerToPeer = false;
        [SerializeField] private string _clientAddress = string.Empty;
        [SerializeField] private string _roomAddress = string.Empty;
        [SerializeField] private string _roomPort = string.Empty;
        #endregion

        #region Private Fields.
        private BrainCloudWrapper _brainCloud = null;
        private bool _isServer = false;
        private Transport _transport;
        
        private int hostId = 0;
        private int localClientId = 0;
        private string hostCxId = string.Empty;

        private bool _shutdownCalled = false;
        private int[] _mtus;
        private const int MTU = 1012;

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

        /// <summary>
        /// Id to use for client when acting as host.
        /// </summary>
        internal const int CLIENT_HOST_ID = 0;

        #region Initialization.
        public void Config(BrainCloudWrapper bcWrapper, string address, string relayPasscode, string currentLobbyId, ushort port)
        {
            _brainCloud = bcWrapper;
            SetClientAddress(address);
            SetRelayPasscode(relayPasscode);
            SetCurrentLobbyId(currentLobbyId);
            SetPort(port);
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

        public override void HandleServerConnectionState(ServerConnectionStateArgs args) {
            Debug.Log($"[FishyBrainCloud] HandleServerConnectionState - connection state changed: State: {args.ConnectionState}");
            OnServerConnectionState?.Invoke(args); 
        }

        public override void HandleRemoteConnectionState(RemoteConnectionStateArgs args)
        {
            Debug.Log($"[FishyBrainCloud] HandleRemoteConnectionState - Remote connection state changed for ConnectionId: {args.ConnectionId}, State: {args.ConnectionState}");
            OnRemoteConnectionState?.Invoke(args);
        }

        #endregion

        #region Iteration.
        public override void IterateIncoming(bool server)
        {
            if (GetConnectionState(server) != LocalConnectionState.Started)
                return;

            //Not yet started, cannot continue.
            LocalConnectionState localState = GetConnectionState(server);
            if (localState != LocalConnectionState.Started)
            {
                ResetQueues();
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
                    //Debug.Log($"[FishyBrainCloud] IterateIncoming SERVER packetId: {incoming.GetPacketId()} Length: {incoming.Length} HEX: {incoming.GetHexString()}");

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
                    //Debug.Log($"[FishyBrainCloud] IterateIncoming LOCAL packetId: {incoming.GetPacketId()} Length: {incoming.Length} HEX: {incoming.GetHexString()}");

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
                    //Debug.Log($"[FishyBrainCloud] IterateIncoming packetId: {incoming.GetPacketId()} Length: {incoming.Length} HEX: {incoming.GetHexString()}");

                    ClientReceivedDataArgs dataArgs = new(
                    incoming.GetArraySegment(),
                    (Channel)incoming.Channel, Index);

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
                int count = _outgoingPackets.Count;
                for (int i = 0; i < count; i++)
                {
                    Packet outgoing = _outgoingPackets.Dequeue();
                    int recipientId = outgoing.RecipientId;
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
        public override void HandleClientReceivedDataArgs(ClientReceivedDataArgs args)
        {
            if (args.Data.Count == 0)
                return;

            OnClientReceivedData?.Invoke(args);
        }

        public override void HandleServerReceivedDataArgs(ServerReceivedDataArgs args)
        {
            //Debug.Log($"[FishyBrainCloud] HandleServerReceivedDataArgs From Connection: {args.ConnectionId}");

            OnServerReceivedData?.Invoke(args);
        }
        #endregion

        #region Sending.
        public override void SendToServer(byte channelId, ArraySegment<byte> segment)
        {
            if (_brainCloud == null || GetConnectionState(_isServer) != LocalConnectionState.Started)
                return;
            if (!_brainCloud.Client.IsAuthenticated())
                return;

            Packet packet = new Packet(localClientId, hostId, segment, channelId, MTU);
            //Debug.Log($"[FishyBrainCloud] Sending packetId {packet.GetPacketId()} HEX: {packet.GetHexString()} to Server ");
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
                //Debug.Log($"[FishyBrainCloud] Sending packetId {packet.GetPacketId()} HEX: {packet.GetHexString()} to Client {packet.RecipientId}");
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
                in_ordered: true,
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
                data,         // The message data as a byte array
                isReliable,                // Reliable transmission
                true,                // Ordered delivery
                0                   // Channel (0-3); use 0 for high priority
            );
        }

        #endregion

        #region Start and Stop.
        public override bool StartConnection(bool server)
        {
            Debug.Log("[FishyBrainCloud] StartConnection IsServer: " + server);
            _isServer = server;

            if (_brainCloud != null && !_brainCloud.Client.IsAuthenticated())
            {
                Debug.Log("[FishyBrainCloud] Waiting for authentication...");
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
            };

            FailureCallback failureCallback = (statusMessage, code, error, cbObject) =>
            {
                Debug.LogWarning($"RelayService Connection Failure - statusMessage: {statusMessage} code: {code} error: {error}");
            };

            _brainCloud.RelayService.Connect(RelayConnectionType.UDP, connectionOptions, successCallback, failureCallback);

            return true;
        }

        private void OnRelayCallback(short netId, byte[] data)
        {
            if (data == null || data.Length == 0) return;

            string hexString = BitConverter.ToString(data, 0, data.Length).Replace("-", "");
            //Debug.Log($"[FishyBrainCloud] RECEIVED PACKET HEX: {hexString}");

            ArraySegment<byte> segment = new ArraySegment<byte>(data);

            Packet packet = new Packet(netId, localClientId, segment, 0, MTU, false, true);

            if(_isServer && netId != hostId)
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
            Debug.Log($"[FishyBrainCloud] OnSystemCallback: {json}");

            // Parse the base event to check the operation type
            var systemEvent = JsonUtility.FromJson<RelaySystemEvent>(json);
            bool isHost, isLocal;
            string localProfileId = _brainCloud.Client.ProfileId;
            string ServerOrClient = _isServer ? "[Host]" : "[Client]";

            switch (systemEvent.op)
            {
                case "CONNECT":
                    var connectEvent = JsonUtility.FromJson<RelaySystemConnect>(json);
                    Debug.Log($"OnSystemCallback connectEvent: {connectEvent.op} ownerCxId: {connectEvent.ownerCxId} cxId: {connectEvent.cxId} netId:{connectEvent.netId} version: {connectEvent.version}");
                    //localClientId = connectEvent.cxId;
                    
                    string connectedProfileId = _brainCloud.RelayService.GetProfileIdForNetId((short)int.Parse(connectEvent.netId));
                    hostCxId = connectEvent.ownerCxId;

                    hostId = _brainCloud.RelayService.GetNetIdForCxId(hostCxId);
                    int connectedNetId = int.Parse(connectEvent.netId);
                    Debug.Log($"HOST ID is {hostId}");
                    isHost = hostCxId == connectEvent.cxId;
                    isLocal = connectedProfileId == localProfileId;

                    string userStatus = isHost ? "[Host]" : "[Client]";
                    string localStatus = isLocal ? "[Local]" : "[Remote]";

                    Debug.Log($"[FishyBrainCloud] {localStatus} Connected as {userStatus}");

                    if (isLocal)
                    {
                        localClientId = connectedNetId;
                        
                        if (isHost)
                        {
                            HandleClientConnectionState(new ClientConnectionStateArgs(LocalConnectionState.Started, Index));
                        }
                    }

                    if (_isServer)
                    {
                        Debug.Log($"[FishyBrainCloud][Host] Processing new {localStatus} connection");
                        AddConnection(connectedNetId);
                        HandleRemoteConnectionState(new RemoteConnectionStateArgs(RemoteConnectionState.Started, connectedNetId, Index));
                    }
                    else
                    {
                        //this is an invalid host ID, wait for host to be connected to start sending messages
                        if (hostId != 40)
                        {
                            Debug.Log($"[FishyBrainCloud][Client] Establishing Fishnet Connection for {localStatus} client");
                            HandleClientConnectionState(new ClientConnectionStateArgs(LocalConnectionState.Started, Index));
                        }
                    }
                    
                    break;
                case "DISCONNECT":
                    var disconnectEvent = JsonUtility.FromJson<RelaySystemDisconnect>(json);

                    string disconnectedProfileId = disconnectEvent.cxId.Split(':')[1];

                    isHost = hostCxId == disconnectEvent.cxId;
                    
                    isLocal = localProfileId == disconnectedProfileId;

                    int disconnectedNetId = _brainCloud.RelayService.GetNetIdForCxId(disconnectEvent.cxId);
                    
                    localStatus = isLocal ? "[Local]" : "[Remote]";
                    userStatus = isHost ? "[Host]" : "[Client]";

                    Debug.Log($"[FishyBrainCloud]{ServerOrClient} Received disconnect from {localStatus} - {userStatus} NetId: {disconnectedNetId}");

                    
                    if (_isServer)
                    {
                        RemoveConnection(disconnectedNetId);
                        HandleRemoteConnectionState(new RemoteConnectionStateArgs(RemoteConnectionState.Stopped, disconnectedNetId, Index));
                    }
                        
                    /*
                    if (isHost)
                    {
                        //If local server is disconnecting, don't stop connection unless we are the last disconnecting client
                        //This allows brainCloud relay servers to migrate the host status to the next connected client
                        Debug.Log($"[FishyBrainCloud] {_connectedClients.Count} connected clients remaining");
                        if (_connectedClients.Count == 1)
                        {
                            //We are the last connected client, shut down server as we are disconnecting
                            HandleRemoteConnectionState(new RemoteConnectionStateArgs(RemoteConnectionState.Stopped, disconnectEvent.netId, Index));
                            HandleServerConnectionState(new ServerConnectionStateArgs(LocalConnectionState.Stopped, Index));
                        }
                    }
                    */

                    break;
                case "MIGRATE_OWNER":

                    var migrateEvent = JsonUtility.FromJson<RelaySystemMigrateOwner>(json);
                    Debug.Log($"[FishyBrainCloud] Received request to migrate owner to {migrateEvent.cxId}");
                    string newHostProfileId = migrateEvent.cxId.Split(':')[1];
                    isLocal = localProfileId == newHostProfileId;

                    int newHostNetID = _brainCloud.RelayService.GetNetIdForCxId(migrateEvent.cxId);
                    hostId = newHostNetID;

                    Debug.Log($"[FishyBrainCloud] New netId: {hostId}");

                    if (isLocal)
                    {
                        Debug.Log($"[FishyBrainCloud] This client is now becoming the server host");
                        _isServer = true;
                        HandleServerConnectionState(new ServerConnectionStateArgs(LocalConnectionState.Started, Index));
                        HandleClientConnectionState(new ClientConnectionStateArgs(LocalConnectionState.Started, Index));
                        HandleRemoteConnectionState(new RemoteConnectionStateArgs(RemoteConnectionState.Started, newHostNetID, Index));
                        /*
                        foreach (int netId in _connectedClients)
                        {
                            Debug.Log($"[FishyBrainCloud] Handling remote connection state for NetId:{netId}");
                            //HandleRemoteConnectionState(new RemoteConnectionStateArgs(RemoteConnectionState.Started, netId, Index));
                        }
                        */
                    }
                    else
                    {
                        //if we are a client and not the new host, just let the new host know we want to remain connected
                        HandleClientConnectionState(new ClientConnectionStateArgs(LocalConnectionState.Started, Index));
                    }

                    //receiving this as the server socket means the client host has disconnected and the host status has been given to the next connected client
                    break;
                case "NET_ID":
                    //Debug.Log($"ServerSocket - OnSystemCallback NET_ID: {json}");
                    var netIdEvent = JsonUtility.FromJson<RelaySystemNetId>(json);
                    hostId = _brainCloud.RelayService.GetNetIdForCxId(hostCxId);
                    Debug.Log($"Host updated to {hostId}");

                    AddConnection(netIdEvent.netId);
                    if (!_isServer && GetConnectionState(false) != LocalConnectionState.Started && hostId != 40)
                    {
                        //if we are a client and are not yet marked as connected, mark as connected
                        HandleClientConnectionState(new ClientConnectionStateArgs(LocalConnectionState.Started, Index));
                    }else if (_isServer)
                    {
                        HandleRemoteConnectionState(new RemoteConnectionStateArgs(RemoteConnectionState.Started, netIdEvent.netId, Index));
                    }

                    break;
                default:
                    Debug.LogError("ServerSocket - OnSystemCallback Unknown system event: " + systemEvent.op);
                    break;
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
            _brainCloud.RelayService.DeregisterRelayCallback();
            _brainCloud.RelayService.Disconnect();
            HandleClientConnectionState(new ClientConnectionStateArgs(LocalConnectionState.Stopped, Index));

            return true;
        }

        private bool StopServer()
        {
            Debug.Log($"[FishyBrainCloud] StopServer called");

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
            _brainCloud.RelayService.DeregisterRelayCallback();
            _brainCloud.RelayService.Disconnect();
            HandleClientConnectionState(new ClientConnectionStateArgs(LocalConnectionState.Stopped, Index));

            return true;
        }

        private void AddConnection(int netId)
        {
            if (!_connectedClients.Contains(netId))
            {
                _connectedClients.Add(netId);
            }
        }
        private void RemoveConnection(int netId)
        {
            if (_connectedClients.Contains(netId))
            {
                _connectedClients.Remove(netId);
            }
        }

        public override void Shutdown()
        {
            Debug.Log($"[FishyBrainCloud] Shutdown called");
            if (_shutdownCalled) return;
            _shutdownCalled = true;
            StopConnection(false);
            //StopConnection(true);//_isServer ?
        }
        #endregion

        public override int GetMTU(byte channel) => _mtus[channel];

        private void OnApplicationQuit()
        {
            if (_brainCloud.RelayService.IsConnected())
            {
                _brainCloud.RelayService.Disconnect();
            }
        }
    }
}
