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

	private void Awake()
	{
		trackNameDropdown.onValueChanged.AddListener(x =>
		{
			var gm = GameManager.Instance;
			if (gm != null) gm.TrackId = x;
		});
		gameTypeDropdown.onValueChanged.AddListener(x =>
		{
			var gm = GameManager.Instance;
			if (gm != null) gm.GameTypeId = x;
		});

		GameManager.OnLobbyDetailsUpdated += UpdateDetails;
		//UpdateDetails(null);
	}

	void UpdateDetails(GameManager manager)
	{
		lobbyNameText.text = "LobbyId: " + BCManager.LobbyManager.LobbyId; //"Room Code: " + manager.LobbyName;
		trackNameText.text = "Cavern Cove";// manager.TrackName;
		modeNameText.text = "Race"; //manager.ModeName;

		var tracks = ResourceManager.Instance.tracks;
		var trackOptions = tracks.Select(x => x.trackName).ToList();

		var gameTypes = ResourceManager.Instance.gameTypes;
		var gameTypeOptions = gameTypes.Select(x => x.modeName).ToList();

		trackNameDropdown.ClearOptions();
		trackNameDropdown.AddOptions(trackOptions);
		trackNameDropdown.value = 0;//manager.TrackId;

		trackIconImage.sprite = ResourceManager.Instance.tracks[0/*manager.TrackId*/].trackIcon;

		gameTypeDropdown.ClearOptions();
		gameTypeDropdown.AddOptions(gameTypeOptions);
		gameTypeDropdown.value = 0;/*manager.GameTypeId;*/
	}

	public void Setup()
	{
		if (IsSubscribed) return;
		BCManager.LobbyManager.PlayerJoined += AddPlayer;
		BCManager.LobbyManager.PlayerLeft += RemovePlayer;

		BCManager.LobbyManager.PlayerChanged += EnsureAllPlayersReady;
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
		
		UpdateDetails(GameManager.Instance);
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

	public void OnDestruction()
	{
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

	private void EnsureAllPlayersReady(LobbyMember lobbyPlayer)
	{
	    if (lobbyPlayer == BCManager.LobbyManager.Local)
	    {
	        var isLeader = lobbyPlayer.isHost;
	        trackNameDropdown.interactable = isLeader;
	        gameTypeDropdown.interactable = isLeader;
	        customizeButton.interactable = !lobbyPlayer.isReady;
	    }

	    if (IsAllReady())
		{
			// join or create correctly
			BCManager.LobbyManager.JoinOrCreateLobby();

			// send signal to lock it all down

			// start listening for when to try and connect to the track

	        // Start coroutine to load track after 2 seconds
	        StartCoroutine(LoadTrackWithDelay(10f));
	    }
	}

	private IEnumerator LoadTrackWithDelay(float delay)
	{
	    yield return new WaitForSeconds(delay);

	    int scene = ResourceManager.Instance.tracks[0].buildIndex; ///GameManager.Instance.TrackId
	    LevelManager.LoadTrack(scene);
	}

	private static bool IsAllReady() => BCManager.LobbyManager.LobbyMembers.Count > 0 &&
									BCManager.LobbyManager.LobbyMembers.All(player => player.Value.isReady);
}