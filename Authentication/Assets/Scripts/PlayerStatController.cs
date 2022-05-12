using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatController : MonoBehaviour
{
    public PlayerStat playerStatPrefab;
    public GameObject prefabParent; 

    private void Awake()
    {
        for(int i = 0; i < 10; i++)
        {
            Instantiate(playerStatPrefab, prefabParent.transform);
        }
    }
}
