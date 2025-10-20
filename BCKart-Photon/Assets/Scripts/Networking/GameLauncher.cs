using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
using Fusion.Sockets;
using FusionExamples.FusionHelpers;
using Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum ConnectionStatus
{
	Disconnected,
	Connecting,
	Failed,
	Connected
}

[RequireComponent(typeof(LevelManager))]
public class GameLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
	[SerializeField] private GameManager _gameManagerPrefab;
	[SerializeField] private RoomPlayer _roomPlayerPrefab;
	[SerializeField] private DisconnectUI _disconnectUI;

	public static ConnectionStatus ConnectionStatus = ConnectionStatus.Disconnected;

	private GameMode _gameMode;
	private NetworkRunner _runner;
	private FusionObjectPoolRoot _pool;
	private LevelManager _levelManager;

	private void Start()
	{
		//Physics.autoSimulation = false;
		Application.runInBackground = true;
		Application.targetFrameRate = Screen.currentResolution.refreshRate;
		QualitySettings.vSyncCount = 1;

		_levelManager = GetComponent<LevelManager>();

		DontDestroyOnLoad(gameObject);

		SceneManager.LoadScene(LevelManager.LOBBY_SCENE);
	}

	public void SetCreateLobby() => _gameMode = GameMode.Host;
	public void SetJoinLobby() => _gameMode = GameMode.Client;
	
	public void JoinOrCreateLobby()
	{
		SetConnectionStatus(ConnectionStatus.Connecting);

		if (_runner != null)
			LeaveSession();

		GameObject go = new GameObject("Session");
		DontDestroyOnLoad(go);

		_runner = go.AddComponent<NetworkRunner>();
		var sim3D = go.AddComponent<RunnerSimulatePhysics3D>();
		sim3D.ClientPhysicsSimulation = ClientPhysicsSimulation.SimulateAlways;

		_runner.ProvideInput = _gameMode != GameMode.Server;
		_runner.AddCallbacks(this);

		_pool = go.AddComponent<FusionObjectPoolRoot>();

		Debug.Log($"Created gameobject {go.name} - starting game");
		_runner.StartGame(new StartGameArgs
		{
			GameMode = _gameMode,
			SessionName = _gameMode == GameMode.Host ? ServerInfo.LobbyName : ClientInfo.LobbyName,
			ObjectProvider = _pool,
			SceneManager = _levelManager,
			PlayerCount = ServerInfo.MaxUsers,
			EnableClientSessionCreation = false
		});
	}

	private void SetConnectionStatus(ConnectionStatus status)
	{
		Debug.Log($"Setting connection status to {status}");

		ConnectionStatus = status;
		
		if (!Application.isPlaying) 
			return;

		if (status == ConnectionStatus.Disconnected || status == ConnectionStatus.Failed)
		{
			SceneManager.LoadScene(LevelManager.LOBBY_SCENE);
			UIScreen.BackToInitial();
		}
	}
	
	public void LeaveSession()
	{
		if (_runner != null)
			_runner.Shutdown();
		else
			SetConnectionStatus(ConnectionStatus.Disconnected);
	}
	
	public void OnConnectedToServer(NetworkRunner runner)
	{
		Debug.Log("Connected to server");
		SetConnectionStatus(ConnectionStatus.Connected);
	}

	public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
	{
		Debug.Log("Disconnected from server");
		LeaveSession();
		SetConnectionStatus(ConnectionStatus.Disconnected);
	}
	
	public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
	{
		if (runner.TryGetSceneInfo(out var scene) && scene.SceneCount > 0)
		{
			Debug.LogWarning($"Refused connection requested by {request.RemoteAddress}");
			request.Refuse();
		}
		else
			request.Accept();
	}
	public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
	{
		Debug.Log($"Connect failed {reason}");
		LeaveSession();
		SetConnectionStatus(ConnectionStatus.Failed);
		(string status, string message) = ConnectFailedReasonToHuman(reason);
		_disconnectUI.ShowMessage(status,message);
	}

	public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

	public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)	{ }

	public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
	{
		Debug.Log($"Player {player} Joined!");
		if (runner.IsServer)
		{
			if(_gameMode==GameMode.Host)
				runner.Spawn(_gameManagerPrefab, Vector3.zero, Quaternion.identity);
			var roomPlayer = runner.Spawn(_roomPlayerPrefab, Vector3.zero, Quaternion.identity, player);
			roomPlayer.GameState = RoomPlayer.EGameState.Lobby;
		}
		SetConnectionStatus(ConnectionStatus.Connected);
	}

	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
	{
		Debug.Log($"{player.PlayerId} disconnected.");

		RoomPlayer.RemovePlayer(runner, player);

		SetConnectionStatus(ConnectionStatus);
	}
	public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
	{
		Debug.Log($"OnShutdown {shutdownReason}");
		SetConnectionStatus(ConnectionStatus.Disconnected);

		(string status, string message) = ShutdownReasonToHuman(shutdownReason);
		_disconnectUI.ShowMessage( status, message);

		RoomPlayer.Players.Clear();

		if(_runner)
			Destroy(_runner.gameObject);
		
		// Reset the object pools
		_pool.ClearPools();
		_pool = null;

		_runner = null;
	}
	public void OnInput(NetworkRunner runner, NetworkInput input) { }
	public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
	public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
	public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
	public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
	public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
	public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
	public void OnSceneLoadDone(NetworkRunner runner) { }
	public void OnSceneLoadStart(NetworkRunner runner) { }

	private static (string, string) ShutdownReasonToHuman(ShutdownReason reason)
	{
		switch (reason)
		{
			case ShutdownReason.Ok:
				return (null, null);
			case ShutdownReason.Error:
				return ("Error", "Shutdown was caused by some internal error");
			case ShutdownReason.IncompatibleConfiguration:
				return ("Incompatible Config", "Mismatching type between client Server Mode and Shared Mode");
			case ShutdownReason.ServerInRoom:
				return ("Room name in use", "There's a room with that name! Please try a different name or wait a while.");
			case ShutdownReason.DisconnectedByPluginLogic:
				return ("Disconnected By Plugin Logic", "You were kicked, the room may have been closed");
			case ShutdownReason.GameClosed:
				return ("Game Closed", "The session cannot be joined, the game is closed");
			case ShutdownReason.GameNotFound:
				return ("Game Not Found", "This room does not exist");
			case ShutdownReason.MaxCcuReached:
				return ("Max Players", "The Max CCU has been reached, please try again later");
			case ShutdownReason.InvalidRegion:
				return ("Invalid Region", "The currently selected region is invalid");
			case ShutdownReason.GameIdAlreadyExists:
				return ("ID already exists", "A room with this name has already been created");
			case ShutdownReason.GameIsFull:
				return ("Game is full", "This lobby is full!");
			case ShutdownReason.InvalidAuthentication:
				return ("Invalid Authentication", "The Authentication values are invalid");
			case ShutdownReason.CustomAuthenticationFailed:
				return ("Authentication Failed", "Custom authentication has failed");
			case ShutdownReason.AuthenticationTicketExpired:
				return ("Authentication Expired", "The authentication ticket has expired");
			case ShutdownReason.PhotonCloudTimeout:
				return ("Cloud Timeout", "Connection with the Photon Cloud has timed out");
			default:
				Debug.LogWarning($"Unknown ShutdownReason {reason}");
				return ("Unknown Shutdown Reason", $"{(int)reason}");
		}
	}

	private static (string,string) ConnectFailedReasonToHuman(NetConnectFailedReason reason)
	{
		switch (reason)
		{
			case NetConnectFailedReason.Timeout:
				return ("Timed Out", "");
			case NetConnectFailedReason.ServerRefused:
				return ("Connection Refused", "The lobby may be currently in-game");
			case NetConnectFailedReason.ServerFull:
				return ("Server Full", "");
			default:
				Debug.LogWarning($"Unknown NetConnectFailedReason {reason}");
				return ("Unknown Connection Failure", $"{(int)reason}");
		}
	}
}