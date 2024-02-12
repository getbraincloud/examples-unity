using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
	[SerializeField] private Button serverButton;
	[SerializeField] private Button hostButton;
	[SerializeField] private Button clientButton;
	[SerializeField] private TMP_Text logText;
 
	private void Awake()
	{
		serverButton.onClick.AddListener((() =>
		{
			NetworkManager.Singleton.StartServer();
		}));
		
		hostButton.onClick.AddListener((() =>
		{
			NetworkManager.Singleton.StartHost();
		}));
		
		clientButton.onClick.AddListener((() =>
		{
			NetworkManager.Singleton.StartClient();
		}));

		Application.logMessageReceived += HandleLog;
	}
	
	private void HandleLog(string logstring, string stackTrace, LogType type)
	{
		logText.text += logstring + "<br>";
	}
}
