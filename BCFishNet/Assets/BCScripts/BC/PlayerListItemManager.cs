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
    private Dictionary<string, PlayerData> _playerDataByProfileId = new Dictionary<string, PlayerData>();
    private Dictionary<int, PlayerListItem> _playerItems = new Dictionary<int, PlayerListItem>();
    private readonly List<PaintSplatData> _globalPaintData = new List<PaintSplatData>();

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
        PlayerListEvents.OnClearAllPlayerList += ClearAll;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            PlayerListEvents.OnClearAllPlayerList -= ClearAll;

            Debug.Log("[PlayerListItemManager] Destroyed and unsubscribed from events.");
        }
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
        //Debug.Log($"[PlayerListItemManager] Registered PlayerListItem for client {clientId}");
    }

    public void SaveLobbyMemberPlayerData(string profileId, string name, Color color)
    {
        _playerDataByProfileId[profileId] = new PlayerData {ProfileId = profileId, Name = name, Color = color };
        //Debug.Log($"[PlayerListItemManager] Saved PlayerData for profile '{profileId}': Name='{name}', Color={color}");
    }

    public PlayerData GetPlayerDataByProfileId(string profileId)
    {
        // debug list all player data by profile id
        //Debug.Log("[PlayerListItemManager] Retrieving PlayerData by profile ID: " + profileId);
        foreach (var kvp in _playerDataByProfileId)
        {
            //Debug.Log($"[PlayerListItemManager] ProfileId: {kvp.Key}, Name: {kvp.Value.Name}, Color: {kvp.Value.Color}");
        }

        if (_playerDataByProfileId.TryGetValue(profileId, out PlayerData data))
        {
            //Debug.Log($"[PlayerListItemManager] Retrieved PlayerData for profile '{profileId}': Name='{data.Name}', Color={data.Color}");
            return data;
        }
        else
        {
            //Debug.LogWarning($"[PlayerListItemManager] No PlayerData found for profile '{profileId}'");
            return default;
        }
    }

    public void SavePlayerData(int clientId, PlayerData playerData)
    {
        _playerData[clientId] = playerData;

        // save it by profile id too
        _playerDataByProfileId[playerData.ProfileId] = playerData;

        Debug.Log($"[PlayerListItemManager] Saved PlayerData for client {clientId}: Name='{playerData.Name}', Color={playerData.Color}");
    }

    public bool SaveGlobalPaintData(PaintSplatData paintSplatData)
    {
        bool savedSplat = false;

        // Check for duplicate by comparing color, position, rotation, and scale
        bool isDuplicate = false;
        foreach (var existing in _globalPaintData)
        {
            if (existing.color == paintSplatData.color &&
                existing.anchoredPosition == paintSplatData.anchoredPosition &&
                Mathf.Approximately(existing.rotation, paintSplatData.rotation) &&
                Mathf.Approximately(existing.scale, paintSplatData.scale))
            {
                isDuplicate = true;
                break;
            }
        }

        if (!isDuplicate)
        {
            _globalPaintData.Add(paintSplatData);
            savedSplat = true;
        }

        return savedSplat;
    }

    public bool SaveGlobalPaintData(PaintSplat splat)
    {
        PaintSplatData paintSplatData = new PaintSplatData
        {
            color = splat.Color,
            anchoredPosition = splat.RectTransform.anchoredPosition,
            rotation = splat.RectTransform.eulerAngles.z,
            scale = splat.RectTransform.localScale.x
        };
        return SaveGlobalPaintData(paintSplatData);
    }

    public List<PaintSplatData> GetGlobalPaintData()
    {
        return new List<PaintSplatData>(_globalPaintData);
    }

    public int GetPlayerCount()
    {
        return _playerData.Count;
    }

    public int GetSplatCount()
    {
        return _globalPaintData.Count;
    }

    public bool TryGetPlayerData(int clientId, out PlayerData data)
    {
        bool found = _playerData.TryGetValue(clientId, out data);
        if (found)
            Debug.Log($"[PlayerListItemManager] Retrieved PlayerData for client {clientId}: Name='{data.Name}', Color={data.Color}");
        return found;
    }
    public bool TryGetPlayerDataByProfileId(string profileId, out PlayerData data)
    {
        //Debug.Log("[PlayerListItemManager] Retrieving PlayerData by profile ID: " + profileId);
        foreach (var kvp in _playerDataByProfileId)
        {
            //Debug.Log($"[PlayerListItemManager] ProfileId: {kvp.Key}, Name: {kvp.Value.Name}, Color: {kvp.Value.Color}");
        }

        bool found = _playerDataByProfileId.TryGetValue(profileId, out data);
        if (found)
            Debug.Log($"[PlayerListItemManager] Retrieved PlayerData for profile '{profileId}': Name='{data.Name}', Color={data.Color}");
        return found;
    }

    public PlayerListItem FindPlayerListItemByClientId(int clientId)
    {
        bool found = _playerItems.TryGetValue(clientId, out PlayerListItem item);
        if (found)
            Debug.Log($"[PlayerListItemManager] Found PlayerListItem for client {clientId}");
        return item;
    }   

    public PlayerListItem FindPlayerListItemByConnection(NetworkConnection conn)
    {
        bool found = _playerItems.TryGetValue(conn.ClientId, out PlayerListItem item);
        if (found)
            Debug.Log($"[PlayerListItemManager] Found PlayerListItem for client {conn.ClientId}");
        return item;
    }

    public void DestroyAllGlobalPaintData()
    {
        //Debug.Log("[PlayerListItemManager] Destroying all global paint data.");
        Transform container = UIContainerCache.GetCursorContainer();
        if (container != null)
        {
            // .iterate over all children in the container and destroy them
            PaintSplat[] children = container.GetComponentsInChildren<PaintSplat>(true);
            foreach (PaintSplat child in children)
            {
                Destroy(child.gameObject);
            }
        }
        ClearGlobalPaintData();
    }

    public void ClearGlobalPaintData()
    {
        Debug.Log("[PlayerListItemManager] Clearing global paint data.");
        _globalPaintData.Clear();
    }
    
    public void ClearAll()
    {
        Debug.Log("[PlayerListItemManager] Clearing all player data and item references.");
        
        _playerData.Clear();

        _globalPaintData.Clear();
        _playerItems.Clear();
    }
    public List<int> GetAllPlayerIds()
    {
        List<int> playerIds = new List<int>();
        foreach (NetworkConnection conn in InstanceFinder.NetworkManager.ServerManager.Clients.Values)
        {
            if (conn != null)
            {
                playerIds.Add(conn.ClientId);
            }
        }
        return playerIds;
    }
    
}

public struct PlayerData
{
    public string ProfileId;
    public string Name;
    public Color Color;
}

public struct PaintSplatData
{
    public Vector2 anchoredPosition;
    public Color color;
    public float rotation;
    public float scale;

    public PaintSplatData(Vector2 pos, Color col, float rot, float scl)
    {
        anchoredPosition = pos;
        color = col;
        rotation = rot;
        scale = scl;
    }
}
