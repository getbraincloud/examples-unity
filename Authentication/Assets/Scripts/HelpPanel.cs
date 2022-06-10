using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HelpPanel : MonoBehaviour
{
    public string HelpTitle { get; set; }
    public string HelpMessage { get; set; }
    public string ApiURL { get; set; }

    [SerializeField] Button exitButton;
    [SerializeField] Button apiURLButton; 
    [SerializeField] Text helpMessageText;
    [SerializeField] Text helpTitleText; 

    public void SetHelpPanel(string title, string message, string url)
    {
        HelpTitle = title;
        HelpMessage = message;
        ApiURL = url;

        helpTitleText.text = HelpTitle;
        helpMessageText.text = HelpMessage; 
    }

    public void OnExitClick()
    {
        gameObject.SetActive(false); 
    }

    public void OnAPIClick()
    {
        Application.OpenURL(ApiURL); 
    }
}
