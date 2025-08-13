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

        PlayerListItemManager.Instance.RegisterPlayerListItem(Owner.ClientId, this);

        if (base.IsOwner)
        {
            _testButton = GetComponent<Button>();

            if (_currentCursor == null)
                StartCoroutine(DelayedSpawnCursor());
        }
        else
        {
            //GetComponent<PlayerListItem>().enabled = false;
            RequestStateSyncServerRpc(); // Ask server to resend state
        }

        _squareImage.gameObject.SetActive(base.IsOwner);
        _highlightHolder.SetActive(base.IsOwner);
    }

    private TextMeshProUGUI _clearedCanvasMessage = null;
    private float _bgImageWidth = 0f;
    private const float SQUARE_IMAGE_OFFSET = 50f;
    private void Update()
    {
        if (_clearedCanvasMessage == null)
        {
            _clearedCanvasMessage = GameObject.Find("ClearedCanvasObj").GetComponent<TextMeshProUGUI>();
            if (_clearedCanvasMessage != null)
            {
                _clearedCanvasMessage.text = "";

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
    }


    [ServerRpc(RequireOwnership = false)]
    private void RequestStateSyncServerRpc()
    {
        if (_hasInitialized && base.IsOwner)
        {
            TestChange(_playerData.ProfileId, _playerData.Name, _playerData.Color);
            UpdateIsHost(Owner.IsHost);
        }
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
        if (!Owner.IsHost) yield return new WaitForSeconds(0.5f);

        if (_currentCursor == null)
            SpawnCursor(Owner);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnCursor(NetworkConnection conn)
    {
        NetworkObject nob = _networkManager.GetPooledInstantiated(_playerCursorPrefab, transform.parent.parent, true);
        _networkManager.ServerManager.Spawn(nob, conn);

        SetCursorRef(nob);
        PlayerData data;
        if (PlayerListItemManager.Instance.TryGetPlayerData(conn.ClientId, out data))
        {
            Debug.Log($"[PlayerListItem] Reusing saved data for client {conn.ClientId}, {data.Name}, {data.Color} ");
            TestChange(data.ProfileId, data.Name, data.Color);
        }
        else if (base.IsOwner && PlayerListItemManager.Instance.TryGetPlayerDataByProfileId(BCManager.Instance.bc.Client.ProfileId, out data))
        {
            Debug.Log($"[PlayerListItem] Reusing saved data for profileid {BCManager.Instance.bc.Client.ProfileId}, {data.Name}, {data.Color} ");
            TestChange(data.ProfileId, data.Name, data.Color);
        }
        else
        {
            Debug.Log($"[PlayerListItem] No data for client {conn.ClientId}, randomizing");
            Randomize();
        }

        UpdateIsHost(conn.IsHost);
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
    }

    [ObserversRpc]
    private void Randomize()
    {
        OnTestButtonClicked();
    }

    [ObserversRpc]
    private void StartTest()
    {
        StartCoroutine(TestMessages());
    }

    private IEnumerator TestMessages()
    {
        while (this.enabled)
        {
            OnTestButtonClicked();
            yield return new WaitForSeconds(0.5f);
        }
    }

    [ObserversRpc]
    public void TestChange(string profileId, string playerName, Color newColor)
    {
        _playerData = new PlayerData { ProfileId = profileId, Name = playerName, Color = newColor };
        _userText.text = base.IsOwner ? playerName + " (You)" : playerName;
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

    private string GetPlayerName()
    {
        string playerName = BCManager.Instance.PlayerName;
        if (string.IsNullOrEmpty(playerName))
        {
            return "Guest_" + Owner.ClientId;
        }
        return playerName;
    }
}
