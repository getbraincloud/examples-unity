using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Fusion;
using UnityEngine;

public class LatencyCode : MonoBehaviour {
    private void Awake() {

        var instance = FindObjectOfType<LatencyCode>();
        if ( instance != null )
            Destroy(instance.gameObject);

        DontDestroyOnLoad(gameObject);
    }

    private void OnGUI() {
        var runner = FindObjectOfType<NetworkRunner>();
        if ( runner == null )
            return;

        var lobbyPlayer = RoomPlayer.Local;
        if ( lobbyPlayer == null ) return;
        
        var latency = runner.GetPlayerRtt(lobbyPlayer.Object.InputAuthority);
        GUILayout.Label($"Latency: {latency}", new GUIStyle() {
            fontSize = 55
        });
    }
}
