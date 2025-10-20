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
			ServerInfo.LobbyName = x;
			confirmButton.interactable = !string.IsNullOrEmpty(x);
		});
		lobbyName.text = ServerInfo.LobbyName = "Session" + Random.Range(0, 1000);

		ServerInfo.TrackId = track.value;
		ServerInfo.GameMode = gameMode.value;
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

	public void SetPlayerCount()
	{
        playerCountSlider.value = ServerInfo.MaxUsers;
		playerCountSliderText.text = $"{ServerInfo.MaxUsers}";
		playerCountIcon.sprite = ServerInfo.MaxUsers > 1 ? publicLobbyIcon : padlockSprite;
	}

	// UI Hooks

    private bool _lobbyIsValid;

	public void ValidateLobby()
	{
		_lobbyIsValid = string.IsNullOrEmpty(ServerInfo.LobbyName) == false;
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
			launcher.JoinOrCreateLobby();
			_lobbyIsValid = false;
		}
	}
}