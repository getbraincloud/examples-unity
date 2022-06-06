using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GlobalStat : MonoBehaviour
{
    //Data Members
    string globalStatName = "";
    long globalStatValue = 0;

    //UI Elements
    [SerializeField] Text statNameText;
    [SerializeField] Text statValueText;
    [SerializeField] Button incrementButton;

    public void SetStatName(string name)
    {
        globalStatName = name;
        statNameText.text = globalStatName;
    }

    public string GetStatName()
    {
        return globalStatName;
    }

    public void SetStatValue(long value)
    {
        globalStatValue = value;
        statValueText.text = globalStatValue.ToString();
    }

    public long GetStatValue()
    {
        return globalStatValue;
    }

    public void OnIncrementStat()
    {
        BrainCloudInterface.instance.IncrementGlobalStats(globalStatName);
    }
}
