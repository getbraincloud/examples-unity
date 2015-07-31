using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System;
using BrainCloudSlots.Connection;
using LitJson;

public class LoginPanel : MonoBehaviour
{
    public InputField EmailInput;
    public InputField PasswordInput;
    public GameObject AuthenticationPopup;
    public Text ErrorText;
    public GameObject PlayerPanel;
    public Toggle SavePassToggle;

    public Button LoginButton;

    private readonly string _emailPref = "n_email";
    private readonly string _passwordPref = "n_password";
    private readonly string _remeberPassPref = "n_rememberPass";
    private readonly string _cryptoKey = "256E2A0D09E748F2AC7F6FBF7057BCAE";

    void Start()
    {
        LoadIds();
        BrainCloudWrapper.Initialize();
    }

    private void Login()
    {
        SaveIds();
        BrainCloudWrapper.GetBC().AuthenticationService.AuthenticateEmailPassword(EmailInput.text, PasswordInput.text, true, AuthSucess, AuthFail);
        AuthenticationPopup.SetActive(true);
    }

    public void OnInputFieldChanged()
    {
        LoginButton.interactable = ValidateInput();
    }

    public void OnClickLoginButton()
    {
        ErrorText.text = "";
        Login();
    }

    public void AuthSucess(string responseData, object cbObject)
    {
        JsonData response = JsonMapper.ToObject(responseData);
        response = response["data"];
        if (response["newUser"].ToString() == "true")
        {
            //this is player's first login, create their user entity
            string userData = "{\"purchaseHistory\" : [], \"firstName\" : \"\", \"lastName\" : \"\", \"email\" : \"\", \"lifetimeWins\" : 0, \"biggestWin\" : {\"amount\" : 0, \"date\" : \""
                + new DateTime().ToShortDateString()
                + "\", \"time\" : \"" + new DateTime().ToShortTimeString()
                + "\"}, \"joinDateTime\" : {\"date\" : \""
                + DateTime.Now.ToShortDateString() + "\", \"time\" : \"" + DateTime.Now.ToShortTimeString() + "\"}}";

            BrainCloudWrapper.GetBC().EntityService.CreateEntity("userData", userData, null, CreateEntitySuccess, EntityFailure, null);
        }
        else
        {
            //this is a returning user, store their information
            BrainCloudWrapper.GetBC().EntityService.GetEntitiesByType("userData", GetEntitySuccess, EntityFailure, null);
        }
        BrainCloudWrapper.GetBC().PlayerStateService.UpdatePlayerName("TestUser");
        BrainCloudStats.Instance.m_userName = "TestUser";
        BrainCloudStats.Instance.ReadSlotsData();
        BrainCloudStats.Instance.ReadStatistics();
        BrainCloudWrapper.GetBC().GlobalEntityService.GetList("{ \"entityType\" : \"termsAndConditions\" }", "", 1, GetTermsEntitySuccess, EntityFailure, null);

        AuthenticationPopup.SetActive(false);
        PanelSwitcher.SwitchToPanel(Panel.PlayerSelect);
    }

    public void CreateEntitySuccess(string responseData, object cbObject)
    {
        BrainCloudWrapper.GetBC().EntityService.GetEntitiesByType("userData", GetEntitySuccess, EntityFailure, null);
    }

    public void GetEntitySuccess(string responseData, object cbObject)
    {
        JsonData response = JsonMapper.ToObject(responseData);
        response = response["data"];

        if (response["entities"].Count == 0)
        {

            string userData = "{\"purchaseHistory\" : [], \"firstName\" : \"\", \"lastName\" : \"\", \"email\" : \"\", \"lifetimeWins\" : 0, \"biggestWin\" : {\"amount\" : 0, \"date\" : \""
                + new DateTime().ToShortDateString()
                + "\", \"time\" : \"" + new DateTime().ToShortTimeString()
                + "\"}, \"joinDateTime\" : {\"date\" : \""
                + DateTime.Now.ToShortDateString() + "\", \"time\" : \"" + DateTime.Now.ToShortTimeString() + "\"}}";

            BrainCloudWrapper.GetBC().EntityService.CreateEntity("userData", userData, null, CreateEntitySuccess, EntityFailure, null);
            return;
        }
        BrainCloudStats.Instance.m_userData = response["entities"][0];
    }

    public void GetTermsEntitySuccess(string responseData, object cbObject)
    {
        JsonData response = JsonMapper.ToObject(responseData);
        response = response["data"];
        BrainCloudStats.Instance.m_termsConditionsString = response["entityList"][0]["data"]["text"].ToString();
    }

    public void EntityFailure(int a, int b, string errorData, object cbObject)
    {
        Debug.Log(a);
        Debug.Log(b);
        Debug.Log(errorData);
    }

    public void AuthFail(int statusCode, int reasonCode, string statusMessage, object cb)
    {
        if (reasonCode == BrainCloud.ReasonCodes.WRONG_EMAIL_AND_PASSWORD)
            ErrorText.text = "Invalid Password";
        else if (reasonCode == BrainCloud.ReasonCodes.CLIENT_NETWORK_ERROR_TIMEOUT)
            ErrorText.text = "Network Error - Could not connect";
        else
            ErrorText.text = statusMessage;

        AuthenticationPopup.SetActive(false);
    }

    public void OnToggleChange()
    {
        PlayerPrefs.SetInt(_remeberPassPref, SavePassToggle.isOn ? 2 : 1);
        PlayerPrefs.Save();
    }

    private bool ValidateInput()
    {
        //Email
        if (!Regex.IsMatch(EmailInput.text, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase))
            return false;

        //Password
        if (PasswordInput.text.Length <= 0)
            return false;

        return true;
    }

    private void LoadIds()
    {
        string encryptedPass = PlayerPrefs.GetString(_passwordPref);

        if (encryptedPass.Length > 0) PasswordInput.text = Crypto.DecryptStringAES(encryptedPass, _cryptoKey);
        EmailInput.text = PlayerPrefs.GetString(_emailPref);
        SavePassToggle.isOn = PlayerPrefs.GetInt(_remeberPassPref) > 1 ? true : false;

        OnInputFieldChanged();
    }

    private void SaveIds()
    {
        PlayerPrefs.SetString(_emailPref, EmailInput.text);
        if (SavePassToggle.isOn) PlayerPrefs.SetString(_passwordPref, Crypto.EncryptStringAES(PasswordInput.text, _cryptoKey));
        else PlayerPrefs.SetString(_passwordPref, "");
        PlayerPrefs.Save();
    }
}
