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
    [SerializeField] private Image _bgImage;
    [SerializeField] private TMP_Text _userText;
    [SerializeField] private GameObject _playerCursorPrefab, _hostIcon, _highlightHolder;

    private Button _testButton;
    private NetworkManager _networkManager;

    public PlayerCursor PlayerCursor => _currentCursor;
    private PlayerCursor _currentCursor;
    private PlayerData _playerData;
    public PlayerData PlayerData => _playerData;
    private bool _hasInitialized = false;

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
            GetComponent<PlayerListItem>().enabled = false;
            RequestStateSyncServerRpc(); // Ask server to resend state
        }

        _highlightHolder.SetActive(base.IsOwner);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestStateSyncServerRpc()
    {
        if (_hasInitialized)
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
            string newName = BCManager.Instance.PlayerName;
            Color newColor = GenerateRandomColor(); // TODO persist this color from lobby info

            TestChangeServer(profileId, newName, newColor);
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
        _userText.text = playerName;
        _bgImage.color = newColor;
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
        string newName = BCManager.Instance.PlayerName;
        TestChangeServer(profileId, newName, color);
    }

}
