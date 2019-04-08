//----------------------------------------------------
// brainCloud client source code
// Copyright 2016 bitHeads, inc.
//----------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using JsonFx.Json;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

namespace BrainCloud.Internal
{
    internal sealed class RSComms
    {
        /// <summary>
        /// 
        /// </summary>
        public RSComms(BrainCloudClient in_client)
        {
            m_clientRef = in_client;
        }

        /// <summary>
        /// </summary>
        /// <param name="in_connectionType"></param>
        /// <param name="in_success"></param>
        /// <param name="in_failure"></param>
        /// <param name="cb_object"></param>
        public void Connect(eRSConnectionType in_connectionType = eRSConnectionType.WEBSOCKET, Dictionary<string, object> in_options = null, SuccessCallback in_success = null, FailureCallback in_failure = null, object cb_object = null)
        {
#if UNITY_WEBGL
            if (in_connectionType != eRSConnectionType.WEBSOCKET)
            {
                m_clientRef.Log("Non-WebSocket Connection Type Requested, on WEBGL.  Please connect via WebSocket.");

                if (in_failure != null)
                    in_failure(403, ReasonCodes.CLIENT_NETWORK_ERROR_TIMEOUT, buildNonWSConnectionRequestError(), cb_object);
                return;
            }
#endif
            if (!m_bIsConnected)
            {
                m_connectedSuccessCallback = in_success;
                m_connectionFailureCallback = in_failure;
                m_connectedObj = cb_object;

                m_connectionOptions = in_options;
                m_currentConnectionType = in_connectionType;
                connectRSConnection();
            }
        }

        /// <summary>
        /// Disables Real Time event for this session.
        /// </summary>
        public void Disconnect()
        {
            addRSCommandResponse(new RSCommandResponse(ServiceName.RoomServer.Value, "disconnect", "Disconnect Called"));
        }

        /// <summary>
        /// 
        /// </summary>
        ///
        public void RegisterDataCallback(RSDataCallback in_callback)
        {
            m_registeredDataCallback = in_callback;
        }

        /// <summary>
        /// 
        /// </summary>
        public void DeregisterDataCallback()
        {
            m_registeredDataCallback = null;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Send(string in_message)
        {
            send(RLAY_HEADER + in_message);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Send(byte[] in_data, string in_header = RLAY_HEADER)
        {
            // appened RLAY to the beginning
            byte[] destination = concatenateByteArrays(Encoding.ASCII.GetBytes(in_header), in_data);
            send(destination);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Echo(string in_message)
        {
            send(ECHO_HEADER + in_message);
        }
        /// <summary>
        /// 
        /// </summary>
        public void Echo(byte[] in_data)
        {
            Send(in_data, ECHO_HEADER);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Ping()
        {
            m_sentPing = DateTime.Now.Ticks;
            short lastPingShort = Convert.ToInt16(LastPing * 0.0001);
            byte data1, data2;
            fromShort(lastPingShort, out data1, out data2);

            byte[] dataArr = { data1, data2 };

            Send(dataArr, PING_HEADER);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Update()
        {
            RSCommandResponse toProcessResponse;
            lock (m_queuedRSCommands)
            {
                for (int i = 0; i < m_queuedRSCommands.Count; ++i)
                {
                    toProcessResponse = m_queuedRSCommands[i];

                    if (m_bIsConnected && m_connectedSuccessCallback != null && toProcessResponse.Operation == "connect")
                    {
                        m_connectedSuccessCallback(toProcessResponse.JsonMessage, m_connectedObj);
                    }
                    else if (m_bIsConnected && m_connectionFailureCallback != null &&
                        toProcessResponse.Operation == "error" || toProcessResponse.Operation == "disconnect")
                    {
                        if (toProcessResponse.Operation == "disconnect")
                            disconnect();

                        // TODO:
                        if (m_connectionFailureCallback != null)
                            m_connectionFailureCallback(400, -1, toProcessResponse.JsonMessage, m_connectedObj);
                    }

                    if (!m_bIsConnected && toProcessResponse.Operation == "connect")
                    {
                        m_bIsConnected = true;
                        send(buildConnectionRequest());
                    }

                    if (m_registeredDataCallback != null && toProcessResponse.RawData != null)
                        m_registeredDataCallback(toProcessResponse.RawData);
                }

                m_queuedRSCommands.Clear();
            }
        }

        /// <summary>
        /// Call Ping() to get an updated LastPing value
        /// </summary>
        public long LastPing { get; private set; }

        #region private
        private string buildConnectionRequest()
        {
            Dictionary<string, object> json = new Dictionary<string, object>();
            json["op"] = "CONNECT";
            json["profileId"] = m_clientRef.ProfileId;
            json["lobbyId"] = m_connectionOptions["lobbyId"] as string;
            json["passcode"] = m_connectionOptions["passcode"] as string;

            return "CONN" + JsonWriter.Serialize(json);
        }

        private string buildNonWSConnectionRequestError()
        {
            Dictionary<string, object> json = new Dictionary<string, object>();
            json["status"] = 403;
            json["reason_code"] = ReasonCodes.CLIENT_NETWORK_ERROR_TIMEOUT;
            json["status_message"] = "Non-WebSocket Connection Type Requested, on WEBGL.  Please connect via WebSocket.";
            json["severity"] = "ERROR";

            return JsonWriter.Serialize(json);
        }

        private string buildDisconnectRequest()
        {
            return "DNCT";
        }
        /// <summary>
        /// 
        /// </summary>
        private void connectRSConnection()
        {
            if (!m_bIsConnected)
            {
                startReceivingRSConnectionAsync();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void disconnect()
        {
            if (m_bIsConnected) send(buildDisconnectRequest());

            if (m_webSocket != null) m_webSocket.Close();
            m_webSocket = null;

            if (m_udpClient != null) m_udpClient.Close();
            m_udpClient = null;

            if (m_tcpClient != null) m_tcpClient.Close();
            m_tcpClient = null;

            m_bIsConnected = false;
        }

        /// <summary>
        /// 
        /// </summary>
        private bool send(string in_message)
        {
            byte[] data = Encoding.ASCII.GetBytes(in_message);
            return send(data);
        }

        /// <summary>
        /// 
        /// </summary>
        private bool send(byte[] in_data)
        {
            bool bMessageSent = false;
            // early return, based on type
            switch (m_currentConnectionType)
            {
                case eRSConnectionType.WEBSOCKET:
                    {
                        if (m_webSocket == null)
                            return bMessageSent;
                    }
                    break;
                case eRSConnectionType.TCP:
                    {
                        if (m_tcpClient == null)
                            return bMessageSent;
                    }
                    break;
                case eRSConnectionType.UDP:
                    {
                        if (m_udpClient == null)
                            return bMessageSent;
                    }
                    break;
                default: break;
            }

            // actually do the send
            try
            {
                switch (m_currentConnectionType)
                {
                    case eRSConnectionType.WEBSOCKET:
                        {
                            //m_clientRef.Log("RS WS SEND Bytes : " + in_data.Length);
                            m_webSocket.SendAsync(in_data);
                            bMessageSent = true;
                        }
                        break;
                    case eRSConnectionType.TCP:
                        {
                            // we may not always be able to parse this to ascii
                            //string recvOpp = Encoding.ASCII.GetString(in_data);
                            //m_clientRef.Log("RS TCP SEND msg : " + recvOpp);

                            byte data1, data2;
                            short sizeOfData = Convert.ToInt16(in_data.Length);
                            fromShortBE(sizeOfData, out data1, out data2);
                            // append length prefixed, before sending off
                            byte[] dataArr = { data1, data2 };
                            in_data = concatenateByteArrays(dataArr, in_data);

                            // send Async
                            tcpWrite(in_data);
                            bMessageSent = true;
                        }
                        break;
                    case eRSConnectionType.UDP:
                        {
                            //m_clientRef.Log("RS UDP SEND Bytes : " + in_data.Length);
                            m_udpClient.SendAsync(in_data, in_data.Length);
                            bMessageSent = true;
                        }
                        break;
                    default: break;
                }
            }
            catch (Exception socketException)
            {
                m_clientRef.Log("send exception: " + socketException);
                addRSCommandResponse(new RSCommandResponse(ServiceName.RoomServer.Value, "error", socketException.ToString()));
            }

            return bMessageSent;
        }

        /// <summary>
        /// 
        /// </summary>
        private void startReceivingRSConnectionAsync()
        {
            bool sslEnabled = (bool)m_connectionOptions["ssl"];
            string host = (string)m_connectionOptions["host"];
            int port = (int)m_connectionOptions["port"];
            switch (m_currentConnectionType)
            {
                case eRSConnectionType.WEBSOCKET:
                    {
                        connectWebSocket(host, port, sslEnabled);
                    }
                    break;
                case eRSConnectionType.TCP:
                    {
                        connectTCPAsync(host, port);
                    }
                    break;
                case eRSConnectionType.UDP:
                    {
                        connectUDPAsync(host, port);
                    }
                    break;
                default: break;
            }
        }

        private void WebSocket_OnClose(BrainCloudWebSocket sender, int code, string reason)
        {
            m_clientRef.Log("Connection closed: " + reason);
            addRSCommandResponse(new RSCommandResponse(ServiceName.RoomServer.Value, "disconnect", reason));
        }

        private void Websocket_OnOpen(BrainCloudWebSocket accepted)
        {
            m_clientRef.Log("Connection established.");
            addRSCommandResponse(new RSCommandResponse(ServiceName.RoomServer.Value, "connect", ""));
        }

        private void WebSocket_OnMessage(BrainCloudWebSocket sender, byte[] data)
        {
            onRecv(data);
        }

        private void WebSocket_OnError(BrainCloudWebSocket sender, string message)
        {
            m_clientRef.Log("Error: " + message);
            addRSCommandResponse(new RSCommandResponse(ServiceName.RoomServer.Value, "error", message));
        }

        /// <summary>
        /// 
        /// </summary>
        private void onRecv(byte[] in_data)
        {
            if (in_data.Length >= MSG_HEADER_LENGTH)
            {
                bool isUDP = m_currentConnectionType == eRSConnectionType.UDP;

                // get msgOp
                byte[] msgOp = new byte[MSG_HEADER_LENGTH];
                Buffer.BlockCopy(in_data, 0, msgOp, 0, MSG_HEADER_LENGTH);
                string recvOpp = Encoding.ASCII.GetString(msgOp);

                bool bOppRSMG = recvOpp == RSMG_HEADER;
                int headerLength = MSG_HEADER_LENGTH;
                if (bOppRSMG && isUDP)
                    headerLength += SIZE_OF_UDP_FLAGS;

                if (bOppRSMG || recvOpp == RLAY_HEADER || recvOpp == ECHO_HEADER) // Room server msg or RLAY
                {
                    // bytes after the headers removed
                    byte[] cutOffData = new byte[in_data.Length - headerLength];
                    Buffer.BlockCopy(in_data, headerLength, cutOffData, 0, cutOffData.Length);

                    addRSCommandResponse(new RSCommandResponse(ServiceName.RoomServer.Value, "onrecv", "", cutOffData));

                    // and acknowledge we got it
                    if (isUDP)
                    {
                        cutOffData = new byte[SIZE_OF_UDP_FLAGS];
                        Buffer.BlockCopy(in_data, MSG_HEADER_LENGTH, cutOffData, 0, SIZE_OF_UDP_FLAGS);
                        // send back ACKN
                        Send(cutOffData, ACKN_HEADER);
                    }
                }
                else if (recvOpp == PONG_HEADER)
                {
                    LastPing = DateTime.Now.Ticks - m_sentPing;
                    //m_clientRef.Log("LastPing: " + (LastPing * 0.0001f).ToString() + "ms");
                }
            }
        }

        private void onUDPRecv(IAsyncResult result)
        {
            // this is what had been passed into BeginReceive as the second parameter:
            UdpClient udpClient = result.AsyncState as UdpClient;

            string host = (string)m_connectionOptions["host"];
            int port = (int)m_connectionOptions["port"];
            IPEndPoint source = new IPEndPoint(IPAddress.Parse(host), port);

            if (udpClient != null)
            {
                // get the actual message and fill out the source:
                byte[] data = udpClient.EndReceive(result, ref source);
                onRecv(data);

                // schedule the next receive operation once reading is done:
                udpClient.BeginReceive(new AsyncCallback(onUDPRecv), udpClient);
            }
        }

        /// <summary>
        /// Writes the specified message to the stream.
        /// </summary>
        /// <param name="message"></param>
        private void tcpWrite(byte[] message)
        {
            // Add this message to the list of message to send. If it's the only one in the
            // queue, fire up the async events to send it.
            try
            {
                lock (fLock)
                {
                    fToSend.Enqueue(message);
                    if (1 == fToSend.Count)
                    {
                        m_tcpStream.BeginWrite(message, 0, message.Length, tcpFinishWrite, null);
                    }

                }
            }
            catch (Exception e)
            {
                addRSCommandResponse(new RSCommandResponse(ServiceName.RTTRegistration.Value, "error", e.ToString()));
            }
        }

        // ASync TCP Writes
        private object fLock = new object();
        private Queue<byte[]> fToSend = new Queue<byte[]>();
        private void tcpFinishWrite(IAsyncResult result)
        {
            try
            {
                m_tcpStream.EndWrite(result);
                lock (fLock)
                {
                    // Pop the message we just sent out of the queue
                    fToSend.Dequeue();

                    // See if there's anything else to send. Note, do not pop the message yet because
                    // that would indicate its safe to start writing a new message when its not.
                    if (fToSend.Count > 0)
                    {
                        byte[] final = fToSend.Peek();
                        m_tcpStream.BeginWrite(final, 0, final.Length, tcpFinishWrite, null);
                    }
                }
            }
            catch (Exception e)
            {
                addRSCommandResponse(new RSCommandResponse(ServiceName.RTTRegistration.Value, "error", e.ToString()));
            }
        }

        // ASync TCP Reads
        const int SIZE_OF_TCP_LENGTH_PREFIX = 2;
        private int m_tcpBytesRead = 0; // the ones already processed
        private byte[] m_tcpHeaderReadBuffer = new byte[SIZE_OF_TCP_LENGTH_PREFIX];
        private void onTCPReadHeader(IAsyncResult ar)
        {
            try
            {
                // Read precisely SIZE_OF_HEADER for the length of the following message
                int read = m_tcpStream.EndRead(ar);
                if (SIZE_OF_TCP_LENGTH_PREFIX == read)
                {
                    byte[] arr = { m_tcpHeaderReadBuffer[0], m_tcpHeaderReadBuffer[1] };
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(arr);

                    short sizeToRead = BitConverter.ToInt16(arr, 0);
                    // Create a buffer to hold the message and start reading it.
                    m_tcpBytesRead = 0;
                    m_tcpReadBuffer = new byte[sizeToRead];
                    m_tcpStream.BeginRead(m_tcpReadBuffer, 0, m_tcpReadBuffer.Length, onTCPFinishRead, null);
                }
                else
                {
                    // re-read for header
                    m_tcpStream.BeginRead(m_tcpHeaderReadBuffer, 0, SIZE_OF_TCP_LENGTH_PREFIX, new AsyncCallback(onTCPReadHeader), null);
                }
            }
            catch (Exception e)
            {
                addRSCommandResponse(new RSCommandResponse(ServiceName.RTTRegistration.Value, "error", e.ToString()));
            }
        }
        private void onTCPFinishRead(IAsyncResult result)
        {
            try
            {
                // Finish reading from our stream. 0 bytes read means stream was closed
                int read = m_tcpStream.EndRead(result);
                if (0 == read)
                    throw new Exception();

                // Increment the number of bytes we've read. If there's still more to get, get them
                m_tcpBytesRead += read;
                if (m_tcpBytesRead < m_tcpReadBuffer.Length)
                {
                    //m_clientRef.Log("m_tcpBytesRead < m_tcpBuffer.Length " + m_tcpBytesRead + " " + m_tcpReadBuffer.Length);
                    m_tcpStream.BeginRead(m_tcpReadBuffer, m_tcpBytesRead, m_tcpReadBuffer.Length - m_tcpBytesRead, onTCPFinishRead, null);
                    return;
                }

                // Should be exactly the right number read now.
                if (m_tcpBytesRead != m_tcpReadBuffer.Length)
                    throw new Exception();

                //string in_message = Encoding.ASCII.GetString(m_tcpReadBuffer);
                //m_clientRef.Log("RS TCP RECV: " + m_tcpReadBuffer.Length + "bytes - " + in_message);

                // Handle the message
                onRecv(m_tcpReadBuffer);
                // read the next header
                m_tcpStream.BeginRead(m_tcpHeaderReadBuffer, 0, SIZE_OF_TCP_LENGTH_PREFIX, new AsyncCallback(onTCPReadHeader), null);
            }
            catch (Exception e)
            {
                addRSCommandResponse(new RSCommandResponse(ServiceName.RTTRegistration.Value, "error", e.ToString()));
            }
        }

        private void connectWebSocket(string in_host, int in_port, bool in_sslEnabled)
        {
            string url = (in_sslEnabled ? "wss://" : "ws://") + in_host + ":" + in_port;
            m_webSocket = new BrainCloudWebSocket(url);
            m_webSocket.OnClose += WebSocket_OnClose;
            m_webSocket.OnOpen += Websocket_OnOpen;
            m_webSocket.OnMessage += WebSocket_OnMessage;
            m_webSocket.OnError += WebSocket_OnError;
        }

        private async void connectTCPAsync(string host, int port)
        {
            bool success = await Task.Run(async () =>
            {
                try
                {
                    m_tcpClient = new TcpClient();
                    m_tcpClient.NoDelay = true;
                    m_tcpClient.Client.NoDelay = true;
                    await m_tcpClient.ConnectAsync(host, port);
                }
                catch (Exception e)
                {
                    addRSCommandResponse(new RSCommandResponse(ServiceName.RoomServer.Value, "error", e.ToString()));
                    return false;
                }
                return true;
            });

            if (success)
            {
                m_tcpStream = m_tcpClient.GetStream();
                addRSCommandResponse(new RSCommandResponse(ServiceName.RoomServer.Value, "connect", ""));
                m_tcpStream.BeginRead(m_tcpHeaderReadBuffer, 0, SIZE_OF_TCP_LENGTH_PREFIX, new AsyncCallback(onTCPReadHeader), null);
            }
        }

        private async void connectUDPAsync(string host, int port)
        {
            bool success = await Task.Run(async () =>
            {
                try
                {
                    m_udpClient = new UdpClient();
                    await m_udpClient.Client.ConnectAsync(host, port);
                }
                catch (Exception e)
                {
                    addRSCommandResponse(new RSCommandResponse(ServiceName.RoomServer.Value, "error", e.ToString()));
                    return false;
                }
                return true;
            });

            if (success)
            {
                addRSCommandResponse(new RSCommandResponse(ServiceName.RoomServer.Value, "connect", ""));
                m_udpClient.BeginReceive(new AsyncCallback(onUDPRecv), m_udpClient);
            }
        }

        private void addRSCommandResponse(RSCommandResponse in_command)
        {
            lock (m_queuedRSCommands)
            {
                m_queuedRSCommands.Add(in_command);
            }
        }

        private byte[] concatenateByteArrays(byte[] a, byte[] b)
        {
            byte[] rv = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, rv, 0, a.Length);
            Buffer.BlockCopy(b, 0, rv, a.Length, b.Length);
            return rv;
        }

        private void fromShort(short number, out byte byte1, out byte byte2)
        {
            byte2 = (byte)(number >> 8);
            byte1 = (byte)(number >> 0);
        }

        private void fromShortBE(short number, out byte byte1, out byte byte2)
        {
            byte2 = (byte)(number >> 0);
            byte1 = (byte)(number >> 8);
        }

        private Dictionary<string, object> m_connectionOptions = null;
        private eRSConnectionType m_currentConnectionType = eRSConnectionType.INVALID;
        private bool m_bIsConnected = false;

        // start
        // different connection types
        private BrainCloudWebSocket m_webSocket = null;
        private UdpClient m_udpClient = null;

        private TcpClient m_tcpClient = null;
        private NetworkStream m_tcpStream = null;
        private byte[] m_tcpReadBuffer = new byte[MAX_PACKETSIZE];
        private byte[] m_tcpWriteBuffer = new byte[MAX_PACKETSIZE];
        // end 

        private const int MSG_HEADER_LENGTH = 4;
        private const int SIZE_OF_UDP_FLAGS = 2;
        private const int MAX_PACKETSIZE = 1024; // TODO:: based off of some config 
        private const string ACKN_HEADER = "ACKN";
        private const string ECHO_HEADER = "ECHO";
        private const string PING_HEADER = "PING";
        private const string PONG_HEADER = "PONG";
        private const string RLAY_HEADER = "RLAY";
        private const string RSMG_HEADER = "RSMG";

        private BrainCloudClient m_clientRef;
        private long m_sentPing = DateTime.Now.Ticks;

        // success callbacks
        private SuccessCallback m_connectedSuccessCallback = null;
        private FailureCallback m_connectionFailureCallback = null;
        private object m_connectedObj = null;

        private RSDataCallback m_registeredDataCallback = null;
        private List<RSCommandResponse> m_queuedRSCommands = new List<RSCommandResponse>();
        private struct RSCommandResponse
        {
            public RSCommandResponse(string in_service, string in_op, string in_msg, byte[] in_data = null)
            {
                Service = in_service;
                Operation = in_op;
                JsonMessage = in_msg;
                RawData = in_data;
            }
            public string Service { get; set; }
            public string Operation { get; set; }
            public string JsonMessage { get; set; }
            public byte[] RawData { get; set; }
        }
        #endregion
    }
}

namespace BrainCloud
{
    #region public enums
    public enum eRSConnectionType
    {
        INVALID,
        WEBSOCKET,
        TCP,
        UDP,

        MAX
    }
    #endregion
}