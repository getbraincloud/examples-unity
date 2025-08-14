using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdatePlayerColor : MonoBehaviour
{
    public Image targetObject;
    public Image sourceComponent;

    void Start()
    {
        // Initialize the color from the target object
        UpdateFromPlayerData();
    }

    void UpdateFromPlayerData()
    {
        if (BCManager.Instance == null || BCManager.Instance.bc == null || !BCManager.Instance.bc.Client.IsAuthenticated())
        {
            return;
        }
        PlayerData data;
        
        // we have some data, use it
        if (PlayerListItemManager.Instance.TryGetPlayerDataByProfileId(BCManager.Instance.bc.Client.ProfileId, out data))
        {
            sourceComponent.color = data.Color;
            targetObject.color = data.Color;
        }
        else
        {
            // .get another colour
            Color randomColor = new Color(Random.value, Random.value, Random.value);
            sourceComponent.color = randomColor;
            SaveColorUpdate();
        }
    }

    void OnEnable()
    {
        UpdateFromPlayerData();
    }


    // Save Color Update
    public void SaveColorUpdate()
    {
        if (targetObject == null || sourceComponent == null)
        {
            return;
        }
        string playerId = BCManager.Instance.bc.Client.ProfileId;
        string playerName = BCManager.Instance.PlayerName;

        // update it
        targetObject.color = sourceComponent.color;
        PlayerListItemManager.Instance.SaveLobbyMemberPlayerData(playerId, playerName, sourceComponent.color);
    }
    
}
