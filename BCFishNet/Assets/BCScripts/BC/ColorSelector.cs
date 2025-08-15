using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;

public class ColorSelector : MonoBehaviour
{
    [Header("UI References")]
    public GameObject targetObject;

    [Header("Color Buttons")]
    public List<Button> colorButtons; // all 21 buttons in Inspector
    
    private void Start()
    {
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

        if ((memberItem != null && memberItem.ProfileId != localProfileId) ||
            (playerItem != null && playerItem.PlayerData.ProfileId != localProfileId))
        {
            Debug.Log("Attempted to open color selector for non-local member.");
            return;
        }

        this.gameObject.SetActive(true);
    }
    void Update()
    {
        if((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) &&
            !ClickingSelfOrChild())
        {
            // If clicked on this selector or its children
            HideColorSelector();
            return;
        }
    }
    
    public bool ClickingSelfOrChild()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            if (result.gameObject.transform.IsChildOf(transform))
            {
                Debug.Log("Clicked on self or child (raycast): " + result.gameObject.name);
                return true;
            }
        }

        // Fallback: check if mouse is within this selector's rect (handles masked/clipped UI)
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, null))
        {
            Debug.Log("Clicked on self or child (rect bounds fallback)");
            return true;
        }

        Debug.Log("Did not click on self or child.");
        return false;
    }
    
    private void HideColorSelector()
    {
        this.gameObject.SetActive(false);
    }

    void OnColorSelected(Color selectedColor)
    {
        ApplyColorToTarget(selectedColor);
        HideColorSelector();
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

        UpdatePlayerColor updateColor = targetObject.GetComponent<UpdatePlayerColor>();
        if (updateColor != null)
        {
            updateColor.SaveColorUpdate();
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