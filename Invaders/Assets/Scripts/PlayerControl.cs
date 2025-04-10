using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.VirtualTexturing;
using Random = UnityEngine.Random;

public class PlayerControl : NetworkBehaviour
{
    [Header("Weapon Settings")]
    public GameObject bulletPrefab;
    private GameObject m_MyBullet;
    private bool shotRecently = false;
    private float timeSinceShot = 0.0f;
    private readonly float shootCooldown = 0.5f;

    [Header("Movement Settings")]
    [SerializeField]
    private float m_MoveSpeed = 3.5f;

    [Header("Player Settings")]
    [SerializeField]
    private NetworkVariable<int> m_Lives = new NetworkVariable<int>(3);

    [SerializeField]
    ParticleSystem m_ExplosionParticleSystem;
    [SerializeField]
    ParticleSystem m_HitParticleSystem;

    [SerializeField]
    Color m_PlayerColorInGame;

    private bool m_HasGameStarted;

    private bool m_IsAlive = true;

    private NetworkVariable<int> m_MoveX = new NetworkVariable<int>(0);

    private ClientRpcParams m_OwnerRPCParams;

    [SerializeField]
    private SpriteRenderer m_PlayerVisual;
    private NetworkVariable<int> m_Score = new NetworkVariable<int>(0);
    public int Score
    {
        get => m_Score.Value;
    }

    public bool IsAlive => m_Lives.Value > 0;

    private string m_playerName;
    public string PlayerName
    {
        get => m_playerName;
        set => m_playerName = value;
    }
    public bool IsDedicatedServer
    {
        get => BrainCloudManager.Singleton.IsDedicatedServer;
    }

    private PlaybackStreamRecord record;
    private int currentRecordFrame = 0;
    private float previousPos = 0;
    private bool finishedRecording = false;

    private void Awake()
    {
        m_HasGameStarted = false;
        record = new PlaybackStreamRecord();
        record.frames.Add(new PlaybackStreamFrame(0));
    }

    private void Start()
    {
        PlayerName = BrainCloudManager.Singleton.LocalUserInfo.Username;
        transform.position = Vector3.right * Random.Range(-40, 40) / 10f;
        record.startPosition = transform.position.x;
        record.username = PlayerName;
    }

    private void Update()
    {
        switch (SceneTransitionHandler.sceneTransitionHandler.GetCurrentSceneState())
        {
            case SceneTransitionHandler.SceneStates.Ingame:
            {
                InGameUpdate();
                break;
            }
        }
    }

    // This is the only function that is guarunteed to be called client side when the game ends. Other functions are inconsistently called.
    override public void OnDestroy()
    {
        EndRecording();
        base.OnDestroy();
    }

    private void FixedUpdate()
    {
        if (IsLocalPlayer)
        {
            int dx;
            if (Mathf.Abs(transform.position.x - previousPos) < 0.01f) dx = 0;
            else dx = Math.Sign(transform.position.x - previousPos);

            record.frames.Add(new PlaybackStreamFrame(dx, shotRecently, currentRecordFrame));
            shotRecently = false;
            currentRecordFrame += 1;
        }

        timeSinceShot += 1.0f * Time.deltaTime;
        previousPos = transform.position.x;
    }

    private void LateUpdate()
    {
        HandleCameraMovement();
    }

    private void EndRecording()
    {
        if (IsLocalPlayer && !finishedRecording)
        {
            finishedRecording = true;
            if (record.totalFrameCount == -2)
            {
                record.totalFrameCount = currentRecordFrame;
            }
            PlaybackSaver.Singleton.StartSubmittingRecord(Score, record);
        }
    }

    private void HandleCameraMovement()
    {
        Vector3 cameraPosition = transform.position;
        cameraPosition.x *= 0.05f;
        cameraPosition.y = 7.2f;
        cameraPosition.z = -35f;
        Camera.main.transform.position = cameraPosition;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsClient)
        {
            m_Lives.OnValueChanged -= OnLivesChanged;
            m_Score.OnValueChanged -= OnScoreChanged;
        }

        if (InvadersGame.Singleton)
        {
            InvadersGame.Singleton.isGameOver.OnValueChanged -= OnGameStartedChanged;
            InvadersGame.Singleton.hasGameStarted.OnValueChanged -= OnGameStartedChanged;
        }
    }

    private void SceneTransitionHandler_sceneStateChanged(SceneTransitionHandler.SceneStates newState)
    {
        UpdateColor();
    }

    private void UpdateColor()
    {
        if (SceneTransitionHandler.sceneTransitionHandler.GetCurrentSceneState() == SceneTransitionHandler.SceneStates.Ingame)
        {
            if (m_PlayerVisual != null) m_PlayerVisual.color = m_PlayerColorInGame;
        }
        else
        {
            if (m_PlayerVisual != null) m_PlayerVisual.color = Color.clear;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Bind to OnValueChanged to display in log the remaining lives of this player
        // And to update InvadersGame singleton client-side
        m_Lives.OnValueChanged += OnLivesChanged;
        m_Score.OnValueChanged += OnScoreChanged;

        if (IsDedicatedServer) m_OwnerRPCParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } } };

        if (!InvadersGame.Singleton)
            InvadersGame.OnSingletonReady += SubscribeToDelegatesAndUpdateValues;
        else
            SubscribeToDelegatesAndUpdateValues();

        SceneTransitionHandler.sceneTransitionHandler.OnSceneStateChanged += SceneTransitionHandler_sceneStateChanged;
        UpdateColor();
    }

    private void SubscribeToDelegatesAndUpdateValues()
    {
        InvadersGame.Singleton.hasGameStarted.OnValueChanged += OnGameStartedChanged;
        InvadersGame.Singleton.isGameOver.OnValueChanged += OnGameStartedChanged;

        if (IsClient && IsOwner)
        {
            InvadersGame.Singleton.SetScore(m_Score.Value);
            InvadersGame.Singleton.SetLives(m_Lives.Value);
        }

        m_HasGameStarted = InvadersGame.Singleton.hasGameStarted.Value;
    }

    public void IncreasePlayerScore(int amount)
    {
        Assert.IsTrue(IsDedicatedServer, "IncreasePlayerScore should be called server-side only");
        if (finishedRecording) return;
        m_Score.Value += amount;
    }

    private void OnGameStartedChanged(bool previousValue, bool newValue)
    {
        m_HasGameStarted = newValue;
    }

    private void OnLivesChanged(int previousAmount, int currentAmount)
    {
        // Hide graphics client side upon death
        if (currentAmount <= 0 && IsClient)
            m_PlayerVisual.enabled = false;

        if (!IsOwner) return;
        if (InvadersGame.Singleton != null) InvadersGame.Singleton.SetLives(m_Lives.Value);
        if(BrainCloud.Plugin.Interface.EnableLogging) Debug.LogFormat("Lives {0} ", currentAmount);

        if (m_Lives.Value <= 0)
        {
            m_IsAlive = false;
            EndRecording();
        }
    }

    private void OnScoreChanged(int previousAmount, int currentAmount)
    {
        if (!IsOwner) return;
        if (finishedRecording) return;
        if (InvadersGame.Singleton != null) InvadersGame.Singleton.SetScore(m_Score.Value);
    } // ReSharper disable Unity.PerformanceAnalysis

    private void InGameUpdate()
    {
        if (!IsLocalPlayer || !IsOwner || !m_HasGameStarted) return;
        if (!m_IsAlive) return;

        var deltaX = 0;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) deltaX -= 1;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) deltaX += 1;

        if (deltaX != 0)
        {
            var newMovement = new Vector3(deltaX, 0, 0);
            transform.position = Vector3.MoveTowards(transform.position,
                transform.position + newMovement, m_MoveSpeed * Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (timeSinceShot >= shootCooldown)
            {
                timeSinceShot = 0;
                shotRecently = true;
                ShootServerRPC();
            }
        }
    }

    [ServerRpc]
    private void ShootServerRPC()
    {
        if (timeSinceShot < shootCooldown) return;

        m_MyBullet = Instantiate(bulletPrefab, transform.position + Vector3.up, Quaternion.identity);
        m_MyBullet.GetComponent<PlayerBullet>().owner = this;
        m_MyBullet.GetComponent<NetworkObject>().Spawn();
        timeSinceShot = 0;
    }

    public void HitByBullet()
    {
        Assert.IsTrue(IsDedicatedServer, "HitByBullet must be called server-side only!");
        if (!m_IsAlive) return;

        m_Lives.Value -= 1;

        if (m_Lives.Value <= 0)
        {
            // gameover!
            m_IsAlive = false;
            m_MoveX.Value = 0;
            m_Lives.Value = 0;
            //InvadersGame.Singleton.SetGameEnd(GameOverReason.Death);
            //NotifyGameOverClientRpc(GameOverReason.Death, m_OwnerRPCParams);
            SpawnVFXClientRpc(0, transform.position);

            // Hide graphics of this player object server-side. Note we don't want to destroy the object as it
            // may stop the RPC's from reaching on the other side, as there is only one player controlled object
            m_PlayerVisual.enabled = false;
        }
        else
        {
            SpawnVFXClientRpc(1, transform.position);
        }
    }

    [ClientRpc]
    public void NotifyGameOverClientRpc(GameOverReason reason)
    {
        NotifyGameOver(reason);
    }

    /// <summary>
    /// This should only be called locally, either through NotifyGameOverClientRpc or through the InvadersGame.BroadcastGameOverReason
    /// </summary>
    /// <param name="reason"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void NotifyGameOver(GameOverReason reason)
    {
        Assert.IsTrue(IsLocalPlayer);
        m_HasGameStarted = false;
        switch (reason)
        {
            case GameOverReason.None:
                InvadersGame.Singleton.DisplayGameOverText("You have lost! Unknown reason!");
                break;
            case GameOverReason.EnemiesReachedBottom:
                InvadersGame.Singleton.DisplayGameOverText("You have lost! The enemies have invaded you!");
                break;
            case GameOverReason.Death:
                InvadersGame.Singleton.DisplayGameOverText("You have lost! Your health was depleted!");
                break;
            case GameOverReason.Max:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(reason), reason, null);
        }
        EndRecording();
    }

    public PlaybackStreamRecord GetRecord()
    {
        return record;
    }

    [ClientRpc]
    void SpawnVFXClientRpc(int vfxType, Vector3 spawnPosition)
    {
        if (vfxType == 0)
            Instantiate(m_ExplosionParticleSystem, spawnPosition, Quaternion.identity);
        else if (vfxType == 1)
            Instantiate(m_HitParticleSystem, spawnPosition, Quaternion.identity);
    }
}
