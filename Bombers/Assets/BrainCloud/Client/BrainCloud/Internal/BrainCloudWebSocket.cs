using System;
using UnityEngine;
#if DOT_NET
using System.Net.WebSockets;
#elif UNITY_WEBGL
using AOT;
using System.Collections.Generic;
#elif !UNITY_WEBGL
using WebSocketSharp;
#endif

public class BrainCloudWebSocket
{
#if DOT_NET
#elif UNITY_WEBGL 
	private NativeWebSocket NativeWebSocket;   
    private static Dictionary<int, BrainCloudWebSocket> webSocketInstances =
        new Dictionary<int, BrainCloudWebSocket>();
#elif !UNITY_WEBGL
    private WebSocket WebSocket;
#endif

    public BrainCloudWebSocket(string url)
    {
#if DOT_NET
#elif UNITY_WEBGL 
	    
	    Debug.Log("BrainCloudWebSocket");
	    
		NativeWebSocket = new NativeWebSocket(url);
		NativeWebSocket.SetOnOpen(NativeSocket_OnOpen);
		NativeWebSocket.SetOnMessage(NativeSocket_OnMessage);
		NativeWebSocket.SetOnError(NativeSocket_OnError);
		NativeWebSocket.SetOnClose(NativeSocket_OnClose);
		webSocketInstances.Add(NativeWebSocket.Id, this);
#elif !UNITY_WEBGL
        WebSocket = new WebSocket(url);
        WebSocket.ConnectAsync();
        WebSocket.AcceptAsync();
        WebSocket.OnOpen += WebSocket_OnOpen;
        WebSocket.OnMessage += WebSocket_OnMessage;
        WebSocket.OnError += WebSocket_OnError;
        WebSocket.OnClose += WebSocket_OnClose;
#endif
    }

    public void Close()
    {
#if DOT_NET
#elif UNITY_WEBGL
        if (NativeWebSocket == null)
			return;
        webSocketInstances.Remove(NativeWebSocket.Id);
		NativeWebSocket.CloseAsync();
		NativeWebSocket = null;
#elif !UNITY_WEBGL
        if (WebSocket == null)
            return;
        WebSocket.CloseAsync();
        WebSocket.OnOpen -= WebSocket_OnOpen;
        WebSocket.OnMessage -= WebSocket_OnMessage;
        WebSocket.OnError -= WebSocket_OnError;
        WebSocket.OnClose -= WebSocket_OnClose;
        WebSocket = null;
#endif
    }

#if DOT_NET
#elif UNITY_WEBGL 
    [MonoPInvokeCallback(typeof(Action<int>))]
	public static void NativeSocket_OnOpen(int id) {
	
		Debug.Log("BrainCloudWebSocket NativeSocket_OnOpen");
		
		if (webSocketInstances.ContainsKey(id) && webSocketInstances[id].OnOpen != null)
			webSocketInstances[id].OnOpen(webSocketInstances[id]);
	}

	[MonoPInvokeCallback(typeof(Action<int>))]
	public static void NativeSocket_OnMessage(int id) {
    
		Debug.Log("BrainCloudWebSocket NativeSocket_OnMessage");
		
        if (webSocketInstances.ContainsKey(id))
        {
	    	byte[] data = webSocketInstances[id].NativeWebSocket.Receive();
	    	if (webSocketInstances[id].OnMessage != null)
	    		webSocketInstances[id].OnMessage(webSocketInstances[id], data);
        }
	}

	[MonoPInvokeCallback(typeof(Action<int>))]
	public static void NativeSocket_OnError(int id) {
		
		Debug.Log("BrainCloudWebSocket NativeSocket_OnError");
		
		if (webSocketInstances.ContainsKey(id) && webSocketInstances[id].OnError != null)
			webSocketInstances[id].OnError(webSocketInstances[id], webSocketInstances[id].NativeWebSocket.Error);
	}

	[MonoPInvokeCallback(typeof(Action<int, int>))]
	public static void NativeSocket_OnClose(int code, int id) {
    
		Debug.Log("BrainCloudWebSocket NativeSocket_OnClose");
		
		CloseError errorInfo = CloseError.Get(code);
		if (webSocketInstances.ContainsKey(id) && webSocketInstances[id].OnClose != null)
			webSocketInstances[id].OnClose(webSocketInstances[id], errorInfo.Code, errorInfo.Message);
	}
#elif !UNITY_WEBGL
    private void WebSocket_OnOpen(object sender, EventArgs e)
    {
        if (OnOpen != null)
            OnOpen(this);
    }

    private void WebSocket_OnMessage(object sender, MessageEventArgs e)
    {
        if (OnMessage != null)
            OnMessage(this, e.RawData);
    }

    private void WebSocket_OnError(object sender, ErrorEventArgs e)
    {
        if (OnError != null)
            OnError(this, e.Message);
    }

    private void WebSocket_OnClose(object sender, CloseEventArgs e)
    {
        if (OnClose != null)
            OnClose(this, e.Code, e.Reason);
    }
#endif

    public void SendAsync(byte[] packet)
    {
#if DOT_NET
#elif UNITY_WEBGL 
    	NativeWebSocket.SendAsync(packet);
#elif !UNITY_WEBGL	    
        WebSocket.SendAsync(packet, null);
#endif
    }

    public delegate void OnOpenHandler(BrainCloudWebSocket accepted);
    public delegate void OnMessageHandler(BrainCloudWebSocket sender, byte[] data);
    public delegate void OnErrorHandler(BrainCloudWebSocket sender, string message);
    public delegate void OnCloseHandler(BrainCloudWebSocket sender, int code, string reason);

    public event OnOpenHandler OnOpen;
    public event OnMessageHandler OnMessage;
    public event OnErrorHandler OnError;
    public event OnCloseHandler OnClose;
}