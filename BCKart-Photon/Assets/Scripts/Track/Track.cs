using Fusion;
using UnityEngine;

public class Track : NetworkBehaviour, ICameraController
{
	public static Track Current { get; private set; }

	[Networked] public TickTimer StartRaceTimer { get; set; }

	public CameraTrack[] introTracks;
	public Checkpoint[] checkpoints;
	public Transform[] spawnpoints;
	public FinishLine finishLine;
	public GameObject itemContainer;
	public GameObject coinContainer;

	public TrackDefinition definition;
	public TrackStartSequence sequence;

	public string music = "";
	public float introSpeed = 0.5f;

	private int _currentIntroTrack;
	private float _introIntervalProgress;

	private void Awake()
	{
		Current = this;
		InitCheckpoints();

		if (GameManager.Instance.GameType.hasPickups == false)
		{
			itemContainer.SetActive(false);
			coinContainer.SetActive(false);
		}

		// Initialize cutscene
		AudioManager.StopMusic();

		GameManager.SetTrack(this);
		GameManager.Instance.camera = Camera.main;
		StartIntro();
	}

	public override void Spawned()
	{
		base.Spawned();

		if (RoomPlayer.Local.IsLeader)
		{
			StartRaceTimer = TickTimer.CreateFromSeconds(Runner, sequence.duration + 4f);
		}

		sequence.StartSequence();
	}

	private void OnDestroy()
	{
		GameManager.SetTrack(null);
	}

	public void SpawnPlayer(NetworkRunner runner, RoomPlayer player)
	{
		var index = RoomPlayer.Players.IndexOf(player);
		var point = spawnpoints[index];

		var prefabId = player.KartId;
		var prefab = ResourceManager.Instance.kartDefinitions[prefabId].prefab;

		// Spawn player
		var entity = runner.Spawn(
			prefab,
			point.position,
			point.rotation,
			player.Object.InputAuthority
		);

		entity.Controller.RoomUser = player;
		player.GameState = RoomPlayer.EGameState.GameCutscene;
		player.Kart = entity.Controller;

		Debug.Log($"Spawning kart for {player.Username} as {entity.name}");
		entity.transform.name = $"Kart ({player.Username})";
	}

	private void InitCheckpoints()
	{
		for (int i = 0; i < checkpoints.Length; i++)
		{
			checkpoints[i].index = i;
		}
	}

	public bool ControlCamera(Camera cam)
	{
		cam.transform.position = Vector3.Lerp(
			introTracks[_currentIntroTrack].startPoint.position,
			introTracks[_currentIntroTrack].endPoint.position,
			_introIntervalProgress);

		cam.transform.rotation = Quaternion.Slerp(
			introTracks[_currentIntroTrack].startPoint.rotation,
			introTracks[_currentIntroTrack].endPoint.rotation,
			_introIntervalProgress);

		_introIntervalProgress += Time.deltaTime * introSpeed;
		if (_introIntervalProgress > 1)
		{
			_introIntervalProgress -= 1;
			_currentIntroTrack++;
			if (_currentIntroTrack == introTracks.Length)
			{
				_currentIntroTrack = 0;
				_introIntervalProgress = 0;
				return false;
			}
		}

		return true;
	}

	public void StartIntro()
	{
		_currentIntroTrack = 0;
		_introIntervalProgress = 0;
		AudioManager.PlayMusic("intro");
		GameManager.GetCameraControl(this);
	}
}