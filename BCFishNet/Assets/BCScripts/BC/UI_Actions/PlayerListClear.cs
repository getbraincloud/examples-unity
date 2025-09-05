using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerListClear : MonoBehaviour
{
    
    public void OnClearCanvasClicked()
    {
        // find the player list item for the local player
        PlayerListItem[] items = GameObject.FindObjectsOfType<PlayerListItem>();
        foreach (var item in items)
        {
            if (item.IsOwner)
            {
                item.OnClearCanvasClicked();
                break;
            }
        }
    }
}
