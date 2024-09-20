using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class PlaybackSelector : MonoBehaviour
{
    [SerializeField]
    private TMP_Text nameText;
    [SerializeField]
    private TMP_Text scoreText;
    [SerializeField]
    private GameObject addButton;

    [HideInInspector]
    public string playerId;
    private string playerName;
    private int playerScore;

    public void InitValues(string newId, string newName, int newScore)
    {
        playerId = newId;
        playerName = newName;
        playerScore = newScore;
        addButton.SetActive(true);
    }

    public void UpdateLabels()
    {
        nameText.text = playerName;
        scoreText.text = playerScore.ToString();
    }

    public void AddPlayerId()
    {
        LobbyControl.Singleton.AddNewPlayerIdSignal(playerId);
    }

    public void HideButton()
    {
        addButton.SetActive(false);
    }
}
