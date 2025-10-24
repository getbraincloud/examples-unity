using UnityEngine;
using UnityEngine.UI;

public class JoinGameUI : MonoBehaviour {
    
    public InputField lobbyName;
	public Button confirmButton;

	private void OnEnable()
	{
		SetLobbyName(lobbyName.text);
	}

	private void Start() {
		lobbyName.onValueChanged.AddListener(SetLobbyName);
        lobbyName.text = BCManager.LobbyManager.LobbyId;
    }

    private void SetLobbyName(string lobby)
	{
		BCManager.LobbyManager.LobbyId = lobby;
		//confirmButton.interactable = !string.IsNullOrEmpty(lobby);
	}
}
