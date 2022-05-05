using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//This class will act as data storage for all IDS and potentially entities and other data related info specific to a user.
public class DataManager : MonoBehaviour
{
    public static DataManager instance;

    string m_profileId = "";
    string m_anonymousId = ""; 
    string m_universalUserId = "";
    string m_universalPwd = "";
    string m_emailId = "";
    string m_emailPwd = "";
    string m_googleId = "";
    string m_serverAuthCode = "";

    void Start()
    {
        instance = this;

        DontDestroyOnLoad(this); 
    }

    public void SetProfileID()
    {
        //AnthonyTODO: this will probably just call GetAnonymousID from bcinterface and store it. Not sure if that's even necessary...
    }

    public void SetAnonymousID()
    {
        //AnthonyTODO: this will probably just call GetAnonymousID from bcinterface and store it. Not sure if that's even necessary...
    }

    public void SetEmailandPass(string email, string pass)
    {
        m_emailId = email;
        m_emailPwd = pass; 
    }

    public string GetEmailID()
    {
        return m_emailId;
    }

    public string GetEmailPassword()
    {
        return m_emailPwd; 
    }

    public void SetUniversalIDandPass(string id, string pass)
    {
        m_universalUserId = id;
        m_universalPwd = pass; 
    }

    public string GetUniversalUserID()
    {
        return m_universalUserId;
    }

    public string GetUniversalUserPassword()
    {
        return m_universalPwd; 
    }

    public void SetGoogleID()
    {

    }

    public void SetServerAuthCode()
    {

    }
}
