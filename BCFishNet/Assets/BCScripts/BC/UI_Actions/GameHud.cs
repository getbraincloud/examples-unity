using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameHud : MonoBehaviour
{

    [SerializeField] private TMP_Text numPlayersText, numSplatsText;

    // Start is called before the first frame update
    void Start()
    {
        UpdatePlayerCount(0);
        UpdateSplatCount(0);
        StartCoroutine(UpdateMainStatus());
    }

    private IEnumerator UpdateMainStatus()
    {
        while (true)
        {
            // Here you can add logic to update the main status text
            // For example, you might want to show the current game state or other relevant information
            yield return new WaitForSeconds(0.15f); 
            // find all players and splats
            int playerCount = PlayerListItemManager.Instance.GetPlayerCount();
            int splatCount = PlayerListItemManager.Instance.GetSplatCount();

            UpdatePlayerCount(playerCount);
            UpdateSplatCount(splatCount);
        }
    }
    public void UpdatePlayerCount(int count)
    {
        numPlayersText.text = $"Players: {count:N0} / {TimeUtils.MAX_PLAYERS:N0}";
    }

    public void UpdateSplatCount(int count)
    {
        numSplatsText.text = $"Canvas Load: {count:N0}";
    }
}
