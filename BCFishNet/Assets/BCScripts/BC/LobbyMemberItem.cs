using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BrainCloud.JsonFx.Json;

public class LobbyMemberItem : MonoBehaviour
{
    [SerializeField] private TMP_Text playerName, readyState;
    [SerializeField] private GameObject _highlightHolder;

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
    void Start()
    {
        UpdateUI();
    }

    // Apply Color Update
    public void ApplyColorUpdate(Color color)
    {
        // changes this colour
        Image img = playerName.transform.parent.gameObject.GetComponent<Image>();
        if (img != null)
        {
            img.color = color;
        }
    }
    public void SendCurrentColourSignal()
    {
        Image img = playerName.transform.parent.gameObject.GetComponent<Image>();
        if (img != null)
        {
            Color color = img.color;
            SendColorUpdateSignal(color);
        }
        else
        {
            Debug.LogError("Image component not found on parent GameObject.");
        }
    }
    
    // for the current lobby SendColorUpdateSignal to all other members of the color of this members image
    public void SendColorUpdateSignal(Color color)
    {
        if (BCManager.Instance.bc == null)
        {
            Debug.LogError("BCManager instance or BrainCloudWrapper is not initialized.");
            return;
        }

        // changes this colour
        ApplyColorUpdate(color);

        // Send a signal to all other members in the lobby with the new color
        Dictionary<string, object> signalData = new Dictionary<string, object>();

        string hexColor = ColorUtility.ToHtmlStringRGBA(color);
        signalData["color"] = hexColor;
        BCManager.Instance.bc.LobbyService.SendSignal(BCManager.Instance.CurrentLobbyId, signalData);
    }

    public void UpdateUI()
    {
        playerName.text = _playerNameValue != "" ? _playerNameValue : _profileId;
        readyState.text = _readyStateValue ? "Ready" : "Not Ready";

        _highlightHolder.SetActive(_profileId == BCManager.Instance.bc.Client.ProfileId);
    }

    public void UpdateReady(bool ready)
    {
        _readyStateValue = ready;
        UpdateUI();
    }
    public string ProfileId => _profileId;
    private string _profileId;
    private string _playerNameValue = "";

    private string _cXId;

    private int _rating;
    private short _netId;

    private bool _readyStateValue = false;

    private Dictionary<string, object> _extraData;
}
