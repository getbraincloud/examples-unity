using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionReplay : MonoBehaviour
{
    private TroopAI _troop;
    private List<ActionReplayRecord> _actionReplayRecords = new List<ActionReplayRecord>();
    private float currentReplayIndex;
    private float indexChangeRate;
    private bool isInReplayMode;

    private void Awake()
    {
        _troop = GetComponent<TroopAI>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (isInReplayMode)
            {
                SetTransform(0);
            }
            else
            {
                SetTransform(_actionReplayRecords.Count - 1);
            }
        }

        indexChangeRate = 0;
        if (Input.GetKey(KeyCode.RightArrow))
        {
            indexChangeRate = 1;
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            indexChangeRate = -1;
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            indexChangeRate *= 0.5f;
        }
    }

    private void FixedUpdate()
    {
        if (isInReplayMode == false)
        {
            // What do    
        }
        else
        {
            float nextIndex = currentReplayIndex + indexChangeRate;
            if (nextIndex < _actionReplayRecords.Count && nextIndex >= 0)
            {
                SetTransform(nextIndex);    
            }
        }
    }

    private void SetTransform(float index)
    {
        currentReplayIndex = index;
        
        ActionReplayRecord actionReplayRecord = _actionReplayRecords[(int)index];

        transform.position = actionReplayRecord.position;
        //health = actionReplayRecord.health;
        //etc
    }
}
