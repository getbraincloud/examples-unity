using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameEvents : MonoBehaviour
{
    public static GameEvents instance; 

    void Awake()
    {
        instance = this; 
    }

    //*************** User Entity Events ***************
    public event Action onCreateUserEntitySuccess;
    public void CreateUserEntitySuccess()
    {
        if(onCreateUserEntitySuccess != null)
        {
            onCreateUserEntitySuccess(); 
        }
    }

    public event Action onDeleteUserEntitySuccess; 
    public void DeleteUserEntitySuccess()
    {
        if(onDeleteUserEntitySuccess != null)
        {
            onDeleteUserEntitySuccess(); 
        }
    }

    public event Action onGetUserEntityPageSuccess;
    public void GetUserEntityPageSuccess()
    {
        if(onGetUserEntityPageSuccess != null)
        {
            onGetUserEntityPageSuccess();
        }
    }


    //*************** Custom Entity Events ***************
    public event Action onCreateCustomEntitySuccess; 
    public void CreateCustomEntitySuccess()
    {
        if (onCreateCustomEntitySuccess != null)
        {
            onCreateCustomEntitySuccess();
        }
    }

    public event Action onDeleteCustomEntitySuccess;
    public void DeleteCustomEntitySuccess()
    {
        if(onDeleteCustomEntitySuccess != null)
        {
            onDeleteCustomEntitySuccess(); 
        }
    }


    //*************** XP Events ***************
    public event Action OnUpdateLevelAndXP;
    public void UpdateLevelAndXP()
    {
        if(OnUpdateLevelAndXP != null)
        {
            OnUpdateLevelAndXP();
        }
    }


    //*************** Virtual Currency Events ***************

    public event Action onGetVirtualCurrency;
    public void GetVirtualCurrency()
    {
        if(onGetVirtualCurrency != null)
        {
            onGetVirtualCurrency();
        }
    }


    //*************** Player Stat Events ***************

    public event Action/*<string>*/ onIncrementUserStat;
    public void IncrementUserStat(/*string statName*/)
    {
        if(onIncrementUserStat != null)
        {
            onIncrementUserStat(/*statName*/);
        }
    }

    public event Action onInstantiatePlayerStats;
    public void InstantiatePlayerStats()
    {
        if(onInstantiatePlayerStats != null)
        {
            onInstantiatePlayerStats(); 
        }
    }

    //*************** Global Stat Events ***************

    public event Action<string> onIncrementGlobalStat;
    public void IncrementGlobalStat(string statName)
    {
        if(onIncrementGlobalStat != null)
        {
            onIncrementGlobalStat(statName);
        }
    }
}
