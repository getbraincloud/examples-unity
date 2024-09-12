//----------------------------------------------------
// brainCloud client source code
// Copyright 2020 bitHeads, inc.
//----------------------------------------------------
#if ((UNITY_5_3_OR_NEWER) && !UNITY_WEBPLAYER && (!UNITY_IOS || ENABLE_IL2CPP)) || UNITY_2018_3_OR_NEWER
#define USE_WEB_REQUEST //Comment out to force use of old WWW class on Unity 5.3+
#else
#define DOT_NET
#endif

using System;
using System.Collections.Generic;
using System.Text;
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
using System.IO;
using System.Collections;
using System.Runtime.Serialization;
using BrainCloud.JsonFx.Json;

public class BrainCloudS2S
{
    private static int NO_PACKET_EXPECTED = -1;
    private static int SERVER_SESSION_EXPIRED = 40365;
    private static string DEFAULT_S2S_URL = "https://api.internal.braincloudservers.com/s2sdispatcher";
    public string ServerURL
    {
        get; private set;
    }
    public string AppId
    {
        get; private set;
    }
    public string ServerSecret
    {
        get; private set;
    }
    public string ServerName
    {
        get; private set;
    }
    public string SessionId
    {
        get; private set;
    }
    public bool IsInitialized
    {
        get; private set;
    }
    public bool LoggingEnabled
    {
        get; set;
    }

    public enum State
    {
        Authenticated,
        Authenticating,
        Disconnected
    }

    private long _packetId = 0;
    private long _heartbeatSeconds = 1800; //Default to 30 mins  
    private State _state = State.Disconnected;
    private bool _autoAuth = false;
    private TimeSpan _heartbeatTimer;
    private DateTime _lastHeartbeat;
    private ArrayList _requestQueue = new ArrayList();
    private ArrayList _waitingForAuthRequestQueue = new ArrayList();
    public delegate void S2SCallback(string responseString);
    S2SRequest activeRequest;

    private struct S2SRequest
    {
#if DOT_NET
        public HttpWebRequest request;
#endif
#if USE_WEB_REQUEST
        public UnityWebRequest request;
#endif
        public string requestData;
        public S2SCallback callback;
    }

    /**
        * Initialize brainclouds2s context
        *
        * @param appId Application ID
        * @param serverName Server name
        * @param serverSecret Server secret key
        * @param autoAuth automatic authentication with braincloud
        */
    public void Init(string appId, string serverName, string serverSecret, bool autoAuth)
    {
        Init(appId, serverName, serverSecret, autoAuth, DEFAULT_S2S_URL);
    }

    /**
    * Initialize brainclouds2s context
    *
    * @param appId Application ID
    * @param serverName Server name
    * @param serverSecret Server secret key
    * @param serverUrl The server URL to send the request to. Defaults to the
    * default brainCloud portal
    */
    public void Init(string appId, string serverName, string serverSecret, bool autoAuth, string serverUrl)
    {
        _packetId = 0;
        IsInitialized = true;
        ServerURL = serverUrl;
        AppId = appId;
        ServerSecret = serverSecret;
        ServerName = serverName;
        _autoAuth = autoAuth;
        SessionId = null;
        activeRequest.request = null;
        _heartbeatTimer = TimeSpan.FromSeconds(_heartbeatSeconds);

        LogString($"Initialized S2S AppId:{appId} ServerName:{serverName} ServerSecret:{serverSecret} ServerUrl:{serverUrl} ");
    }

    /**
    * Authenticate with brainCloud
    */
    public void Authenticate()
    {
        Authenticate(OnAuthenticationCallback);
    }

    /**
    * Send an S2S request.
    *
    * @param json S2S operation to be sent as a string
    * @param callback Callback function
    */
    public void Request(string jsonRequestData, S2SCallback callback)
    {
        if (_autoAuth == true)
        {
            if (!(_state == State.Authenticated) && _packetId == 0) //this is an authentication request no matter what
            {
                Authenticate(OnAuthenticationCallback);
            }
        }
        if (!(_state == State.Authenticated)) // these are the requests that have been made that are awaiting authentication. We NEED to store the request so we can properly call this function back for additional requests that are made after authenitcation.
        {
            S2SRequest nonAuthRequest = new S2SRequest();
            nonAuthRequest.requestData = jsonRequestData;
            nonAuthRequest.callback = callback;

            _waitingForAuthRequestQueue.Add(nonAuthRequest);
        }
        else
        {
            QueueRequest(jsonRequestData, callback);
        }
    }

    /**
    * Send an S2S request.
    *
    * @param json S2S operation to be sent as a string
    * @param callback Callback function
    */
    public void Request(Dictionary<string, object> jsonRequestData, S2SCallback callback)
    {
        string jsonRequestDataString = JsonWriter.Serialize(jsonRequestData);
        if (_autoAuth == true)
        {
            if (!(_state == State.Authenticated) && _packetId == 0) //this is an authentication request no matter what
            {
                Authenticate(OnAuthenticationCallback);
            }
        }
        if (!(_state == State.Authenticated)) // these are the requests that have been made that are awaiting authentication. We NEED to store the request so we can properly call this function back for additional requests that are made after authenitcation.
        {
            S2SRequest nonAuthRequest = new S2SRequest();
            nonAuthRequest.requestData = jsonRequestDataString;
            nonAuthRequest.callback = callback;

            _waitingForAuthRequestQueue.Add(nonAuthRequest);
        }
        else
        {
            QueueRequest(jsonRequestDataString, callback);
        }
    }

    private void QueueRequest(string jsonRequestData, S2SCallback callback)
    {
        Debug.Log("[S2S] Queuing request: " + jsonRequestData);
#if DOT_NET
        //create new request
        HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(ServerURL);

        //customize request
        httpRequest.Method = "POST";
        httpRequest.ContentType = "application/json; charset=utf-8";
#endif
#if USE_WEB_REQUEST
        
        //create new request
        UnityWebRequest httpRequest = UnityWebRequest.Post(ServerURL, new Dictionary<string, string>());

        //customize request
        httpRequest.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
#endif
        //store request info
        S2SRequest req = new S2SRequest();
        req.request = httpRequest;
        req.requestData = jsonRequestData;
        req.callback = callback;

        //add to requestqueue
        _requestQueue.Add(req);

        SendData(req.request, req.requestData);
    }

    private string CreatePacket(string packetData)
    {
        //form the packet
        string packetDataString = "{\"packetId\":" + (int)_packetId;
        if (SessionId != null)
        {
            if (SessionId.Length != 0)
            {
                packetDataString += ",\"sessionId\":\"" + SessionId + "\"";
            }
        }
        if (AppId != null)
        {
            packetDataString += ",\"appId\":\"" + AppId + "\"";
        }
        packetDataString += ",\"messages\":[" + packetData + "]}";

        _packetId++;

        return packetDataString;
    }
#if DOT_NET
    private void SendData(HttpWebRequest request, string dataPacket)
    {
        string packet = CreatePacket(dataPacket);                   //create data packet of the data with packetId info

        byte[] byteArray = Encoding.UTF8.GetBytes(packet);          //convert data packet to byte[]

        Stream requestStream = request.GetRequestStream();          //gets a stream to send dataPacket for request
        requestStream.Write(byteArray, 0, byteArray.Length);        //writes dataPacket to stream and sends data with request. 
        request.ContentLength = byteArray.Length;
    }
#endif

#if USE_WEB_REQUEST
    private void SendData(UnityWebRequest request, string dataPacket)
    {
        string packet = CreatePacket(dataPacket);                   //create data packet of the data with packetId info

        LogString("Sending Request: " + packet + " to url " + request.url);

        byte[] byteArray = Encoding.UTF8.GetBytes(packet);          //convert data packet to byte[]
        request.uploadHandler = new UploadHandlerRaw(byteArray);    //prepare data

        request.SendWebRequest();
    }
#endif


    private void ResetHeartbeat()
    {
        _lastHeartbeat = DateTime.Now;
    }

    public void Authenticate(S2SCallback callback)
    {
        Debug.Log("[S2S] Authenticating");
        _state = State.Authenticating;
        string jsonAuthString = "{\"service\":\"authenticationV2\",\"operation\":\"AUTHENTICATE\",\"data\":{\"appId\":\"" + AppId + "\",\"serverName\":\"" + ServerName + "\",\"serverSecret\":\"" + ServerSecret + "\"}}";
        _packetId = 0;
        QueueRequest(jsonAuthString, callback + OnAuthenticationCallback); //We need to call OnAuthenticate callback to refill the queue with requests waiting on an auth request, and handle heartbeat and sessionId data. 
    }

    public void SendHeartbeat(S2SCallback callback)
    {
        if (SessionId != null)
        {
            string jsonHeartbeatString = "{\"service\":\"heartbeat\",\"operation\":\"HEARTBEAT\"}";
            QueueRequest(jsonHeartbeatString, callback);
        }
    }

#if DOT_NET
    private string ReadResponseBody(HttpWebResponse response)
    {
        Stream receiveStream = response.GetResponseStream();                        // Get the stream associated with the response.
        StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);   // Pipes the stream to a higher level stream reader with the required encoding format. 
        return readStream.ReadToEnd();
    }
#endif

    private void LogString(string s)
    {
        if (LoggingEnabled)
        {
#if DOT_NET
            Console.WriteLine("\n#S2S " + s);
#endif
#if USE_WEB_REQUEST
            Debug.Log("\n#S2S " + s);
#endif
        }
    }

    public void RunCallbacks()
    {
        if (activeRequest.request == null) //if there is no active request, make the first in the queue the active request.
        {
            if (_requestQueue.Count != 0) //make sure the queue isn't empty
            {
                activeRequest = (S2SRequest)_requestQueue[0];
            }
        }
        else //on an update, if we have an active request we need to process it. This is VITAL for WEB_REQUEST becasue it handles requests differently than DOT_NET
        {

#if DOT_NET
            HttpWebResponse csharpResponse = null;

            try
            {
                LogString("Sending Request: " + activeRequest.requestData);
                csharpResponse = (HttpWebResponse)activeRequest.request.GetResponse();
            }
            catch (Exception e)
            {
                LogString("S2S Failed: " + e.ToString());
                activeRequest.request.Abort();
                activeRequest.request = null;
                _requestQueue.RemoveAt(0);
                return;
            }
#endif
#if USE_WEB_REQUEST
            string unityResponse = null;
            if(activeRequest.request.downloadHandler.isDone)
            {
                unityResponse = activeRequest.request.downloadHandler.text;
            }
            if(!string.IsNullOrEmpty(activeRequest.request.error))
            {
                LogString("S2S Failed: " + activeRequest.request.error);
                activeRequest.callback(activeRequest.request.error);
                activeRequest.request.Abort();
                activeRequest.request = null;
                _requestQueue.RemoveAt(0);
            }
#endif

#if DOT_NET
            if (csharpResponse != null)
            {
                //get the response body
                string responseString = ReadResponseBody(csharpResponse);
#endif
#if USE_WEB_REQUEST
            if (unityResponse != null)
            {
            //get the response body
            string responseString = unityResponse;
#endif
                Dictionary<string, object> responseBody = (Dictionary<string, object>)JsonReader.Deserialize(responseString);

                if (responseBody.ContainsKey("messageResponses"))
                {
                    //extract the map array
                    Dictionary<string, object>[] messageArray = (Dictionary<string, object>[])responseBody["messageResponses"];
                    //extract the map from the map array
                    Dictionary<string, object> messageResponses = (Dictionary<string, object>)messageArray.GetValue(0);
                    if ((int)messageResponses["status"] == 200) //success 200
                    {
                        LogString("S2S Response: " + responseString);

                        //callback
                        if (activeRequest.callback != null)
                        {
                            activeRequest.callback(JsonWriter.Serialize((Dictionary<string, object>)messageResponses));
                        }

                        //remove the request finished request form the queue
                        _requestQueue.RemoveAt(0);
                    }
                    else //failed
                    {
                        //check if its a session expiry
                        if (responseBody.ContainsKey("reason_code"))
                        {
                            if ((int)responseBody["reason_code"] == SERVER_SESSION_EXPIRED)
                            {
                                LogString("S2S session expired");
                                activeRequest.request.Abort();
                                Disconnect();
                                return;
                            }
                        }

                        LogString("S2S Failed: " + responseString);

                        //callback
                        if (activeRequest.callback != null)
                        {
                            activeRequest.callback(JsonWriter.Serialize((Dictionary<string, object>)messageResponses));
                        }

                        activeRequest.request.Abort();

                        //remove the finished request from the queue
                        _requestQueue.RemoveAt(0);
                    }
                }
                activeRequest.request = null; //reset the active request so that it can move onto the next request. 
            }
        }
        //do a heartbeat if necessary.
        if (_state == State.Authenticated)
        {
            if (DateTime.Now.Subtract(_lastHeartbeat) >= _heartbeatTimer)
            {
                SendHeartbeat(OnHeartbeatCallback);
                ResetHeartbeat();
            }
        }
    }

    /**
    * Terminate current session from server.
    * (New Session will automatically be created on next request)
    */
    public void Disconnect()
    {
        _state = State.Disconnected;
        SessionId = null;
        _packetId = 0;
    }

    public void OnAuthenticationCallback(string responseString)
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

        if (response != null)
        {
            ////check if its a failure
            if (!response.ContainsKey("reason_code"))
            {
                Dictionary<string, object> data = (Dictionary<string, object>)response["data"];
                if (data.ContainsKey("sessionId") && data.ContainsKey("heartbeatSeconds"))
                {
                    SessionId = (string)data["sessionId"];
                    if (data.ContainsKey("heartbeatSeconds"))
                    {
                        _heartbeatSeconds = (int)data["heartbeatSeconds"]; // get the heartbeat seconds from braincloud.
                    }

                    ResetHeartbeat();
                    _state = State.Authenticated;

                    for (int i = 0; i < _waitingForAuthRequestQueue.Count; i++)
                    {
                        S2SRequest req = (S2SRequest)_waitingForAuthRequestQueue[i];
                        Request(req.requestData, req.callback);
                    }
                }
            }
            //clear in case a reauthentication is needed.
            _waitingForAuthRequestQueue.Clear();
        }
    }

    public void OnHeartbeatCallback(string responseString)
    {
        Dictionary<string, object> response = (Dictionary<string, object>)JsonReader.Deserialize(responseString);
        if (response != null)
        {
            if (response.ContainsKey("status"))
            {
                if ((int)response["status"] == 200)
                {
                    return;
                }
            }
        }
        Disconnect();
    }
}
