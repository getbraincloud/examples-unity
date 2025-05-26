using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
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

    private PlaybackStreamRecord actionReplayRecord;

    private void Awake()
    {
        t = transform;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (actionReplayRecord != null)
            ChangeUsernameClientRpc(actionReplayRecord.username);
    }

    public void StartStream(PlaybackStreamRecord record, int skippedFrames = 0)
    {
        actionReplayRecord = record;
        t.position = Vector3.right * actionReplayRecord.startPosition;
        StartCoroutine(ActPlayBack(skippedFrames));
    }

    private IEnumerator ActPlayBack(int startFrame)
    {
        for (int ii = startFrame; ii < actionReplayRecord.frames.Count; ii++)
        {
            if (actionReplayRecord.frames[ii].xDelta != 0)
                MoveShip(actionReplayRecord.frames[ii].xDelta);

            if (actionReplayRecord.frames[ii].createBullet)
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
    private void SpawnVFXClientRpc(int vfxType, Vector3 spawnPosition)
    {
        switch (vfxType)
        {
            case 0:
                Instantiate(m_ExplosionParticleSystem, spawnPosition, Quaternion.identity);
                break;
            case 1:
                Instantiate(m_HitParticleSystem, spawnPosition, Quaternion.identity);
                break;
        }
    }

    [ClientRpc]
    private void ChangeUsernameClientRpc(string newName)
    {
        if (newName == null) return;
        if (newName.Length == 0) return;

        usernameText.text = newName;
    }
}
