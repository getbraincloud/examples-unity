using System;
using Fusion;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
	public static event Action<GameManager> OnLobbyDetailsUpdated;

	[SerializeField, Layer] private int groundLayer;
	public static int GroundLayer => Instance.groundLayer;
	[SerializeField, Layer] private int kartLayer;
	public static int KartLayer => Instance.kartLayer;


	public new Camera camera;
	private ICameraController cameraController;

	public GameType GameType => ResourceManager.Instance.gameTypes[BCManager.LobbyManager.GameTypeId];

	public static Track CurrentTrack { get; private set; }
	public static bool IsPlaying => CurrentTrack != null;

	public static GameManager Instance { get; private set; }

	public string TrackName => ResourceManager.Instance.tracks[TrackId].trackName;
	public string ModeName => ResourceManager.Instance.gameTypes[BCManager.LobbyManager.GameTypeId].modeName;

	[Networked] public NetworkString<_32> LobbyName { get; set; }
	[Networked] public int TrackId { get; set; }
	//[Networked] public int GameTypeId { get; set; }
	[Networked] public int MaxUsers { get; set; }

	private static void OnLobbyDetailsChangedCallback(GameManager changed)
	{
		OnLobbyDetailsUpdated?.Invoke(changed);
	}
	
	private ChangeDetector _changeDetector;

	private void Awake()
	{
		if (Instance)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	public override void Spawned()
	{
		base.Spawned();
		
		_changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

		if (Object.HasStateAuthority)
		{
			LobbyName = BCManager.LobbyManager.LobbyId;
			TrackId = BCManager.LobbyManager.TrackId;
			//GameTypeId = BCManager.LobbyManager.GameTypeId;
			MaxUsers = ServerInfo.MaxUsers;
		}
	}
	
	public override void Render()
	{
		foreach (var change in _changeDetector.DetectChanges(this))
		{
			switch (change)
			{
				case nameof(LobbyName):
				case nameof(TrackId):
				//case nameof(GameTypeId):
				case nameof(MaxUsers):
					OnLobbyDetailsChangedCallback(this);
					break;
			}
		}
	}
	
	private void LateUpdate()
	{
		// this shouldn't really be an interface due to how Unity handle's interface lifecycles (null checks dont work).
		if (cameraController == null) return;
		if (cameraController.Equals(null))
		{
			Debug.LogWarning("Phantom object detected");
			cameraController = null;
			return;
		}

		if (cameraController.ControlCamera(camera) == false)
			cameraController = null;
	}
	
	public static void GetCameraControl(ICameraController controller)
	{
		Instance.cameraController = controller;
	}

	public static bool IsCameraControlled => Instance.cameraController != null;

	public static void SetTrack(Track track)
	{
		CurrentTrack = track;
	}
}