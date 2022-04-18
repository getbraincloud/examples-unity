using UnityEngine;
using TMPro;

public class ErrorMessage : MonoBehaviour
{
    public TMP_Text Message;

    public void SetUpPopUpMessage(string message)
    {
        Cursor.visible = true;
        MenuManager.Instance.LoadingMenuState.CancelNextState = true;
        Message.text = message;
        gameObject.SetActive(true);
    }
}