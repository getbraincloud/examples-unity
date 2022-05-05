using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    public static ScreenManager instance; 

    [SerializeField] GameObject ConnectScreen;
    [SerializeField] GameObject MainScreen;

    void Start()
    {
        instance = this;

        DontDestroyOnLoad(this); 
    }

    public void ActivateConnectScreen()
    {
        MainScreen.SetActive(false);
        ConnectScreen.SetActive(true); 
    }

    public void ActivateMainScreen()
    {
        ConnectScreen.SetActive(false);
        MainScreen.SetActive(true); 
    }
}
