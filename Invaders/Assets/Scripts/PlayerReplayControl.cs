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
    private PlayerControl creditedPlayer;

    private int replayIndex;
    private PlaybackStreamRecord _actionReplayRecords;

    public void StartStream(PlayerControl player, PlaybackStreamRecord record)
    {
        Debug.Log("Starting ghost replay");
        creditedPlayer = player;
        _actionReplayRecords = record;
        StartCoroutine(StartPlayBack());
    }

    private IEnumerator StartPlayBack()
    {
        replayIndex = 0;
        while(replayIndex < _actionReplayRecords.totalFrameCount)
        {
            if (_actionReplayRecords.frames[replayIndex].xDelta != 0)
                MoveShip(_actionReplayRecords.frames[replayIndex].xDelta);

            if (_actionReplayRecords.frames[replayIndex].createBullet)
                ShootBullet();

            replayIndex += 1;
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
