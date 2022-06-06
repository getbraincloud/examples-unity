using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//This class will act as data storage for all IDS and potentially entities and other data related info specific to a user.
public class DataManager : MonoBehaviour
{
    public static DataManager instance;

    //Authentication Data
    public string ProfileID { get; set; }
    public string AnonymousID { get; set; }
    public string UniversalUserID { get; set; }
    public string UniversalPass { get; set; }
    public string EmailID { get; set; }
    public string EmailPass { get; set; }
    public string GoogleID { get; set; }
    public string ServerAuthCode { get; set; }

    //PlayerXP Data
    public int PlayerLevel { get; set; }
    public int PlayerXP { get; set; }

    //Currency Data
    public class Currency
    {
        public string currencyType;
        public int purchased;
        public int balance;
        public int consumed;
        public int awarded;

        public string award = "0";
        public string consume = "0";
    }
    public IDictionary<string, Currency> Currencies { get; set; }

    //Player Stat Data
    public Dictionary<string, long> PlayerStats { get; set; }

    //Global Stat Data
    public Dictionary<string, long> GlobalStats { get; set; }


    void Start()
    {
        instance = this;

        //InitData(); 

        DontDestroyOnLoad(this); 
    }

    public void InitData()
    {
        Currencies = new Dictionary<string, Currency>();
        PlayerStats = new Dictionary<string, long>();
        GlobalStats = new Dictionary<string, long>();
    }

    public void ResetData()
    {
        ProfileID = "";
        AnonymousID = "";
        UniversalUserID = "";
        UniversalPass = "";
        EmailID = "";
        EmailPass = "";
        GoogleID = "";
        ServerAuthCode = "";
        PlayerLevel = 0;
        PlayerXP = 0;

        Currencies.Clear();
        Currencies = null;

        PlayerStats.Clear();
        PlayerStats = null;

        GlobalStats.Clear();
        GlobalStats = null;
    }
}
