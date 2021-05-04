using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogueMessage : MonoBehaviour
{
    public TMP_Text Message;

    public void SetUpPopUpMessage(string message)
    {
        Message.text = message;
        gameObject.SetActive(true);
        Debug.Log("Enabled");
    }
}
