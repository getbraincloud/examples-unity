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
	
	public Button leave;
	public Button readyUp;
	public Button customizeButton;
	public Text trackNameText;
	public Text modeNameText;
	public Text lobbyNameText;
	public Text lobbyMemberDisplayCountText;
	public Text hostDropDisplayText;
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
	}

	void UpdateDetails(BCLobbyManager manager)
	{
		if (manager.Local == null)
			return;

		bool isHost = manager.Local.isHost;
		bool lobbyStateStarting = manager.LobbyState == LobbyState.Starting;

		trackNameDropdown.interactable = isHost && !lobbyStateStarting;
		gameTypeDropdown.interactable = isHost && !lobbyStateStarting;
		customizeButton.interactable = !manager.Local.isReady && !lobbyStateStarting;
		leave.interactable = true; //lobbyStateStarting;
		readyUp.interactable = !lobbyStateStarting;

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
				// the leader is going to start this
				// .the clients are going to wait for the host leader 
				// .to be connected before connecting to it
                Debug.Log($"CONNECTED: , Host={isHost}");
				if (isHost) BCManager.LobbyManager.JoinOrCreateLobby();
				
				ShowLobbyDisplayMessage("Starting");
			}
		}
		previousLobbyState = manager.LobbyState;

		lobbyMemberDisplayCountText.text = manager.LobbyMembers.Count + "/" + manager.GetLobbyInt(BCLobbyManager.ACTIVE_LOBBY_TYPE);

		// this will check if we should launch into the game
		EnsureAllPlayersReady();
	}

	public void OnDestruction()
	{

	}

	void OnEnable()
	{
	    // Add all members from the lobby
	    foreach (var kvp in BCManager.LobbyManager.LobbyMembers)
	    {
	        AddPlayer(kvp.Value);
	    }

		Setup();
		
		UpdateDetails(BCManager.LobbyManager);
		
	}

	void OnDisable()
	{
	    // Create a local copy of the keys to safely iterate
	    var members = new List<LobbyMember>(ListItems.Keys);

	    foreach (var member in members)
	    {
	        RemovePlayer(member);
	    }

	    // Clear the dictionary to remove leftover references
	    ListItems.Clear();

		OnCleanup();
		hostDropDisplayText.gameObject.SetActive(false);
	}

	public void Setup()
	{
		if (IsSubscribed) return;
		BCManager.LobbyManager.PlayerJoined += AddPlayer;
		BCManager.LobbyManager.PlayerLeft += RemovePlayer;
		readyUp.onClick.AddListener(ReadyUpListener);
		BCManager.LobbyManager.OnLobbyDetailsUpdated += UpdateDetails;

		IsSubscribed = true;
	}

    private float visibleTime = 5f;
    private float fadeDuration = 1.5f; 

    public void ShowLobbyDisplayMessage(string message)
    {
        hostDropDisplayText.text = message;
        StartCoroutine(ShowAndFade());
    }

	private IEnumerator ShowAndFade()
	{
		hostDropDisplayText.gameObject.SetActive(true);

		// reset alpha to full
		Color color = hostDropDisplayText.color;
		color.a = 1f;
		hostDropDisplayText.color = color;

		// wait visible time
		yield return new WaitForSeconds(visibleTime);

		float t = 0f;
		while (t < fadeDuration)
		{
			t += Time.deltaTime;
			float alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
			color.a = alpha;
			hostDropDisplayText.color = color;
			yield return null;
		}

		hostDropDisplayText.gameObject.SetActive(false);
	}
	
	
	private void OnCleanup()
    {
		if (!IsSubscribed) return;
		BCManager.LobbyManager.PlayerJoined -= AddPlayer;
		BCManager.LobbyManager.PlayerLeft -= RemovePlayer;
		
		readyUp.onClick.RemoveListener(ReadyUpListener);
		BCManager.LobbyManager.OnLobbyDetailsUpdated -= UpdateDetails;

		IsSubscribed = false;
    }

	private void OnDestroy()
    {
		OnCleanup();
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