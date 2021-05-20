using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogueMessage : MonoBehaviour
{
    public TMP_Text Message;

    public void SetUpPopUpMessage(string message)
    {
        Cursor.visible = true;
        StateManager.Instance.LoadingGameState.CancelNextState = true;
        Message.text = message;
        gameObject.SetActive(true);
    }
}
