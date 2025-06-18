using System.Collections.Generic;
using UnityEngine;
using FishNet;
using FishNet.Managing;
using FishNet.Connection;
using BCFishNet;

public class PlayerListItemManager : MonoBehaviour
{
    public static PlayerListItemManager Instance { get; private set; }

    private Dictionary<int, PlayerData> _playerData = new Dictionary<int, PlayerData>();
    private Dictionary<int, PlayerListItem> _playerItems = new Dictionary<int, PlayerListItem>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[PlayerListItemManager] Initialized and subscribed to events.");

        PlayerListEvents.OnResyncPlayerList += ResyncPlayerListItems;
        PlayerListEvents.OnClearAllPlayerList += ClearAll;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            PlayerListEvents.OnClearAllPlayerList -= ClearAll;
            PlayerListEvents.OnResyncPlayerList -= ResyncPlayerListItems;

            Debug.Log("[PlayerListItemManager] Destroyed and unsubscribed from events.");
        }
    }

    private void ResyncPlayerListItems()
    {
        Debug.Log("[PlayerListItemManager] Resyncing player list items...");

        foreach (NetworkConnection conn in InstanceFinder.NetworkManager.ServerManager.Clients.Values)
        {
            Debug.Log($"[PlayerListItemManager] Checking client {conn.ClientId}");

            if (TryGetPlayerData(conn.ClientId, out var data))
            {
                var playerListItem = FindPlayerListItemByConnection(conn);
                if (playerListItem != null)
                {
                    Debug.Log($"[PlayerListItemManager] Resyncing PlayerListItem for client {conn.ClientId} with name '{data.Name}' and color {data.Color}");
                    playerListItem.TestChange(data.Name, data.Color);
                    playerListItem.UpdateIsHost(conn.IsHost);
                }
                else
                {
                    Debug.LogWarning($"[PlayerListItemManager] No PlayerListItem found for client {conn.ClientId}.");
                }
            }
            else
            {
                Debug.LogWarning($"[PlayerListItemManager] No saved PlayerData found for client {conn.ClientId}.");
            }
        }
    }

    public void RegisterPlayerListItem(int clientId, PlayerListItem item)
    {
        _playerItems[clientId] = item;
        Debug.Log($"[PlayerListItemManager] Registered PlayerListItem for client {clientId}");
    }

    public void SavePlayerData(int clientId, string name, Color color)
    {
        if (string.IsNullOrEmpty(name)) return;
    
        _playerData[clientId] = new PlayerData { Name = name, Color = color };
        Debug.Log($"[PlayerListItemManager] Saved PlayerData for client {clientId}: Name='{name}', Color={color}");
    }

    public bool TryGetPlayerData(int clientId, out PlayerData data)
    {
        bool found = _playerData.TryGetValue(clientId, out data);
        if (found)
            Debug.Log($"[PlayerListItemManager] Retrieved PlayerData for client {clientId}: Name='{data.Name}', Color={data.Color}");
        return found;
    }

    public PlayerListItem FindPlayerListItemByConnection(NetworkConnection conn)
    {
        bool found = _playerItems.TryGetValue(conn.ClientId, out PlayerListItem item);
        if (found)
            Debug.Log($"[PlayerListItemManager] Found PlayerListItem for client {conn.ClientId}");
        return item;
    }

    public void ClearAll()
    {
        Debug.Log("[PlayerListItemManager] Clearing all player data and item references.");
        _playerItems.Clear();
        _playerData.Clear();
    }
}

public struct PlayerData
{
    public string Name;
    public Color Color;
}
