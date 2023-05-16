using System;

[Serializable]
public class UserData
{
    public string m_screenName = "";
    public string m_identities = "";

    public UserData()
    {
    }

    public UserData(string screenName, string identities)
    {
        m_screenName = screenName;
        m_identities = identities;
    }
}