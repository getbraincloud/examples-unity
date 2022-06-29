using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using BrainCloud.Common;
using BrainCloud.JsonFx.Json;

public class AuthenticateHandler : MonoBehaviour
{
    public GameObject MainScene;
    public BCConfig BCConfig;

    public static BrainCloudWrapper _bc;

    enum eAuthTypes {
        ANONYMOUS,
        UNIVERSAL,
        EMAIL,
        GOOGLE,
        FACEBOOK,
        XBOXLIVE
    };

    bool useAdvancedAuthentication = false;
    AuthenticationType selectedAdvancedAuthType;

    eAuthTypes currentAuthType = eAuthTypes.ANONYMOUS;

    string inputProfileId = "";
    string inputPassword = "";
    string inputEntityName;
    string inputEntityAge;

    int statusTextInitSize;
    const int STATUS_SHRINK_SIZE = 8;

    const string GOOGLE_AUTH_INFO = "Only for Android";
    const string FACEBOOK_AUTH_INFO = "Only for WEBGL";

    //UI elements set in editor.
    [SerializeField] Text statusText;
    [SerializeField] Text profileIdText;
    [SerializeField] Text passwordText;
    [SerializeField] GameObject profileIdFieldObject;
    [SerializeField] GameObject passwordFieldObject;
    [SerializeField] GameObject entityNameField;
    [SerializeField] GameObject entityAgeField;
    [SerializeField] GameObject entityNameText;
    [SerializeField] GameObject entityAgeText;
    [SerializeField] GameObject advAuthInfoText;
    [SerializeField] Toggle advAuthToggle;
    [SerializeField] Text extraAuthInfoText;
    [SerializeField] GameObject resetProfileIdButton;
    [SerializeField] GameObject resetAnonIdButton;
    [SerializeField] Text storedProfileIdText;
    [SerializeField] Text storedAnonymousIdText;
    [SerializeField] GameObject googleIdText;
    [SerializeField] GameObject serverAuthCodeText;
    [SerializeField] GameObject googleSignInButton;
    [SerializeField] Dropdown authDropdown;

    //Save Data fields
    private const string SAVE_FILE_NAME = "AuthTypeSaveData.json";
    private string path = "";

    class UserData {

        public string profileId = "";
        public string anonId = "";
        public eAuthTypes lastAuthType;

        public UserData(eAuthTypes authType) { lastAuthType = authType; } 

    }

    //Save Data Helpers
    private void SetPath()
    {
        path = Application.dataPath + Path.AltDirectorySeparatorChar + SAVE_FILE_NAME;

        //Uncomment line below if persistent save path is preferred.
        //path = Application.persistentDataPath + Path.AltDirectorySeparatorChar + PATH_TO_SAVE; 
    }

    public void SaveAuthData()
    {
        string savePath = path;

        UserData data = new UserData(currentAuthType); 

        string json = JsonUtility.ToJson(data); 
        Debug.Log("Saving authentication data to " + savePath);

        using StreamWriter writer = new StreamWriter(savePath);
        writer.Write(json); 
    }

    public void LoadAuthData()
    {
        try
        {
            using StreamReader reader = new StreamReader(path);
            string json = reader.ReadToEnd();

            UserData data = JsonUtility.FromJson<UserData>(json);

            if (data != null)
            {
                currentAuthType = data.lastAuthType;

                Debug.Log("Loading data from " + path); 
            }
        }
        catch
        {
            Debug.Log( SAVE_FILE_NAME + " Could not be found at " + path + "\nAuth Save data will be created on your next authentication."); 
        }
    }

    void Start()
    {
        SetPath(); 
        LoadAuthData();

        if(currentAuthType == 0)
        {
            ChangeAuthType(currentAuthType);
        }
        else
        {
            authDropdown.value = (int) currentAuthType; 
        }

        statusTextInitSize = statusText.fontSize;

        SetPath(); 
    }

    private void OnEnable()
    {
        statusText.fontSize = statusTextInitSize;
        statusText.text = "Welcome to brainCloud";

        if (BrainCloudInterface.instance == null)
            return;
        
        storedProfileIdText.text = BrainCloudInterface.instance.GetStoredProfileID();
        storedAnonymousIdText.text = BrainCloudInterface.instance.GetStoredAnonymousID();
        
    }

    void ChangeAuthType(eAuthTypes authType)
    {
        switch(authType)
        {
            case eAuthTypes.EMAIL:
                selectedAdvancedAuthType = AuthenticationType.Email;
                SetScreen("Email", "Password", authType);
                advAuthToggle.interactable = true;
                break;
            case eAuthTypes.UNIVERSAL:
                selectedAdvancedAuthType = AuthenticationType.Universal;
                SetScreen("User ID", "Password", authType);
                advAuthToggle.interactable = true;
                break;
            case eAuthTypes.ANONYMOUS:
                selectedAdvancedAuthType = AuthenticationType.Anonymous;
                SetScreen("Profile ID", "Anonymous", authType);
                advAuthToggle.interactable = true;
                break;
            case eAuthTypes.GOOGLE:
                selectedAdvancedAuthType = AuthenticationType.Google;
                SetScreen("Profile ID", "Anonymous", authType);
                advAuthToggle.interactable = false;
                break;
            case eAuthTypes.FACEBOOK:
                selectedAdvancedAuthType = AuthenticationType.Facebook;
                SetScreen("Profile ID", "Anonymous", authType);
                advAuthToggle.interactable = false;
                break;
            case eAuthTypes.XBOXLIVE:
                SetScreen("Profile ID", "Anonymous", authType);
                advAuthToggle.interactable = false;
                break;
        }
    }

    void SetScreen(string profileIdInfo, string passwordInfo, eAuthTypes authType)
    {
        profileIdText.text = profileIdInfo + ":";
        passwordText.text = passwordInfo + ":";

        ActivateAdvancedAuthFields();

        //Deactivating non-constant elements first to make method simpler.
        profileIdFieldObject.SetActive(false);
        passwordFieldObject.gameObject.SetActive(false);
        resetProfileIdButton.SetActive(false);
        resetAnonIdButton.SetActive(false);
        storedProfileIdText.gameObject.SetActive(false);
        storedAnonymousIdText.gameObject.SetActive(false);
        googleIdText.SetActive(false);
        serverAuthCodeText.SetActive(false);
        googleSignInButton.SetActive(false);
        extraAuthInfoText.text = "";

        switch (authType)
        {
            case eAuthTypes.EMAIL:
                profileIdFieldObject.SetActive(true);
                passwordFieldObject.gameObject.SetActive(true);
                break;
            case eAuthTypes.UNIVERSAL:
                profileIdFieldObject.SetActive(true);
                passwordFieldObject.gameObject.SetActive(true);
                break;
            case eAuthTypes.ANONYMOUS:
                resetProfileIdButton.SetActive(true);
                resetAnonIdButton.SetActive(true);
                storedProfileIdText.gameObject.SetActive(true);
                storedAnonymousIdText.gameObject.SetActive(true);
                storedProfileIdText.text = BrainCloudInterface.instance.GetStoredProfileID();
                storedAnonymousIdText.text = BrainCloudInterface.instance.GetStoredAnonymousID();
                break;
            case eAuthTypes.GOOGLE:
                resetProfileIdButton.SetActive(true);
                resetAnonIdButton.SetActive(true);
                storedProfileIdText.gameObject.SetActive(true);
                storedAnonymousIdText.gameObject.SetActive(true);
                storedProfileIdText.text = BrainCloudInterface.instance.GetStoredProfileID();
                storedAnonymousIdText.text = BrainCloudInterface.instance.GetStoredAnonymousID();
                googleIdText.SetActive(true);
                serverAuthCodeText.SetActive(true);
                googleSignInButton.SetActive(true);
                advAuthToggle.isOn = false;
                extraAuthInfoText.text = GOOGLE_AUTH_INFO;
                break;
            case eAuthTypes.FACEBOOK:
                resetProfileIdButton.SetActive(true);
                resetAnonIdButton.SetActive(true);
                storedProfileIdText.gameObject.SetActive(true);
                storedAnonymousIdText.gameObject.SetActive(true);
                storedProfileIdText.text = BrainCloudInterface.instance.GetStoredProfileID();
                storedAnonymousIdText.text = BrainCloudInterface.instance.GetStoredAnonymousID();
                extraAuthInfoText.text = FACEBOOK_AUTH_INFO;
                break;
        }
    }

    //*************** UI Event Methods ***************
    public void OnAuthTypeChange(int val)
    {
        currentAuthType = (eAuthTypes)val;
        ChangeAuthType(currentAuthType);
    }

    public void OnAdvancedAuthToggle(bool isToggled)
    {
        useAdvancedAuthentication = isToggled;
        ActivateAdvancedAuthFields();
    }

    void ActivateAdvancedAuthFields()
    {
        entityNameField.SetActive(useAdvancedAuthentication);
        entityAgeField.SetActive(useAdvancedAuthentication);
        entityNameText.SetActive(useAdvancedAuthentication);
        entityAgeText.SetActive(useAdvancedAuthentication);
        advAuthInfoText.SetActive(useAdvancedAuthentication);
    }

    public void OnAuthenticate()
    {
        SaveAuthData();

        inputProfileId = profileIdFieldObject.GetComponent<InputField>().text;
        inputPassword = passwordFieldObject.GetComponent<InputField>().text; 

        if((inputProfileId == "" || inputPassword == "") && currentAuthType != eAuthTypes.ANONYMOUS)
        {
            return;
        }

        statusText.fontSize = STATUS_SHRINK_SIZE;
        statusText.text = "Attempting to Authenticate...";

        if(useAdvancedAuthentication)
        {
            inputEntityName = entityNameField.GetComponent<InputField>().text;
            inputEntityAge = entityAgeField.GetComponent<InputField>().text;

            BrainCloud.AuthenticationIds ids;
            Dictionary<string, object> extraJson = new Dictionary<string, object>();
            extraJson["name"] = inputEntityName;
            extraJson["age"] = inputEntityAge;

            if(selectedAdvancedAuthType == AuthenticationType.Anonymous)
            {
                ids.externalId = BrainCloudInterface.instance.GetStoredAnonymousID();
                ids.authenticationToken = "";
                ids.authenticationSubType = "";

                BrainCloudInterface.instance.AuthenticateAdvanced(selectedAdvancedAuthType, ids, extraJson);
            }
            else
            {
                ids.externalId = inputProfileId;
                ids.authenticationToken = inputPassword;
                ids.authenticationSubType = "";

                BrainCloudInterface.instance.AuthenticateAdvanced(selectedAdvancedAuthType, ids, extraJson);
            }

            return;
        }

        switch(currentAuthType)
        {
            case eAuthTypes.EMAIL:
                DataManager.instance.EmailID = inputProfileId;
                DataManager.instance.EmailPass = inputPassword;
                BrainCloudInterface.instance.AuthenticateEmail();
                break;
            case eAuthTypes.UNIVERSAL:
                DataManager.instance.UniversalUserID = inputProfileId;
                DataManager.instance.UniversalPass = inputPassword;
                BrainCloudInterface.instance.AuthenticateUniversal();
                break;
            case eAuthTypes.ANONYMOUS:
                BrainCloudInterface.instance.AuthenticateAnonymous();
                break;
            case eAuthTypes.GOOGLE:
                BrainCloudInterface.instance.AuthenticateGoogle();
                break;
            case eAuthTypes.FACEBOOK:
                BrainCloudInterface.instance.AuthenticateFacebook();
                break;
            case eAuthTypes.XBOXLIVE:
                break; 
        }
    }

    public void OnResetProfileID()
    {
        storedProfileIdText.text = BrainCloudInterface.instance.ResetStoredProfileID(); 
    }

    public void OnResetAnonymousID()
    {
        storedAnonymousIdText.text = BrainCloudInterface.instance.ResetStoredAnonymousID();
    }

    public void OnGoogleSignIn()
    {
        BrainCloudInterface.instance.GoogleSignIn();
    }
}