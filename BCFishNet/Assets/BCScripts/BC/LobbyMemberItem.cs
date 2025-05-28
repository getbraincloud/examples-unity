using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMemberItem : MonoBehaviour
{
    [SerializeField]
    private TMP_Text playerName, readyState;

    public void Config(
        string playerNameValue,
        bool readyStateValue,
        string profileId,
        short netId,
        int rating,
        string cXId,
        Dictionary<string, object> extraData
    )
    {
        _playerNameValue = playerNameValue;
        _readyStateValue = readyStateValue;
        _profileId = profileId;
        _netId = netId;
        _rating = rating;
        _cXId = cXId;
        _extraData = extraData;
    
        UpdateUI();
    }

    public void UpdateUI()
    {
        playerName.text = _playerNameValue != "" ? _playerNameValue : _profileId;
        readyState.text = _readyStateValue ? "Ready" : "Not Ready";
    }

    public void UpdateReady(bool ready)
    {
        _readyStateValue = ready;
        UpdateUI();
    }

    private string _profileId;
    private string _playerNameValue = "";

    private string _cXId;

    private int _rating;
    private short _netId;

    private bool _readyStateValue = false;

    private Dictionary<string, object> _extraData;
}
