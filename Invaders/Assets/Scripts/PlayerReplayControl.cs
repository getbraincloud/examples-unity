using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerReplayControl : NetworkBehaviour
{
    [Header("Weapon Settings")]
    public GameObject bulletPrefab;

    [Header("Movement Settings")]
    [SerializeField]
    private float m_MoveSpeed = 3.5f;

    private GameObject m_MyBullet;

    private int replayIndex;
    private PlaybackStreamReadData _actionReplayRecords;

    private void FixedUpdate()
    {
        
    }

    public void StartStream(PlaybackStreamReadData record)
    {
        _actionReplayRecords = record;
        StartCoroutine(StartPlayBack());
    }

    private IEnumerator StartPlayBack()
    {
        for(int ii = 0; ii < _actionReplayRecords.totalFrameCount; ii++)
        {
            if (_actionReplayRecords.frames[ii].xDelta != 0)
                MoveShip(_actionReplayRecords.frames[ii].xDelta);

            if (_actionReplayRecords.frames[ii].createBullet)
                ShootBullet();

            yield return new WaitForFixedUpdate();
        }
        Transform t = transform;
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
        Transform t = transform;
        t.position = Vector3.MoveTowards(t.position, t.position + newMovement, m_MoveSpeed * Time.deltaTime);
    }

    private void ShootBullet()
    {
        m_MyBullet = Instantiate(bulletPrefab, transform.position + Vector3.up, Quaternion.identity);
        m_MyBullet.GetComponent<NetworkObject>().Spawn();
    }
}
