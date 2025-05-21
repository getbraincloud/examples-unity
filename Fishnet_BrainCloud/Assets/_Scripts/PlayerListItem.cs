using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListItem : NetworkBehaviour
{
    [SerializeField]
    private Image _bgImage;

    [SerializeField]
    private TMP_Text _userText, _localText;

    [SerializeField]
    private GameObject _playerCursorPrefab, _hostIcon;

    private Button _testButton;
    private NetworkManager _networkManager;
    private PlayerCursor _currentCursor;

    private float _testInterval = .5f;

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
        
        _localText.gameObject.SetActive(IsOwner);

        if (base.IsOwner)
        {
            _testButton = GetComponent<Button>();
            _testButton.onClick.AddListener(OnTestButtonClicked);

            //spawn cursor
            if (_currentCursor == null)
            {
                StartCoroutine(DelayedSpawnCursor());
            }
        }
        else
        {
            GetComponent<PlayerListItem>().enabled = false;
        }
    }

    public void OnTestButtonClicked()
    {
        string newName = GenerateRandomString();
        Color newColor = GenerateRandomColor();

        TestChangeServer(newName, newColor);
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
        yield return new WaitForSeconds(0.6f);
        if(_currentCursor == null)
            SpawnCursor(Owner);
    }

    [ServerRpc(RequireOwnership =false)]
    public void SpawnCursor(NetworkConnection conn)
    {
        NetworkObject nob = _networkManager.GetPooledInstantiated(_playerCursorPrefab, transform.parent.parent, true);
        _networkManager.ServerManager.Spawn(nob, conn);
        
        SetCursorRef(nob);

        Randomize();

        StartTest();

        UpdateIsHost(IsServerInitialized && IsOwner);
    }

    [ObserversRpc]
    private void UpdateIsHost(bool isHost)
    {
        _hostIcon.SetActive(isHost);
    }

    [ObserversRpc]
    private void SetCursorRef(NetworkObject nob)
    {
        Debug.Log("Set cursor ref for client " + Owner.ClientId);
        _currentCursor = nob.GetComponent<PlayerCursor>();
    }

    [ServerRpc]
    public void TestChangeServer(string playerName, Color newColor)
    {
        TestChange(playerName, newColor);
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
            yield return new WaitForSeconds(.5f);
        }
    }

    [ObserversRpc]
    public void TestChange(string playerName, Color newColor)
    {
        _userText.text = playerName;
        _bgImage.color = newColor;
        _currentCursor?.ChangeColor(newColor);
    }

    public void InitializePlayer()
    {
        _testButton = GetComponent<Button>();
        _testButton.onClick.AddListener(OnTestButtonClicked);
    }
}
