using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Managers;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour, IDisabledUI
{
	public GameObject textPrefab;
	public Transform parent;
	public Button readyUp;
	public Button customizeButton;
	public Text trackNameText;
	public Text modeNameText;
	public Text lobbyNameText;
	public Dropdown trackNameDropdown;
	public Dropdown gameTypeDropdown;
	public Image trackIconImage;

	private static readonly Dictionary<LobbyMember, LobbyItemUI> ListItems = new Dictionary<LobbyMember, LobbyItemUI>();
	private static bool IsSubscribed;
	private LobbyState previousLobbyState = LobbyState.NotInLobby;

	private void Awake()
	{
		trackNameDropdown.onValueChanged.AddListener(x =>
		{
			bool wasUpdated = x != BCManager.LobbyManager.TrackId;
			if (wasUpdated)
            {   
				BCManager.LobbyManager.TrackId = x;
				// send a lobby event about the track change
				SendLobbySignal();
            }
		});
		gameTypeDropdown.onValueChanged.AddListener(x =>
		{
			bool wasUpdated = x != BCManager.LobbyManager.GameTypeId;
			if (wasUpdated)
			{
				BCManager.LobbyManager.GameTypeId = x;
				// send a lobby event about the GameTypeId
				SendLobbySignal();
			}
		});

		UpdateDetails(BCManager.LobbyManager);
		BCManager.LobbyManager.OnLobbyDetailsUpdated += UpdateDetails;
	}

	void UpdateDetails(BCLobbyManager manager)
	{
		if (manager.Local == null)
			return;
		var isLeader = manager.Local.isHost;
		trackNameDropdown.interactable = isLeader;
		gameTypeDropdown.interactable = isLeader;
		customizeButton.interactable = !manager.Local.isReady;

		lobbyNameText.text = "LobbyId: " + manager.LobbyId;
		trackNameText.text = manager.TrackName;
		modeNameText.text = manager.ModeName;

		var tracks = ResourceManager.Instance.tracks;
		var trackOptions = tracks.Select(x => x.trackName).ToList();

		var gameTypes = ResourceManager.Instance.gameTypes;
		var gameTypeOptions = gameTypes.Select(x => x.modeName).ToList();

		trackNameDropdown.ClearOptions();
		trackNameDropdown.AddOptions(trackOptions);
		trackNameDropdown.value = manager.TrackId;

		trackIconImage.sprite = ResourceManager.Instance.tracks[manager.TrackId].trackIcon;

		gameTypeDropdown.ClearOptions();
		gameTypeDropdown.AddOptions(gameTypeOptions);
		gameTypeDropdown.value = manager.GameTypeId;

		// we changed states between messages
		if (previousLobbyState != manager.LobbyState)
		{
			// we started!
			if (manager.LobbyState == LobbyState.Starting)
			{
				BCManager.LobbyManager.JoinOrCreateLobby();
			}
		}
		previousLobbyState = manager.LobbyState;

		// this will check if we should launch into the game
		EnsureAllPlayersReady();
	}

	public void OnDestruction()
	{
		
	}

	public void Setup()
	{
		if (IsSubscribed) return;
		BCManager.LobbyManager.PlayerJoined += AddPlayer;
		BCManager.LobbyManager.PlayerLeft += RemovePlayer;
		readyUp.onClick.AddListener(ReadyUpListener);

		IsSubscribed = true;
	}

	private void OnDestroy()
	{
		if (!IsSubscribed) return;
		BCManager.LobbyManager.PlayerJoined -= AddPlayer;
		BCManager.LobbyManager.PlayerLeft -= RemovePlayer;
		
		readyUp.onClick.RemoveListener(ReadyUpListener);

		IsSubscribed = false;
	}
	
	private void AddPlayer(LobbyMember player)
	{
		if (ListItems.ContainsKey(player))
		{
			var toRemove = ListItems[player];
			Destroy(toRemove.gameObject);

			ListItems.Remove(player);
		}

		var obj = Instantiate(textPrefab, parent).GetComponent<LobbyItemUI>();
		obj.SetPlayer(player);

		ListItems.Add(player, obj);
	}

	private void RemovePlayer(LobbyMember player)
	{
		if (!ListItems.ContainsKey(player))
			return;

		var obj = ListItems[player];
		if (obj != null)
		{
			Destroy(obj.gameObject);
			ListItems.Remove(player);
		}
	}

	private void SendLobbySignal()
	{
		// I'd usually put this into the lobby config
		// but since it can change live, we are acting on 
		// signals within the lobby of players
		// update the state of things
		Dictionary<string, object> signalData = new Dictionary<string, object>();

		signalData["TrackId"] = BCManager.LobbyManager.TrackId; // TrackId
		signalData["GameTypeId"] = BCManager.LobbyManager.GameTypeId;// GameTypeId

		BCManager.LobbyService.SendSignal(BCManager.LobbyManager.LobbyId,
				signalData,
			OnSendSignalSuccess, OnSendSignalError);

	}
	
	private void OnSendSignalSuccess(string jsonResponse, object cbObject)
    {
        Debug.Log("LobbyUI OnUpdateReadySuccess: " + jsonResponse);
    }

	private void OnSendSignalError(int status, int reasonCode, string jsonError, object cbObject)
	{
		Debug.LogError($"LobbyUI OnUpdateReadyError: {status}, {reasonCode}, {jsonError}");
	}

	private void ReadyUpListener()
	{
		var local = BCManager.LobbyManager.Local;

		Dictionary<string, object> extra = new Dictionary<string, object>();
		// extra will be the players kart information
		BCManager.Wrapper.LobbyService.UpdateReady(BCManager.LobbyManager.LobbyId, !local.isReady, extra,
								OnUpdateReadySuccess, OnUpdateReadyError);
	}
	private void OnUpdateReadySuccess(string jsonResponse, object cbObject)
    {
        Debug.Log("LobbyUI OnUpdateReadySuccess: " + jsonResponse);
    }

	private void OnUpdateReadyError(int status, int reasonCode, string jsonError, object cbObject)
	{
		Debug.LogError($"LobbyUI OnUpdateReadyError: {status}, {reasonCode}, {jsonError}");
	}

	private void EnsureAllPlayersReady()
	{
	    if (IsAllReady())
		{
			int scene = ResourceManager.Instance.tracks[BCManager.LobbyManager.TrackId].buildIndex;
	    	LevelManager.LoadTrack(scene);
	    }
	}

	private static bool IsAllReady() => BCManager.LobbyManager.LobbyMembers.Count > 0 &&
									BCManager.LobbyManager.LobbyMembers.All(player => player.Value.isConnected);
}