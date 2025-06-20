using System.Collections.Generic;
using UnityEngine;
using FishNet;
using FishNet.Managing;
using FishNet.Connection;
using BCFishNet;
using System.Collections;

public class PlayerListItemManager : MonoBehaviour
{
    public static PlayerListItemManager Instance { get; private set; }

    private Dictionary<int, PlayerData> _playerData = new Dictionary<int, PlayerData>();
    private Dictionary<int, PlayerListItem> _playerItems = new Dictionary<int, PlayerListItem>();
    private Dictionary<int, List<PaintSplatData>> _playerPaintData = new Dictionary<int, List<PaintSplatData>>();

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
            if (TryGetPlayerData(conn.ClientId, out var data))
            {
                var playerListItem = FindPlayerListItemByConnection(conn);
                if (playerListItem != null)
                {
                    playerListItem.TestChange(data.Name, data.Color);
                    playerListItem.UpdateIsHost(conn.IsHost);

                    Debug.Log("[PlayerListItemManager] complete for new item...");
                }
            }
        }
    }

    public IEnumerator WaitForPlayerCursorAndRestore(int clientId, PlayerListItem playerListItem)
    {
        float timeout = 3f;
        float elapsed = 0f;
        float retryInterval = 0.1f;
        Debug.LogWarning($"[PlayerListItemManager] Starting {clientId} after {timeout} seconds.");

        while (elapsed < timeout)
        {
            if (playerListItem.PlayerCursor != null)
            {
                Debug.Log("[PlayerListItemManager] PlayerCursor found, restoring paint splats.");
                playerListItem.PlayerCursor.RestorePaintSplatsForPlayer(clientId);
                yield break;
            }

            elapsed += retryInterval;
            yield return new WaitForSeconds(retryInterval);
        }

        Debug.LogWarning($"[PlayerListItemManager] Failed to find PlayerCursor for client {clientId} after {timeout} seconds.");
    }

    private PlayerCursor FindPlayerCursorByClientId(int clientId)
    {
        PlayerCursor[] cursors = Object.FindObjectsOfType<PlayerCursor>();
        foreach (var cursor in cursors)
        {
            if (cursor.clientId == clientId)
                return cursor;
        }
        return null;
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

    public void SavePlayerPaintData(int clientId, PaintSplat splat)
    {
        if (!_playerPaintData.ContainsKey(clientId))
            _playerPaintData[clientId] = new List<PaintSplatData>();

        var dataList = _playerPaintData[clientId];

        var newData = new PaintSplatData(splat.RectTransform.anchoredPosition, splat.Color);
        dataList.Add(newData);

        Debug.Log($"[PlayerListItemManager] Saved paint splat for client {clientId}");
    }

    public List<PaintSplatData> GetPlayerPaintData(int clientId)
    {
        if (_playerPaintData.TryGetValue(clientId, out var dataList))
            return dataList;
        return new List<PaintSplatData>();
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

public struct PaintSplatData
{
    public Vector2 anchoredPosition;
    public Color color;

    public PaintSplatData(Vector2 pos, Color col)
    {
        anchoredPosition = pos;
        color = col;
    }
}
