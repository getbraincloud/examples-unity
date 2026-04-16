using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using BrainCloud.JsonFx.Json;

#if DOT_NET
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
#endif

#if USE_WEB_REQUEST
using UnityEngine.Networking;
using UnityEngine;
#endif

public class Brainclouds2sRttComms
{
    public enum WebsocketStatus
    {
        OPEN,
        CLOSED,
        MESSAGE,
        ERROR,
        NONE
    }

    public enum RTTConnectionStatus
    {
        CONNECTED,
        DISCONNECTED,
        CONNECTING,
        DISCONNECTING
    }

    private struct RTTCommandResponse
    {
        public RTTCommandResponse(string in_service, string in_op, string in_msg)
        {
            Service = in_service;
            Operation = in_op;
            JsonMessage = in_msg;
        }
        public string Service { get; set; }
        public string Operation { get; set; }
        public string JsonMessage { get; set; }
    }

    public delegate void RTTCallback(string responseString);
    public string RTTConnectionID { get; private set; }
    public string RTTEventServer { get; private set; }

    private List<RTTCommandResponse> _queuedRTTCommands = new List<RTTCommandResponse>();
    private RTTConnectionStatus _rttConnectionStatus = RTTConnectionStatus.DISCONNECTED;
    private WebsocketStatus _webSocketStatus = WebsocketStatus.NONE;
    private TimeSpan _heartbeatRTTTime = TimeSpan.FromMilliseconds(10 * 1000);
    private RTTCallback _registeredCallback;
    private TimeSpan _sinceLastRTTHeartbeat;
    private BrainCloudS2S.S2SRequest _rttConnectionCallback;
    private BrainCloudS2S _brainCloudS2S;
    private BrainCloudWebSocket _webSocket;

    private bool _disconnectedWithReason = false;
    private bool _channelConnected = false;
    private string _channelId;
    private Dictionary<string, object> _rttHeaders = new Dictionary<string, object>();
    private Dictionary<string, object> _endpoint = null;
    private Dictionary<string, object> _disconnectJson = new Dictionary<string, object>();


    /// <summary>
    /// Check to see if we're not disconnected or we have a command to run.
    /// </summary>
    /// <returns>true if we have RTT updates to execute.</returns>
    public bool InquireRTTStatusForUpdate()
    {
        return _rttConnectionStatus != RTTConnectionStatus.DISCONNECTED || _queuedRTTCommands.Count > 0;
    }

    /// <summary>
    /// Returns true if RTT connection status is connected
    /// </summary>
    public bool IsRTTEnabled()
    {
        return _rttConnectionStatus == RTTConnectionStatus.CONNECTED;
    }

    ///<summary>
    ///Returns the status of the connection
    ///</summary>
    public RTTConnectionStatus GetConnectionStatus()
    {
        return _rttConnectionStatus;
    }

    /// <summary>
    /// Register to receive Raw Callbacks with RTT.
    /// After registering and ConnectToChannel() you can call SendRawRTTPacket().
    /// </summary>
    /// <param name="callback"></param>
    public void RegisterRTTRawCallback(RTTCallback callback)
    {
        if (_brainCloudS2S == null) return;
        if (_brainCloudS2S.SessionId != null)
        {
            _registeredCallback = callback;
        }
        else
        {
            _brainCloudS2S.LogString("Authentication is required to register callback");
        }
    }

    /// <summary>
    /// Removing the registered RTTRawCallback.
    /// </summary>
    public void DeregisterRTTRawCallback()
    {
        _registeredCallback = null;
    }

    /// <summary>
    /// Enables Real Time event for this session.
    /// Real Time events are disabled by default. Usually events
    /// need to be polled using GET_EVENTS. By enabling this, events will
    /// be received instantly when they happen through a TCP connection to an Event Server.
    ///
    ///This function will first call requestClientConnection, then connect to the address
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="context"></param>
    public void EnableRTT(BrainCloudS2S.S2SCallback callback, BrainCloudS2S context)
    {
        _brainCloudS2S = context;
        if (_brainCloudS2S.SessionId != null)
        {
            Dictionary<string, object> requstInfo = new Dictionary<string, object>();
            requstInfo = new Dictionary<string, object>();
            requstInfo.Add("service", "rttRegistration");
            requstInfo.Add("operation", "REQUEST_SYSTEM_CONNECTION");
            string contextInfo = JsonWriter.Serialize(requstInfo);
            _rttConnectionCallback = new BrainCloudS2S.S2SRequest();
            _rttConnectionCallback.callback = callback;
            _rttConnectionCallback.requestData = contextInfo;
            _brainCloudS2S.QueueRequest(contextInfo, onRTTConnectCallback);
        }
        else if (callback != null)
        {
            callback("Unable to request RTT connection without a session established, please authenticate and try again.");
        }
    }

    /// <summary>
    /// Disables Real Time event for this session.
    /// </summary>
    public void DisableRTT()
    {
        if (_rttConnectionStatus != RTTConnectionStatus.CONNECTED || _rttConnectionStatus == RTTConnectionStatus.DISCONNECTING)
        {
            return;
        }
        addQueueRTTResponse(new RTTCommandResponse("rttRegistration", "disconnect", "DisableRTT Called"));
    }

    public void RunCallbacks()
    {
        RTTCommandResponse toProcessResponse;
        lock (_queuedRTTCommands)
        {
            for (int i = 0; i < _queuedRTTCommands.Count; ++i)
            {
                toProcessResponse = _queuedRTTCommands[i];

                // WebSocket closed — tear down and bail
                if (_webSocketStatus == WebsocketStatus.CLOSED)
                {
                    _rttConnectionCallback.callback("RTT Connection has been closed. Re-Enable RTT to re-establish connection :" + toProcessResponse.JsonMessage);
                    _rttConnectionStatus = RTTConnectionStatus.DISCONNECTING;
                    disconnect();
                    break;
                }

                // WebSocket just opened — send the RTT CONNECT handshake to the server
                if (_rttConnectionStatus == RTTConnectionStatus.DISCONNECTED && toProcessResponse.Operation == "connect")
                {
                    _rttConnectionStatus = RTTConnectionStatus.CONNECTING;
                    sendRTTRequest(buildConnectionRequest());
                }

                // Server acknowledged our CONNECT — RTT is now live
                else if (_rttConnectionStatus == RTTConnectionStatus.CONNECTING && _rttConnectionCallback.callback != null && toProcessResponse.Operation == "connect")
                {
                    _sinceLastRTTHeartbeat = DateTime.Now.TimeOfDay;
                    _rttConnectionStatus = RTTConnectionStatus.CONNECTED;
                    _rttConnectionCallback.callback(toProcessResponse.JsonMessage);
                }

                // Server sent a disconnect while we were connected
                else if (_rttConnectionStatus == RTTConnectionStatus.CONNECTED && _rttConnectionCallback.callback != null && toProcessResponse.Operation == "disconnect")
                {
                    _rttConnectionStatus = RTTConnectionStatus.DISCONNECTING;
                    disconnect();
                }

                // RTT-level error
                else if (_rttConnectionCallback.callback != null && toProcessResponse.Operation == "error")
                {
                    _rttConnectionCallback.callback(toProcessResponse.JsonMessage ?? "Error - No Response from Server");
                }

                // Actual data message — forward to the registered callback
                else if (_registeredCallback != null)
                {
                    _registeredCallback(toProcessResponse.JsonMessage);
                }
                else
                {
                    _brainCloudS2S.LogString("WARNING no handler registered for RTT callbacks ");
                }

            }

            _queuedRTTCommands.Clear();
        }

        if (_rttConnectionStatus == RTTConnectionStatus.CONNECTED)
        {
            if ((DateTime.Now.TimeOfDay - _sinceLastRTTHeartbeat) >= _heartbeatRTTTime)
            {
                _sinceLastRTTHeartbeat = DateTime.Now.TimeOfDay;
                Dictionary<string, object> json = new Dictionary<string, object>();
                json["service"] = "rtt";
                json["operation"] = "HEARTBEAT";
                json["data"] = null;
                string heartBeatRequest = JsonWriter.Serialize(json);
                sendRTTRequest(heartBeatRequest, true);
            }
        }
    }

    /// <summary>
    /// Channel to connect for subscribing raw callbacks
    /// </summary>
    /// <param name="in_channel"></param>
    /// <param name="callback"></param>
    public void ConnectToChannel(string in_channelId, BrainCloudS2S.S2SCallback callback)
    {
        Dictionary<string, object> jsonData = new Dictionary<string, object>();
        jsonData["service"] = "chat";
        jsonData["operation"] = "SYS_CHANNEL_CONNECT";
        Dictionary<string, object> data = new Dictionary<string, object>();
        data["channelId"] = in_channelId;
        data["maxReturn"] = 100;
        jsonData["data"] = data;
        string jsonString = JsonWriter.Serialize(jsonData);
        _brainCloudS2S.QueueRequest(jsonString, (response) =>
        {
            _channelId = in_channelId;
            callback(response);
            _channelConnected = true;

            _brainCloudS2S.LogString("Channel Connected");
        });
    }

    /// <summary>
    /// Send a raw packet to the main channel.
    /// Must call RegisterRTTRawCallback() and ConnectToChannel() before sending anything,
    /// otherwise you won't receive callbacks for Raw Packets.
    /// </summary>
    /// <param name="in_jsonData">JSON Data to send</param>
    public void SendRawRTTPacket(Dictionary<string, object> in_jsonData)
    {
        if (!_channelConnected)
        {
            _brainCloudS2S.LogString("Connect to channel before sending packet.");
            return;
        }

        Dictionary<string, object> json = new Dictionary<string, object>
        {
            ["service"] = "chat",
            ["operation"] = "SYS_POST_CHAT_MESSAGE"
        };
        Dictionary<string, object> jsonData = new Dictionary<string, object>
        {
            ["channelId"] = _channelId,
            ["content"] = in_jsonData,
            ["recordInHistory"] = false
        };
        json["data"] = jsonData;
        string jsonDataString = JsonWriter.Serialize(json);
        _brainCloudS2S.QueueRequest(jsonDataString, null);
    }

    /// <summary>
    /// Sending a websocket request.
    /// Used for sending connection & heartbeat requests.
    /// </summary>
    /// <param name="in_message"></param>
    /// <param name="in_bLogMessage"></param>
    /// <returns></returns>
    private bool sendRTTRequest(string in_message, bool in_bLogMessage = true)
    {
        bool bMessageSent = false;
        // early return
        if (_webSocket == null)
        {
            return bMessageSent;
        }

        try
        {
            if (in_bLogMessage)
            {

                _brainCloudS2S.LogString("RTT SEND: " + in_message);
            }

            // Web Socket
            byte[] data = Encoding.ASCII.GetBytes(in_message);
            _webSocket.SendAsync(data);
        }
        catch (Exception socketException)
        {

            _brainCloudS2S.LogString("send exception: " + socketException);
            addQueueRTTResponse(new RTTCommandResponse("rttRegistration", "error", buildRTTRequestError(socketException.ToString())));
        }

        return bMessageSent;
    }

    /// <summary>
    /// Close websocket and reset any variables for the connection.
    /// Send a log if we disconnected unexpectedly and execute
    /// failure callback from EnableRTT.
    /// </summary>
    private void disconnect()
    {
        if (_webSocket != null) _webSocket.Close();

        RTTConnectionID = "";
        RTTEventServer = "";
        _channelConnected = false;
        _webSocket = null;

        if (_disconnectedWithReason == true)
        {

            _brainCloudS2S.LogString("RTT: Disconnect: " + JsonWriter.Serialize(_disconnectJson));
            if (_rttConnectionCallback.callback != null)
            {
                _rttConnectionCallback.callback((string)_disconnectJson["reason"]);
            }
        }
        _rttConnectionStatus = RTTConnectionStatus.DISCONNECTED;
    }

    /// <summary>
    /// Callback for after REQUEST_SYSTEM_CONNECTION request call.
    /// </summary>
    /// <param name="responseString"></param>
    private void onRTTConnectCallback(string responseString)
    {
        Dictionary<string, object> response = null;
        try
        {
            response = (Dictionary<string, object>)JsonReader.Deserialize(responseString);
        }
        catch
        {
            return;
        }
        Dictionary<string, object> jsonData = (Dictionary<string, object>)response["data"];
        Array endpoints = (Array)jsonData["endpoints"];
        _rttHeaders = (Dictionary<string, object>)jsonData["auth"];
        _brainCloudS2S.LogString("RTT Connect Callback " + _rttConnectionStatus);

        //   1st choice: websocket + ssl
        //   2nd: websocket
        _endpoint = getEndpointForType(endpoints, "ws", true);
        if (_endpoint == null)
        {
            _endpoint = getEndpointForType(endpoints, "ws", false);
        }
        if (_rttConnectionStatus == RTTConnectionStatus.DISCONNECTED)
        {
            bool sslEnabled = (bool)_endpoint["ssl"];
            string url = (sslEnabled ? "wss://" : "ws://") + _endpoint["host"] as string + ":" + (int)_endpoint["port"] + getUrlQueryParameters();
            setupWebSocket(url);
        }
    }

    private Dictionary<string, object> getEndpointForType(Array endpoints, string type, bool in_bWantSsl)
    {
        Dictionary<string, object> toReturn = null;
        Dictionary<string, object> tempToReturn = null;
        for (int i = 0; i < endpoints.Length; ++i)
        {
            tempToReturn = endpoints.GetValue(i) as Dictionary<string, object>;
            if (tempToReturn["protocol"] as string == type)
            {
                if (in_bWantSsl)
                {
                    if ((bool)tempToReturn["ssl"])
                    {
                        toReturn = tempToReturn;
                        break;
                    }
                }
                else
                {
                    toReturn = tempToReturn;
                    break;
                }
            }
        }

        return toReturn;
    }

    private string getUrlQueryParameters()
    {
        string sToReturn = "?";
        int count = 0;
        foreach (KeyValuePair<string, object> item in _rttHeaders)
        {
            if (count > 0) sToReturn += "&";
            sToReturn += item.Key + "=" + item.Value;
            ++count;
        }

        return sToReturn;
    }

    private string buildConnectionRequest()
    {
        Dictionary<string, object> system = new Dictionary<string, object>();
#if DOT_NET
        system["platform"] = "csharp";
#elif USE_WEB_REQUEST
        system["platform"] = "csharp-unity";
#endif
        system["protocol"] = "ws";

        Dictionary<string, object> jsonData = new Dictionary<string, object>();
        jsonData["appId"] = _brainCloudS2S.AppId;
        jsonData["sessionId"] = _brainCloudS2S.SessionId;
        jsonData["profileId"] = "s";
        jsonData["system"] = system;

        jsonData["auth"] = _rttHeaders;

        Dictionary<string, object> json = new Dictionary<string, object>();
        json["service"] = "rtt";
        json["operation"] = "CONNECT";
        json["data"] = jsonData;

        return JsonWriter.Serialize(json);
    }

    private void setupWebSocket(string in_url)
    {
        _brainCloudS2S.LogString("setupWebSocket: " + in_url);
        _webSocket = new BrainCloudWebSocket(in_url);
        _webSocket.OnClose += webSocket_OnClose;
        _webSocket.OnOpen += websocket_OnOpen;
        _webSocket.OnMessage += webSocket_OnMessage;
        _webSocket.OnError += webSocket_OnError;
    }

    private void webSocket_OnClose(BrainCloudWebSocket sender, int code, string reason)
    {

        _brainCloudS2S.LogString("RTT: Connection closed: " + reason);
        _webSocketStatus = WebsocketStatus.CLOSED;
        addQueueRTTResponse(new RTTCommandResponse("rttRegistration", "disconnect", reason));
    }

    private void websocket_OnOpen(BrainCloudWebSocket accepted)
    {

        _brainCloudS2S.LogString("RTT: WebSocket is open.");
        _webSocketStatus = WebsocketStatus.OPEN;
        addQueueRTTResponse(new RTTCommandResponse("rttRegistration", "connect", ""));
    }

    private void webSocket_OnError(BrainCloudWebSocket sender, string message)
    {

        _brainCloudS2S.LogString("RTT Error: " + message);
        _webSocketStatus = WebsocketStatus.ERROR;
        addQueueRTTResponse(new RTTCommandResponse("rttRegistration", "error", buildRTTRequestError(message)));
    }

    private void webSocket_OnMessage(BrainCloudWebSocket sender, byte[] data)
    {
        if (data.Length == 0) return;
        _webSocketStatus = WebsocketStatus.MESSAGE;
        string message = System.Text.Encoding.UTF8.GetString(data);
        onRecv(message);
    }

    private void onRecv(string in_message)
    {

        _brainCloudS2S.LogString("RTT RECV: " + in_message);

        Dictionary<string, object> response = (Dictionary<string, object>)JsonReader.Deserialize(in_message);

        string service = (string)response["service"];
        string operation = (string)response["operation"];

        Dictionary<string, object> data = null;
        if (response.ContainsKey("data"))
            data = (Dictionary<string, object>)response["data"];
        if (operation == "CONNECT")
        {
            int heartBeat = _heartbeatRTTTime.Milliseconds / 1000;
            try
            {
                heartBeat = (int)data["heartbeatSeconds"];
            }
            catch (Exception)
            {
                heartBeat = (int)data["wsHeartbeatSecs"];
            }

            _brainCloudS2S.LogString("RTT WebSocket Connection Established.");
            _heartbeatRTTTime = TimeSpan.FromMilliseconds(heartBeat * 1000);
        }
        else if (operation == "DISCONNECT")
        {
            _disconnectedWithReason = true;
            _disconnectJson["reason_code"] = (int)data["reasonCode"];
            _disconnectJson["reason"] = (string)data["reason"];
            _disconnectJson["severity"] = "ERROR";
        }

        if (data != null)
        {
            if (data.ContainsKey("cxId")) RTTConnectionID = (string)data["cxId"];
            if (data.ContainsKey("evs")) RTTEventServer = (string)data["evs"];
        }

        if (operation != "HEARTBEAT")
        {
            addQueueRTTResponse(new RTTCommandResponse(service.ToLower(), operation.ToLower(), in_message));
        }
    }

    private string buildRTTRequestError(string in_statusMessage)
    {
        Dictionary<string, object> json = new Dictionary<string, object>();
        json["status"] = 403;
        json["reason_code"] = 80300;
        json["status_message"] = in_statusMessage;
        json["severity"] = "ERROR";

        return JsonWriter.Serialize(json);
    }

    private void addQueueRTTResponse(RTTCommandResponse in_command)
    {
        lock (_queuedRTTCommands)
        {
            _queuedRTTCommands.Add(in_command);
        }
    }
}
