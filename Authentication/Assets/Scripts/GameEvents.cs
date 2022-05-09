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

}
