using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStat : MonoBehaviour
{
    //Data Members
    string playerStatName = "";
    long playerStatValue = 0;

    //UI Elements
    [SerializeField] Text statNameText;
    [SerializeField] Text statValueText;
    [SerializeField] Button incrementButton;

    public void SetStatName(string name)
    {
        playerStatName = name;
        statNameText.text = playerStatName;
    }

    public void SetStatNameText()
    {
        statNameText.text = playerStatName;
    }

    public string GetStatName()
    {
        return playerStatName; 
    }

    public void SetStatValue(long value)
    {
        playerStatValue = value;
        statValueText.text = playerStatValue.ToString();
    }

    public void SetStatValueText()
    {
        statValueText.text = playerStatValue.ToString();
    }

    public long GetStatValue()
    {
        return playerStatValue; 
    }

    public void OnIncrementStat()
    {
        BrainCloudInterface.instance.IncrementUserStats(playerStatName);
    }
}
