using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class PlaybackSelector : MonoBehaviour
{
    public string playerId;
    private string playerName;
    private int playerScore;

    [SerializeField]
    private TMP_Text nameText;
    [SerializeField]
    private TMP_Text scoreText;
    [SerializeField]
    private GameObject addButton;

    private LobbyControl lobbyControl;

    private void Start()
    {
        lobbyControl = LobbyControl.Singleton;
    }

    public void InitValues(string newId, string newName, int newScore)
    {
        playerId = newId;
        playerName = newName;
        playerScore = newScore;
    }

    public void UpdateLabels()
    {
        nameText.text = playerName;
        scoreText.text = playerScore.ToString();
    }

    public void AddPlayerId()
    {
        lobbyControl.AddNewPlayerIdSignal(playerId);
    }

    public void HideButton()
    {
        addButton.SetActive(false);
    }
}
