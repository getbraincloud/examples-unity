using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class CreateGameUI : MonoBehaviour
{
	public InputField lobbyName;
	public Dropdown track;
	public Dropdown gameMode;
	public Slider playerCountSlider;
	public Image trackImage;
	public Text playerCountSliderText;
	public Image playerCountIcon;
	public Button confirmButton;
    
	//resources
	public Sprite padlockSprite, publicLobbyIcon;

	private void Start()
	{

		playerCountSlider.SetValueWithoutNotify(8);
		SetPlayerCount();

		track.ClearOptions();
		track.AddOptions(ResourceManager.Instance.tracks.Select(x => x.trackName).ToList());
		track.onValueChanged.AddListener(SetTrack);
		SetTrack(0);

		gameMode.ClearOptions();
		gameMode.AddOptions(ResourceManager.Instance.gameTypes.Select(x => x.modeName).ToList());
		gameMode.onValueChanged.AddListener(SetGameType);
		SetGameType(0);

		playerCountSlider.wholeNumbers = true;
		playerCountSlider.minValue = 1;
		playerCountSlider.maxValue = 8;
		playerCountSlider.value = 2;
		playerCountSlider.onValueChanged.AddListener(x => ServerInfo.MaxUsers = (int)x);

		lobbyName.onValueChanged.AddListener(x =>
		{
			BCManager.LobbyManager.LobbyId= x;
			confirmButton.interactable = !string.IsNullOrEmpty(x);
		});
		lobbyName.text = BCManager.LobbyManager.LobbyId;

		BCManager.LobbyManager.TrackId = track.value;
		BCManager.LobbyManager.GameTypeId = gameMode.value;
		ServerInfo.MaxUsers = (int)playerCountSlider.value;
		
	}

	public void SetGameType(int gameType)
	{
		ServerInfo.GameMode = gameType;
	}

	public void SetTrack(int trackId)
	{
		ServerInfo.TrackId = trackId;
		trackImage.sprite = ResourceManager.Instance.tracks[trackId].trackIcon;
	}

	private string SetLobbyIdFromMaxUsers()
	{
		string lobbyType = ServerInfo.MaxUsers switch
		{
			1 => "one",
			2 => "two",
			3 => "three",
			4 => "four",
			5 => "five",
			6 => "six",
			7 => "seven",
			8 => "eight",
			_ => "unknown"
		};

		BCLobbyManager.ACTIVE_LOBBY_TYPE = lobbyType;
		return lobbyType;
	}
	
	public void SetPlayerCount()
	{
		SetLobbyIdFromMaxUsers();

		playerCountSlider.value = ServerInfo.MaxUsers;
		playerCountSliderText.text = $"{ServerInfo.MaxUsers}";
		playerCountIcon.sprite = ServerInfo.MaxUsers > 1 ? publicLobbyIcon : padlockSprite;
	}

	// UI Hooks

    private bool _lobbyIsValid;

	public void ValidateLobby()
	{
		_lobbyIsValid = true; //string.IsNullOrEmpty(ServerInfo.LobbyName) == false;
	}

	public void TryFocusScreen(UIScreen screen)
	{
		if (_lobbyIsValid)
		{
			UIScreen.Focus(screen);
		}
	}

	public void TryCreateLobby(GameLauncher launcher)
	{
		if (_lobbyIsValid)
		{
			//launcher.JoinOrCreateLobby();
			BCManager.LobbyManager.HostLobby(launcher);
			_lobbyIsValid = false;
		}
	}
}