using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Server
{
    public string Host;
    public int WsPort = -1;
    public int TcpPort = -1;
    public int UdpPort = -1;
    public int i3dPort = -1;
    public string Passcode;
    public string LobbyId;
    
    public Server(Dictionary<string, object> serverJson)
    {
        var connectData = serverJson["connectData"] as Dictionary<string, object>;
        var ports = connectData["ports"] as Dictionary<string, object>;

        Host = connectData["address"] as string;
        if(ports.ContainsKey("ws"))
        {
            WsPort = (int)ports["ws"];
        }
        if (ports.ContainsKey("tcp"))
        {
            TcpPort = (int)ports["tcp"];
        }
        if(ports.ContainsKey("udp"))
        {
            UdpPort = (int)ports["udp"];
        }
        if(ports.ContainsKey("i3d"))
        {
            i3dPort = (int)ports["i3d"];
        }

        Passcode = serverJson["passcode"] as string;
        LobbyId = serverJson["lobbyId"] as string;
    }
    
    public Server(Dictionary<string, object> serverJson, bool noServerSelected)
    {
        var connectData = serverJson["connectInfo"] as Dictionary<string, object>;
        var ports = connectData["ports"] as Dictionary<string, object>;

        Host = connectData["address"] as string;
        if(ports.ContainsKey("ws"))
        {
            WsPort = (int)ports["ws"];
        }
        if (ports.ContainsKey("tcp"))
        {
            TcpPort = (int)ports["tcp"];
        }
        if(ports.ContainsKey("udp"))
        {
            UdpPort = (int)ports["udp"];
        }
        if(ports.ContainsKey("i3d"))
        {
            i3dPort = (int)ports["i3d"];
        }
        Passcode = serverJson["passcode"] as string;
        //LobbyId = serverJson["lobbyId"] as string;
    }
}
