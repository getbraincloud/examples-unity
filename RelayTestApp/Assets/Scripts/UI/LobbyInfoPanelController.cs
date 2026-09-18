using TMPro;
using UnityEngine;

/// <summary>
/// Lobby INFO tab — lobby id, region, player count, time-in-lobby, and a short status line,
/// matching the Lobby mockup's INFO panel. Refreshed each frame while visible.
/// </summary>
public class LobbyInfoPanelController : MonoBehaviour
{
    public TMP_Text LobbyIdText;
    public TMP_Text RegionText;
    public TMP_Text PlayerCountText;
    public TMP_Text TimeInLobbyText;
    public TMP_Text StatusText;

    private float _timeInLobbySec;

    private void OnEnable()
    {
        _timeInLobbySec = 0f;
    }

    private void Update()
    {
        _timeInLobbySec += Time.deltaTime;

        Lobby lobby = StateManager.Instance.CurrentLobby;
        string lobbyId = lobby != null ? lobby.LobbyID : "-";
        LobbyIdText.text = "Lobby: " + lobbyId;

        string region = "-";
        int colonPos = lobbyId.IndexOf(':');
        if (colonPos > 0 && !int.TryParse(lobbyId.Substring(0, colonPos), out _))
            region = lobbyId.Substring(0, colonPos);
        RegionText.text = "Region: " + region;

        int playerCount = lobby != null ? lobby.Members.Count : 0;
        PlayerCountText.text = "Players: " + playerCount;

        int totalSec = (int)_timeInLobbySec;
        TimeInLobbyText.text = $"Time in lobby: {totalSec / 60:D2}:{totalSec % 60:D2}";

        bool isHost = GameManager.Instance.IsLocalUserHost();
        StatusText.text = isHost ? "Press Start when ready." : "Waiting for the host to start...";
    }
}
