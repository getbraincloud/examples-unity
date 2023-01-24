using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PopUpMessage : MonoBehaviour
{
    public TMP_Text Message;

    public void SetUpPopUpMessage(string message)
    {
        if (SceneManager.GetActiveScene().name.Contains("Game"))
        {
            Debug.LogError("Failure callback during gameplay, message: " + message);
            return;
        }
        
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
        NetworkManager.Instance.StartMatch();
    }
}