using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListItem : NetworkBehaviour
{
    [SerializeField] private Image _bgImage, _squareImage;
    [SerializeField] private TMP_Text _userText;
    [SerializeField] private GameObject _playerCursorPrefab, _hostIcon, _highlightHolder;

    private Button _testButton;
    private NetworkManager _networkManager;

    public PlayerCursor PlayerCursor => _currentCursor;
    private PlayerCursor _currentCursor;
    private PlayerData _playerData;
    public PlayerData PlayerData => _playerData;
    private bool _hasInitialized = false;

    private Coroutine _clearCanvasCoroutine;

    private int localClientId = -1;

    public override void OnStartClient()
    {
        base.OnStartClient();

        _networkManager = InstanceFinder.NetworkManager;

        Transform parentObject = transform.parent;
        if (parentObject == null || parentObject.name != "PlayerListContainer")
        {
            GameObject targetParent = GameObject.Find("PlayerListContainer");
            transform.SetParent(targetParent.transform);
            transform.localScale = Vector3.one;
        }

        localClientId = Owner.ClientId;
        PlayerListItemManager.Instance.RegisterPlayerListItem(localClientId, this);

        // Host sets authoritative server start time if not already set
        if (base.IsHost && PlayerListItemManager.Instance.ServerStartTime < 0)
        {
            double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
            PlayerListItemManager.Instance.SetServerStartTime(now);
            SyncServerStartTimeObserversRpc(now);
        }
        if (base.IsOwner)
        {
            _testButton = GetComponent<Button>();
            if (_currentCursor == null)
                StartCoroutine(DelayedSpawnCursor());
        }

        _squareImage.gameObject.SetActive(base.IsOwner);
        _highlightHolder.SetActive(base.IsOwner);
    }

    // Called by a client to request the authoritative server start time from the host
    [ServerRpc(RequireOwnership = false)]
    public void RequestServerStartTimeServerRpc()
    {
        // Only the host should respond
        if (base.IsHost)
        {
            double serverStartTime = PlayerListItemManager.Instance.ServerStartTime;
            SyncServerStartTimeObserversRpc(serverStartTime);
        }
    }

    [ObserversRpc]
    public void SyncServerStartTimeObserversRpc(double serverStartTime)
    {
        SyncServerStartTime(serverStartTime);
    }

    // Called on all clients to update their local server start time
    public void SyncServerStartTime(double serverStartTime)
    {
        PlayerListItemManager.Instance.SetServerStartTime(serverStartTime);
    }

    // Called by a new client to request all others to echo their info
    [ServerRpc(RequireOwnership = false)]
    void EchoPlayerInfoToAllClientsServerRpc()
    {
        // Tell all clients to echo their info
        EchoPlayerInfoToAllClientsObserversRpc();
    }

    [ObserversRpc]
    void EchoPlayerInfoToAllClientsObserversRpc()
    {
        // Each client sends their info to everyone
        if (_hasInitialized)
        {
            string playerName = string.IsNullOrEmpty(_playerData.Name) ? "Guest_" + _playerData.ProfileId.Substring(0, 8) : _playerData.Name;
            TestChangeServer(_playerData.ProfileId, playerName, _playerData.Color);
        }
    }

    private void Update()
    {
        if (_clearedCanvasMessage == null)
        {
            _clearedCanvasMessage = GameObject.Find("ClearedCanvasObj").GetComponent<TextMeshProUGUI>();
            if (_clearedCanvasMessage != null)
            {
                _clearedCanvasMessage.text = "";
                EnableClearedCanvasImmediately(false);

                Debug.Log("[PlayerListItem] ClearedCanvasObj found and initialized.");
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            EnableClearedCanvasImmediately(false);
        }

        // add the sqaure at the end of the image backgound, since is resizes in the hud
        float bgImageWidth = _bgImage.rectTransform.rect.width;
        if (_bgImageWidth != bgImageWidth)
        {
            _bgImageWidth = bgImageWidth;
            _squareImage.transform.localPosition = new Vector3(_bgImageWidth - _squareImage.rectTransform.rect.width - SQUARE_IMAGE_OFFSET, 0, 0);
        }

        // Periodically echo player info to all clients
        if (base.IsOwner && _hasInitialized)
        {
            _echoTimer += Time.deltaTime;
            if (_echoTimer >= ECHO_INTERVAL)
            {
                EchoPlayerInfoToAllClientsServerRpc();
                _echoTimer = 0f;
            }
        }
    }


    [ServerRpc(RequireOwnership = false)]
    private void RequestStateSyncServerRpc()
    {
        // Instead of just syncing this client, trigger all clients to echo their info
        EchoPlayerInfoToAllClientsServerRpc();
    }

    public void OnTestButtonClicked()
    {
        if (base.IsOwner)
        {
            string profileId = BCManager.Instance.bc.Client.ProfileId;
            string newName = GetPlayerName();
            PlayerData playerData = PlayerListItemManager.Instance.GetPlayerDataByProfileId(profileId);
            Color newColor = playerData.Color;

            TestChangeServer(profileId, newName, newColor);
        }
    }
    public void OnClearCanvasClicked()
    {
        if (base.IsServer)
        {
            PlayerListItemManager.Instance.DestroyAllGlobalPaintData();
            ClearCanvasForAllClients();
        }
    }

    [ObserversRpc]
    private void ClearCanvasForAllClients()
    {
        Debug.Log("[PlayerListItem] Clearing canvas for all clients.");
        PlayerListItemManager.Instance.DestroyAllGlobalPaintData();

        // Only start a new coroutine if one isn't already running
        if (_clearCanvasCoroutine == null)
        {
            _clearCanvasCoroutine = StartCoroutine(DisplayClearedMessage());
        }
    }

    private const string HOST_CLEAR_CANVAS_MESSAGE_PREFIX = "Host cleared the canvas\n\n";
    private IEnumerator DisplayClearedMessage()
    {
        EnableClearedCanvasImmediately(true);

        _clearedCanvasMessage.text = HOST_CLEAR_CANVAS_MESSAGE_PREFIX + " 3";
        yield return new WaitForSeconds(1f);

        _clearedCanvasMessage.text = HOST_CLEAR_CANVAS_MESSAGE_PREFIX + " 2";
        yield return new WaitForSeconds(1f);

        _clearedCanvasMessage.text = HOST_CLEAR_CANVAS_MESSAGE_PREFIX + " 1";
        yield return new WaitForSeconds(1f);

        EnableClearedCanvasImmediately(false);
        _clearCanvasCoroutine = null;
    }

    private void EnableClearedCanvasImmediately(bool enable)
    {
        _clearedCanvasMessage.transform.localScale = enable ? Vector3.one : Vector3.zero;

        if (enable)
        {
            _clearedCanvasMessage.text = "Host cleared the canvas\n\n 3";
        }
        else
        {
            _clearedCanvasMessage.text = "";
            if (_clearCanvasCoroutine != null) StopCoroutine(_clearCanvasCoroutine);
            _clearCanvasCoroutine = null;
        }
    }

    private string GenerateRandomString()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    void OnDestroy()
    {
        PlayerListItemManager.Instance.UnregisterPlayerListItem(Owner.ClientId);
    }

    private Color GenerateRandomColor()
    {
        string hex = Guid.NewGuid().ToString("N").Substring(0, 6);
        int r = Convert.ToInt32(hex.Substring(0, 2), 16);
        int g = Convert.ToInt32(hex.Substring(2, 2), 16);
        int b = Convert.ToInt32(hex.Substring(4, 2), 16);
        return new Color(r / 255f, g / 255f, b / 255f);
    }

    IEnumerator DelayedSpawnCursor()
    {
        // If the clients, let's delay a bit, to let the server get there and we can echo back to it
        yield return null;
        if (!Owner.IsHost) yield return new WaitForSeconds(DELAY);

        if (_currentCursor == null)
            SpawnCursor(Owner);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnCursor(NetworkConnection conn)
    {
        NetworkObject nob = _networkManager.GetPooledInstantiated(_playerCursorPrefab, transform.parent.parent, true);
        _networkManager.ServerManager.Spawn(nob, conn);

        SetCursorRef(nob);
        StartCoroutine(UpdateData(conn));
    }

    IEnumerator UpdateData(NetworkConnection conn)
    {
        yield return new WaitForSeconds(SHORT_DELAY);

        PlayerData data;
        if (base.IsOwner && PlayerListItemManager.Instance.TryGetPlayerDataByProfileId(BCManager.Instance.bc.Client.ProfileId, out data))
        {
            Debug.Log($"[PlayerListItem] Reusing saved data for profileid {BCManager.Instance.bc.Client.ProfileId}, {data.Name}, {data.Color} ");
            TestChange(data.ProfileId, data.Name, data.Color);
        }
        else if (PlayerListItemManager.Instance.TryGetPlayerData(conn.ClientId, out data))
        {
            Debug.Log($"[PlayerListItem] Reusing saved data for client {conn.ClientId}, {data.Name}, {data.Color} ");
            TestChange(data.ProfileId, data.Name, data.Color);
        }
        else
        {
            Debug.Log($"[PlayerListItem] No data for client {conn.ClientId}, randomizing");
            Randomize();
        }

        UpdateIsHost(conn.IsHost);

        // If joining client, request the server start time from the host
        RequestServerStartTimeServerRpc();

        // When a new PlayerListItem is created, broadcast our info to all
        EchoPlayerInfoToAllClientsServerRpc();

        RequestStateSyncServerRpc();
    }

    [ObserversRpc]
    public void UpdateIsHost(bool isHost)
    {
        _hostIcon.SetActive(isHost);
    }

    [ObserversRpc]
    private void SetCursorRef(NetworkObject nob)
    {
        Debug.Log($"Set cursor ref for client {Owner.ClientId}");
        _currentCursor = nob.GetComponent<PlayerCursor>();
    }

    [ServerRpc]
    public void TestChangeServer(string profileId, string playerName, Color newColor)
    {
        _playerData = new PlayerData { ProfileId = profileId, Name = playerName, Color = newColor };
        _hasInitialized = true;

        PlayerListItemManager.Instance.SavePlayerData(Owner.ClientId, _playerData);

        TestChange(_playerData.ProfileId, _playerData.Name, _playerData.Color);
        UpdateIsHost(Owner.IsHost);
    }

    [ObserversRpc]
    private void Randomize()
    {
        OnTestButtonClicked();
    }

    [ObserversRpc]
    public void TestChange(string profileId, string playerName, Color newColor)
    {
        _playerData = new PlayerData { ProfileId = profileId, Name = playerName, Color = newColor };
        _userText.text = base.IsOwner ? GetPlayerName() + " (You)" : playerName;
        _bgImage.color = newColor;
        _squareImage.color = newColor;
        _currentCursor?.ChangeColor(newColor);
        _hasInitialized = true;

        PlayerListItemManager.Instance.SavePlayerData(Owner.ClientId, _playerData);
    }

    public void InitializePlayer()
    {
        _testButton = GetComponent<Button>();
    }

    // for the current lobby SendColorUpdateSignal to all other members of the color of this members image
    public void SendColorUpdateSignal(Color color)
    {
        string profileId = BCManager.Instance.bc.Client.ProfileId;
        string newName = GetPlayerName();
        TestChangeServer(profileId, newName, color);
    }

    public void UpdateSplatScale(float scale)
    {
        _currentCursor?.UpdateSplatScale(scale);
    }

    private string GetPlayerName()
    {
        string playerName = BCManager.Instance.PlayerName;
        if (string.IsNullOrEmpty(playerName))
        {
            string profileId = BCManager.Instance.bc.Client.ProfileId;
            return "Guest_" + profileId.Substring(0, 4);
        }
        return playerName;
    }


    private TextMeshProUGUI _clearedCanvasMessage = null;
    private float _bgImageWidth = 0f;
    private const float SQUARE_IMAGE_OFFSET = 20f;

    private const float DELAY = 0.15f;
    private const float SHORT_DELAY = 0.05f;
    
    private float _echoTimer = 0f;
    private const float ECHO_INTERVAL = 3f; // secondss
        
}
