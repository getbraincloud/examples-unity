using FishNet;
using FishNet.Component.Spawning;
using FishNet.Object;
using FishNet.Transporting.Tugboat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BCFishNet
{
    public class ConnectionStarter : MonoBehaviour
    {
        private PlayerSpawner _playerSpawner;

        [SerializeField]
        private Transform _playerListContainer;

        [SerializeField]
        private GameObject _playerPrefab;

        [SerializeField]
        private Button _spawnButton;

        private Tugboat _t;

        private void Start()
        {
            _spawnButton.onClick.AddListener(OnSpawnClicked);

            if(TryGetComponent(out Tugboat t))
            {
                _t = t;
            #if UNITY_EDITOR && SNR
                if (ParrelSync.ClonesManager.IsClone())
                {
                    _t.StartConnection(false);
                }
                else
            #endif
                {
                    _t.StartConnection(true);
                    _t.StartConnection(false);
                }
            }
               
            if (TryGetComponent(out PlayerSpawner _s))
            {
                _playerSpawner = _s;
                _playerSpawner.OnSpawned += (NetworkObject netObj) =>
                {
                    Debug.LogWarning("PLAYER OBJECT SPAWNED");
                    netObj.transform.SetParent(_playerListContainer, false);
                    netObj.transform.localScale = Vector3.one;
                };
            }
            else
            {
                Debug.LogError("Couldn't get player spawner", this);
            }
        }

        private void OnSpawnClicked()
        {
            Debug.Log("Spawning player object");
            GameObject go = Instantiate(_playerPrefab, _playerListContainer);
            InstanceFinder.ServerManager.Spawn(go, InstanceFinder.ClientManager.Connection);
        }
    }
}