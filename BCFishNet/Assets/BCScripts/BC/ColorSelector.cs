using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;

public class ColorSelector : MonoBehaviour
{
    [Header("UI References")]
    public Button showSelectorButton;
    public GameObject targetObject;

    [Header("Color Buttons")]
    public List<Button> colorButtons; // all 21 buttons in Inspector
    
    private void Start()
    {
        // Hide color selector initially
        this.gameObject.SetActive(false);

        // Hook up color buttons
        foreach (var button in colorButtons)
        {
            var color = button.GetComponent<Image>().color; // assumes the button's Image shows the color
            button.onClick.AddListener(() => OnColorSelected(color));
        }
    }

    public void ShowColorSelector()
    {
        LobbyMemberItem memberItem = targetObject.GetComponent<LobbyMemberItem>();
        PlayerListItem playerItem = targetObject.GetComponent<PlayerListItem>();
        string localProfileId = BCManager.Instance.bc.Client.ProfileId;

        if (memberItem == null && playerItem == null)
        {
            Debug.LogWarning("No LobbyMemberItem or PlayerListItem on targetObject.");
            return;
        }

        if ((memberItem != null && memberItem.ProfileId != localProfileId) ||
            (playerItem != null && playerItem.PlayerData.ProfileId != localProfileId))
        {
            Debug.Log("Attempted to open color selector for non-local member.");
            return;
        }

        this.gameObject.SetActive(true);
    }

    void OnColorSelected(Color selectedColor)
    {
        ApplyColorToTarget(selectedColor);
        this.gameObject.SetActive(false);
    }

    void ApplyColorToTarget(Color color)
    {
        if (targetObject == null)
            return;

        // Try to apply to UI Image (2D UI) - Generic
        Image img = targetObject.GetComponent<Image>();
        if (img != null)
        {
            img.color = color;
        }

        // if the target is a LobbyMemberItem, make sure to send a lobby signal so all others know about the new color
        LobbyMemberItem lobbyMemberItem = targetObject.GetComponent<LobbyMemberItem>();
        if (lobbyMemberItem != null)
        {
            lobbyMemberItem.SendColorUpdateSignal(color);
        }

        // if the target is a PlayerListItem, make sure to send a player signal so all others know about the new color
        PlayerListItem playerListItem = targetObject.GetComponent<PlayerListItem>();
        if (playerListItem != null)
        {
            playerListItem.SendColorUpdateSignal(color);
        }
    }
}