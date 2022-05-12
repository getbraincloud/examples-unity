using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScreenManager : MonoBehaviour
{
    public static ScreenManager instance; 

    [SerializeField] GameObject ConnectScreen;
    [SerializeField] GameObject MainScreen;
    private EventSystem _eventSystem;

    void Start()
    {
        instance = this;
        _eventSystem = EventSystem.current;
        DontDestroyOnLoad(this); 
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Selectable next = _eventSystem.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();

            if (next != null)
            {
                InputField inputfield = next.GetComponent<InputField>();
                if (inputfield != null)
                {
                    //if it's an input field, also set the text caret
                    inputfield.OnPointerClick(new PointerEventData(_eventSystem));
                }
                _eventSystem.SetSelectedGameObject(next.gameObject, new BaseEventData(_eventSystem));
            }
        }
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
