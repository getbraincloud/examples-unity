using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using System.Text;
using BrainCloud.LitJson;
using UnityEngine.UI;

public class MainScene : MonoBehaviour
{
    [SerializeField] Text appDataText;
    [SerializeField] Dropdown funcDropdown;

    public BCConfig BCConfig;
    private BrainCloudWrapper _bc;
    private EntityInterface _entityInterface;
    List<BCScreen> bcScreens;
    Dictionary<eBCFunctionType, BCScreen> bcScreenDict;
    BCScreen currentlyActiveScreen = null;
    bool bHasAuthenticatedOnce = false;

    public EntityInterface EntityInterface
    {
        get => _entityInterface;
    }
    
    private CustomEntityInterface _customEntityInterface;

    public CustomEntityInterface CustomEntityInterface
    {
        get => _customEntityInterface;
    }

    public enum eBCFunctionType : int
    {
        FN_ENTITY = 0,
        FN_ENTITY_CUSTOM_CLASS,
        FN_PLAYER_XP_CURRENCY,
        FN_PLAYER_STATS,
        FN_GLOBAL_STATS,
        FN_CLOUD_CODE,
		FN_IDENTITY, 
        FN_LOGOUT // This should be last.
        //etc
    }

    string[] m_bcFuncLabels =
    {
        "Entity",
        "Entity Custom",
        "XP/Currency",
        "Player Stats",
        "Global Stats",
        "Cloud Code",
		"Identity", 
        "Logout" // This should be last.
    };
    

    void Awake()
    {
        _bc = BCConfig.GetBrainCloud();

        if(_entityInterface == null)
        {
            _entityInterface = GetComponent<EntityInterface>();
        }

        if(_customEntityInterface == null)
        {
            _customEntityInterface = GetComponent<CustomEntityInterface>();
        }

        _entityInterface.Wrapper = _bc;
        _customEntityInterface.Wrapper = _bc;

        //Find all Braincloud function screens under MainScreen and sort them based on position in scene hierarchy.
        if (bcScreens == null)
        {
            bcScreens = new List<BCScreen>(FindObjectsOfType<BCScreen>(true));
            bcScreens.Sort((BCScreen screen1, BCScreen screen2) => screen1.transform.GetSiblingIndex().CompareTo(screen2.transform.GetSiblingIndex()));
        }

        //Setting Dictionary of braincloud function screens based on sorted list.
        if(bcScreenDict == null)
        {
            bcScreenDict = new Dictionary<eBCFunctionType, BCScreen>();
            for (int i = 0; i < bcScreens.Count; i++)
            {
                bcScreens[i].SetFunctionType((eBCFunctionType)i);
                bcScreenDict.Add(bcScreens[i].GetFunctionType(), bcScreens[i]);
            }
        }
    }

    private void MoveToScreen(eBCFunctionType in_fn)
    {
        if(currentlyActiveScreen != null)
        {
            currentlyActiveScreen.gameObject.SetActive(false);
            currentlyActiveScreen = null; 
        }

        if (in_fn == eBCFunctionType.FN_LOGOUT)
        {
            BrainCloudInterface.instance.Logout();
            return;
        }

        bcScreenDict.TryGetValue(in_fn, out currentlyActiveScreen);

        if(currentlyActiveScreen != null)
        {
            currentlyActiveScreen.gameObject.SetActive(true);
            currentlyActiveScreen.SetMainScene(this);
            currentlyActiveScreen.Activate(_bc);
        }

        #region Old scene switching code
        //First Iteration for moving screens
        //foreach(BCScreen screen in bcScreens)
        //{
        //    if(screen.GetFunctionType() == in_fn)
        //    {
        //        screen.gameObject.SetActive(true);
        //        screen.SetMainScene(this);
        //        screen.Activate(_bc);
        //    }
        //    else
        //    {
        //        screen.gameObject.SetActive(false); 
        //    }
        //}

        //if(in_fn == eBCFunctionType.FN_LOGOUT)
        //{
        //    BrainCloudInterface.instance.Logout();
        //    return;
        //}


        //switch (in_fn)
        //{
        //    case eBCFunctionType.FN_ENTITY:
        //        m_screen = new ScreenEntity(_bc);
        //        break;
        //    case eBCFunctionType.FN_ENTITY_CUSTOM_CLASS:
        //        m_screen = new ScreenEntityCustomClass(_bc);
        //        break;
        //    case eBCFunctionType.FN_PLAYER_XP_CURRENCY:
        //        m_screen = new ScreenPlayerXp(_bc);
        //        break;
        //    case eBCFunctionType.FN_PLAYER_STATS:
        //        m_screen = new ScreenPlayerStats(_bc);
        //        break;
        //    case eBCFunctionType.FN_GLOBAL_STATS:
        //        m_screen = new ScreenGlobalStats(_bc);
        //        break;
        //    case eBCFunctionType.FN_CLOUD_CODE:
        //        m_screen = new ScreenCloudCode(_bc);
        //        break;
        //    case eBCFunctionType.FN_IDENTITY:
        //        m_screen = new ScreenIdentity(_bc);
        //        break;
        //}
        //currentBCFunction = in_fn;
        //m_screen.SetMainScene(this);
        //m_screen.Activate();
        #endregion
    }

    void SetGameData()
    {
        string appID = BrainCloudInterface.instance.GetAppID();
        string gameVersion = BrainCloudInterface.instance.GetAppVersion();
        string profileID = BrainCloudInterface.instance.GetAuthenticatedProfileID();

        appDataText.text = "AppID:" + appID + "  AppVersion:" + gameVersion + "  ProfileID:" + profileID;
    }

    private void OnEnable()
    {
        //Preventing duplicate text logs on subsequent logins.
        if(!bHasAuthenticatedOnce)
        {
            MoveToScreen(eBCFunctionType.FN_ENTITY);
           bHasAuthenticatedOnce = true;
        }
        else
        {
            //Calling this invokes OnSelectBCFunction() if not already set to 0.
            funcDropdown.value = 0; 
        }

        SetGameData();
    }


    //*************** UI Methods ***************
    public void OnSelectBCFunction(int val)
    {
        MoveToScreen((eBCFunctionType)val);
    }


    #region Logging Logic
    // lays out the right hand pane for the log
    //void OnGUILog()
    //{
    //    m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
    //    GUILayout.TextArea(m_log);
    //    GUILayout.EndScrollView();
        
    //    GUILayout.BeginHorizontal();
    //    GUILayout.FlexibleSpace();
    //    if (GUILayout.Button("Clear Log", GUILayout.Height(25), GUILayout.Width(100)))
    //    {
    //        m_log = "";
    //    }
    //    GUILayout.EndHorizontal();
        
    //    //GUILayout.Space(20);
    //}

    //public void AddLog(string log)
    //{
    //    m_log += log;
    //    m_log += "\n";
    //    m_scrollPosition = new Vector2(m_scrollPosition.x, Mathf.Infinity);
    //}

    //public void RealLogging(string in_log)
    //{
    //    m_log = in_log;
    //    Debug.Log($"My Log: {in_log}");
    //}

    //public void AddLogNoLn(string log)
    //{
    //    m_log += log;
    //    m_scrollPosition = new Vector2(m_scrollPosition.x, Mathf.Infinity);
    //}

    //public void AddLogJson(string json)
    //{
    //    StringBuilder sb = new StringBuilder();
    //    JsonWriter writer = new JsonWriter(sb);
    //    writer.PrettyPrint = true;
    //    JsonMapper.ToJson(JsonMapper.ToObject(json), writer);
    //    AddLog(sb.ToString());
    //}
    #endregion

    #region Stuff To Remove
    // lays out the top toolbar + player info
    //void OnGUITopButtons()
    //{
    //    GUILayout.BeginHorizontal();
    //    GUILayout.Box("Select a BrainCloud Function:");
    //    eBCFunctionType fn = (eBCFunctionType)GUILayout.Toolbar((int)currentBCFunction, m_bcFuncLabels);
    //    GUILayout.EndHorizontal();

    //    // if user selected another screen, move to it
    //    if (fn != currentBCFunction)
    //    {
    //        MoveToScreen(fn);
    //    }
    //}

    void OnGUIBottom()
    {
        //AnthonyTODO: create a method in bcinterface that get this info and set the game info text to this.
        GUILayout.BeginHorizontal();
        GUILayout.Box("Game Id: " + _bc.Client.AppId);
        GUILayout.Box("Game Version: " + _bc.Client.AppVersion);
        GUILayout.Box("Profile Id: " + _bc.Client.AuthenticationService.ProfileId);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    //public void OnGUI()
    //{
    //    GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");

    //    OnGUITopButtons();

    //    GUILayout.BeginHorizontal();

    //    GUILayout.BeginVertical();
    //    GUILayout.BeginHorizontal();
    //    GUILayout.Space(MIN_LEFT_SIDE_WIDTH);
    //    GUILayout.EndHorizontal();
    //    if (m_screen != null)
    //    {
    //        m_screen.OnScreenGUI();
    //    }
    //    GUILayout.EndVertical();

    //    GUILayout.Space(25);

    //    GUILayout.BeginVertical();
    //    OnGUILog();
    //    GUILayout.EndVertical();

    //    GUILayout.EndHorizontal();


    //    OnGUIBottom();
    //}
    #endregion
    public void TwitterCoroutine(IEnumerator coroutine)
    {
        StartCoroutine(coroutine);
    }

}
