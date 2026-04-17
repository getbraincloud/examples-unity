using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks stats for the game.
/// Anytime the app increments a stat, this script will check if its in the list already
/// If it is already in the list then script will just increment the stat
/// </summary>

public class StatTracker : MonoBehaviour
{
    public static StatTracker Instance;

    private Dictionary<string, int> _stats = new Dictionary<string, int>();

    public static event Action<string, int> OnStatChanged;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Instance = this;
        _stats.Clear();
        
    }

    public void IncrementStat(string statName, int amount = 1)
    {
        if(!_stats.ContainsKey(statName))
        {
            _stats.Add(statName, amount);
        }
        else
        {
            _stats[statName] += amount;
        }
        OnStatChanged?.Invoke(statName, _stats[statName]);
    }
    
    public void SetStat(string statName, int amount)
    {
        _stats[statName] = amount;
        OnStatChanged?.Invoke(statName, _stats[statName]);
    }
    
    public int GetStat(string statName)
    {
        return _stats.ContainsKey(statName) ? _stats[statName] : 0;
    }
    
    public void ResetStat(string statName)
    {
        _stats[statName] = 0;
        OnStatChanged?.Invoke(statName, _stats[statName]);
    }

    public void ResetAllStats()
    {
        _stats.Clear();
    }
}
