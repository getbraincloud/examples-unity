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
        lobbyName.text = ClientInfo.LobbyName;
    }

    private void SetLobbyName(string lobby)
	{
		ClientInfo.LobbyName = lobby;
		//confirmButton.interactable = !string.IsNullOrEmpty(lobby);
	}
}
