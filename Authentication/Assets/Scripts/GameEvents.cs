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

    //User Entity Events
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

    //Custom Entity Events
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
