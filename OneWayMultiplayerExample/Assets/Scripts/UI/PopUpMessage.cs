using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PopUpMessage : MonoBehaviour
{
    public TMP_Text Message;

    public void SetUpPopUpMessage(string message)
    {
        Cursor.visible = true;
        MenuManager.Instance.LoadingMenuState.CancelNextState = true;
        Message.text = message;
        gameObject.SetActive(true);
    }

    public void SetUpConfirmationForMatch()
    {
        Cursor.visible = true;
        Message.text = $"Are you sure you want to invade {GameManager.Instance.OpponentUserInfo.Username} ?";
        
        gameObject.SetActive(true);
    }

    public void AcceptGame()
    {
        BrainCloudManager.Instance.StartMatch();
    }
}