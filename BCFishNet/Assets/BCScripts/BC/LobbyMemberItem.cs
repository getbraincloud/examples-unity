using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BrainCloud.JsonFx.Json;

public class LobbyMemberItem : MonoBehaviour
{
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private GameObject _highlightHolder, _readyStateHolder, _notReadyStateHolder, _hostIconHolder;

    private LobbyMemberData _data;
    public LobbyMemberData Data => _data;

    public void Config(LobbyMemberData data)
    {
        _data = data;

        // add the player name value and default colour to it
        _playerData.Name = !string.IsNullOrEmpty(_data.PlayerNameValue) ? _data.PlayerNameValue : "Guest_" + _data.ProfileId.Substring(0, 4);
        PlayerData pdata;
        if (PlayerListItemManager.Instance.TryGetPlayerDataByProfileId(_data.ProfileId, out pdata))
        {
            _playerData.Color = pdata.Color;
        }
        else if (_data.ExtraData != null && _data.ExtraData.ContainsKey("colour"))
        {
            string hexColor = _data.ExtraData["colour"] as string;
            Color color;
            if (ColorUtility.TryParseHtmlString("#" + hexColor, out color))
            {
                _playerData.Color = color;
            }
            else
            {
                _playerData.Color = Color.black;
            }
        }
        else
        {
            _playerData.Color = Color.black;
        }

        UpdateUI();

        if (data.ProfileId == BCManager.Instance.bc.Client.ProfileId)
        {
            Invoke("SendCurrentColourSignal", TimeUtils.SHORT_DELAY);
        }
    }

    void Start()
    {   
        UpdateUI();
    }

    void UpdateReadyState()
    {
        Dictionary<string, object> extra = BCManager.Instance.GetLobbyExtraData();
        BCManager.Instance.bc.LobbyService.UpdateReady(BCManager.Instance.CurrentLobbyId, _data.ReadyStateValue, extra);
    }

    // Apply Color Update
    public void ApplyColorUpdate(Color color)
    {
        // changes this colour
        Image img = playerName.transform.parent.parent.gameObject.GetComponent<Image>();
        if (img != null)
        {
            img.color = color;
        }
        _playerData.Color = color;

        PlayerListItemManager.Instance.SaveLobbyMemberPlayerData(_data.ProfileId, _data.PlayerNameValue, _playerData.Color);
    }

    public void SendCurrentColourSignal()
    {
        Image img = playerName.transform.parent.parent.gameObject.GetComponent<Image>();
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
        playerName.text = !string.IsNullOrEmpty(_data.PlayerNameValue) ? _data.PlayerNameValue : "Guest_" + _data.ProfileId.Substring(0, 4);

        _readyStateHolder.SetActive(_data.ReadyStateValue);
        _notReadyStateHolder.SetActive(!_data.ReadyStateValue);

        _highlightHolder.SetActive(_data.ProfileId == BCManager.Instance.bc.Client.ProfileId);
        _hostIconHolder.SetActive(BCManager.Instance.LobbyOwnerId == _data.ProfileId);
        ApplyColorUpdate(_playerData.Color);
    }

    public void UpdateReady(bool ready)
    {
        if (_data != null)
        {
            _data.ReadyStateValue = ready;
        }
        UpdateUI();
    }
    public string ProfileId => _data?.ProfileId;
    public PlayerData PlayerData => _playerData;
    private PlayerData _playerData;
}

public class LobbyMemberData
{
    public string PlayerNameValue;
    public bool ReadyStateValue;
    public string ProfileId;
    public short NetId;
    public int Rating;
    public string CXId;
    public Dictionary<string, object> ExtraData;

    public LobbyMemberData(string playerNameValue, bool readyStateValue, string profileId, short netId, int rating, string cXId, Dictionary<string, object> extraData)
    {
        PlayerNameValue = playerNameValue;
        ReadyStateValue = readyStateValue;
        ProfileId = profileId;
        NetId = netId;
        Rating = rating;
        CXId = cXId;
        ExtraData = extraData;
    }
}
