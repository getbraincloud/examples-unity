using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class PlayerReplayControl : NetworkBehaviour
{
    [Header("Health")]
    private int lives = 3;
    private bool alive = true;
    [SerializeField]
    private ParticleSystem m_ExplosionParticleSystem;
    [SerializeField]
    private ParticleSystem m_HitParticleSystem;
    [SerializeField]
    private TextMeshPro usernameText;

    [Header("Weapon Settings")]
    public GameObject bulletPrefab;

    [Header("Movement Settings")]
    [SerializeField]
    private float m_MoveSpeed = 3.5f;
    private Transform t;

    private GameObject m_MyBullet;

    private int replayIndex;
    private PlaybackStreamRecord _actionReplayRecords;

    private void Awake()
    {
        t = transform;
    }

    public void StartStream(PlaybackStreamRecord record, int skippedFrames = 0)
    {
        _actionReplayRecords = record;
        if(_actionReplayRecords.username != string.Empty) usernameText.text = _actionReplayRecords.username;
        t.position = Vector3.right * _actionReplayRecords.startPosition;
        StartCoroutine(StartPlayBack(skippedFrames));
    }

    private IEnumerator StartPlayBack(int startFrame)
    {
        for (int ii = startFrame; ii < _actionReplayRecords.frames.Count; ii++)
        {
            if (_actionReplayRecords.frames[ii].xDelta != 0)
                MoveShip(_actionReplayRecords.frames[ii].xDelta);

            if (_actionReplayRecords.frames[ii].createBullet)
                ShootBullet();

            yield return new WaitForFixedUpdate();
        }
        StartRetreat();
    }

    public void StartRetreat()
    {
        StopAllCoroutines();
        StartCoroutine(Retreat());
    }

    private IEnumerator Retreat()
    {
        while (t.position.y > -10)
        {
            t.position = Vector3.MoveTowards(t.position, t.position + Vector3.down, m_MoveSpeed * Time.deltaTime);
            yield return 0;
        }
        Destroy(gameObject);
    }

    private void MoveShip(float deltaX)
    {
        var newMovement = new Vector3(deltaX, 0, 0);
        t.position = Vector3.MoveTowards(t.position, t.position + newMovement, m_MoveSpeed * Time.deltaTime);
    }

    private void ShootBullet()
    {
        m_MyBullet = Instantiate(bulletPrefab, transform.position + Vector3.up, Quaternion.identity);
        m_MyBullet.GetComponent<NetworkObject>().Spawn();
    }

    public void HitByBullet()
    {
        if (!alive) return;

        lives -= 1;

        if (lives <= 0)
        {
            alive = false;
            StopAllCoroutines();
            SpawnVFXClientRpc(0, transform.position);
            NetworkObject.Despawn();
        }
        else
        {
            SpawnVFXClientRpc(1, transform.position);
        }
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
