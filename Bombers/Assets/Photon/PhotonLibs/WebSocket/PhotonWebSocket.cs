#if UNITY_WEBGL || UNITY_XBOXONE || WEBSOCKET

using System;
using System.Text;
using UnityEngine;
#if UNITY_WEBGL
using System.Runtime.InteropServices;
#else
using System.Collections.Generic;
using System.Security.Authentication;
#endif


public class PhotonWebSocket
{
    private Uri mUrl;
    /// <summary>Photon uses this to agree on a serialization protocol. Either: GpBinaryV16 or GpBinaryV18. Based on enum SerializationProtocol.</summary>
    private string protocols = "GpBinaryV16";

    public PhotonWebSocket(Uri url, string protocols = null)
    {
        Debug.Log("Photon WebSocket");
        
        this.mUrl = url;
        if (protocols != null)
        {
            this.protocols = protocols;
        }

        string protocol = mUrl.Scheme;
        if (!protocol.Equals("ws") && !protocol.Equals("wss"))
            throw new ArgumentException("Unsupported protocol: " + protocol);
    }

    public void SendString(string str)
    {
        Send(Encoding.UTF8.GetBytes (str));
    }

    public string RecvString()
    {
        byte[] retval = Recv();
        if (retval == null)
            return null;
        return Encoding.UTF8.GetString (retval);
    }

#if UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern int PhotonSocketCreate (string url, string protocols);

    [DllImport("__Internal")]
    private static extern int PhotonSocketState (int socketInstance);

    [DllImport("__Internal")]
    private static extern void PhotonSocketSend (int socketInstance, byte[] ptr, int length);

    [DllImport("__Internal")]
    private static extern void PhotonSocketRecv (int socketInstance, byte[] ptr, int length);

    [DllImport("__Internal")]
    private static extern int PhotonSocketRecvLength (int socketInstance);

    [DllImport("__Internal")]
    private static extern void PhotonSocketClose (int socketInstance);

    [DllImport("__Internal")]
    private static extern int PhotonSocketError (int socketInstance, byte[] ptr, int length);

    int m_NativeRef = 0;

    public void Send(byte[] buffer)
    {
        PhotonSocketSend (m_NativeRef, buffer, buffer.Length);
    }

    public byte[] Recv()
    {
        int length = PhotonSocketRecvLength (m_NativeRef);
        if (length == 0)
            return null;
        byte[] buffer = new byte[length];
        PhotonSocketRecv (m_NativeRef, buffer, length);
        return buffer;
    }

    public void Connect()
    {
        m_NativeRef = PhotonSocketCreate (mUrl.ToString(), this.protocols);

        //while (SocketState(m_NativeRef) == 0)
        //    yield return 0;
    }

    public void Close()
    {
        PhotonSocketClose(m_NativeRef);
    }

    public bool Connected
    {
        get { return PhotonSocketState(m_NativeRef) != 0; }
    }

    public string Error
    {
        get {
            const int bufsize = 1024;
            byte[] buffer = new byte[bufsize];
            int result = PhotonSocketError (m_NativeRef, buffer, bufsize);

            if (result == 0)
                return null;

            return Encoding.UTF8.GetString (buffer);
        }
    }
#else
    WebSocketSharp.WebSocket m_Socket;
    Queue<byte[]> m_Messages = new Queue<byte[]>();
    bool m_IsConnected = false;
    string m_Error = null;

    public void Connect()
    {
        m_Socket = new WebSocketSharp.WebSocket(mUrl.ToString(), new string[] { this.protocols });
        m_Socket.SslConfiguration.EnabledSslProtocols = m_Socket.SslConfiguration.EnabledSslProtocols | (SslProtocols)(3072| 768);
        m_Socket.OnMessage += (sender, e) => m_Messages.Enqueue(e.RawData);
        m_Socket.OnOpen += (sender, e) => m_IsConnected = true;
        m_Socket.OnError += (sender, e) => m_Error = e.Message + (e.Exception == null ? "" : " / " + e.Exception);
        m_Socket.ConnectAsync();
    }

    public bool Connected { get { return m_IsConnected; } }// added by TS


    public void Send(byte[] buffer)
    {
        m_Socket.Send(buffer);
    }

    public byte[] Recv()
    {
        if (m_Messages.Count == 0)
            return null;
        return m_Messages.Dequeue();
    }

    public void Close()
    {
        m_Socket.Close();
    }

    public string Error
    {
        get
        {
            return m_Error;
        }
    }
#endif
}
#endif