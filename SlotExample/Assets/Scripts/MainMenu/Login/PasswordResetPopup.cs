using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class PasswordResetPopup : MonoBehaviour
{
    public InputField MainEmailInput;
    public InputField PopupEmailInput;
    public GameObject StatusPopup;
    public Text StatusText;
    public Text ErrorText;

    public Button SendButton;

    void OnEnable()
    {
        PopupEmailInput.text = MainEmailInput.text;
        StatusText.text = "";
        ErrorText.text = "";
        StatusPopup.SetActive(false);

        OnInputFieldChanged();
    }

    public void OnInputFieldChanged()
    {
        SendButton.interactable = ValidateInput();
    }

    public void OnClickForgotPassword()
    {
        gameObject.SetActive(true);
    }

    public void OnClickCancel()
    {
        gameObject.SetActive(false);
    }

    public void OnClickSend()
    {
        MainEmailInput.text = PopupEmailInput.text;
        BrainCloudWrapper.GetBC().AuthenticationService.ResetEmailPassword(PopupEmailInput.text, SendSucess, SendFail);
        StatusText.text = "Sending...";
        ErrorText.text = "";
        StatusPopup.SetActive(true);
    }

    private void SendFail(int status, int reasonCode, string statusMessage, object cbObject)
    {
        StatusText.text = "";
        if (reasonCode == BrainCloud.ReasonCodes.SECURITY_ERROR)
            ErrorText.text = "Error: User does not exist";
        else if (reasonCode == BrainCloud.ReasonCodes.CLIENT_NETWORK_ERROR_TIMEOUT)
            ErrorText.text = "Network Error - Could not connect";
        else
            ErrorText.text = "Error sending reset email";
        StartCoroutine(WaitToReturn(3f, false));
    }

    private void SendSucess(string jsonResponse, object cbObject)
    {
        StatusText.text = "Email Sent!";
        StartCoroutine(WaitToReturn(2f, true));
    }

    private IEnumerator WaitToReturn(float seconds, bool exitPopup)
    {
        yield return new WaitForSeconds(1f);
        StatusPopup.SetActive(false);
        gameObject.SetActive(!exitPopup);
    }

    private bool ValidateInput()
    {
        //Email
        if (!Regex.IsMatch(PopupEmailInput.text, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase))
        {
            ErrorText.text = "Please enter a valid email address";
            return false;
        }
        return true;
    }
}
