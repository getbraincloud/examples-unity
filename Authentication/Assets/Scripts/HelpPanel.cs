using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HelpPanel : MonoBehaviour
{
    public string helpTitle { get; set; }
    public string helpMessage { get; set; }
    public string apiURL { get; set; }

    [SerializeField] Button exitButton;
    [SerializeField] Button apiURLButton; 
    [SerializeField] Text helpMessageText;
    [SerializeField] Text helpTitleText; 

    public void SetHelpPanel(string title, string message, string url)
    {
        helpTitle = title;
        helpMessage = message;
        apiURL = url;

        helpTitleText.text = helpTitle;
        helpMessageText.text = helpMessage; 
    }

    public void OnExitClick()
    {
        gameObject.SetActive(false); 
    }

    public void OnAPIClick()
    {
        Application.OpenURL(apiURL); 
    }
}
