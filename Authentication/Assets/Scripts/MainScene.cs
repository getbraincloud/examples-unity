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
    public BCConfig BCConfig;

    private BrainCloudWrapper _bc;
    private EntityInterface _entityInterface;
    public EntityInterface EntityInterface
    {
        get => _entityInterface;
    }
    
    private CustomEntityInterface _customEntityInterface;

    public CustomEntityInterface CustomEntityInterface
    {
        get => _customEntityInterface;
    }
    
    static int MIN_LEFT_SIDE_WIDTH = 350;
    Vector2 m_scrollPosition;
    string m_log = "";
    BCScreen m_screen = null;

    //AnthonyTODO: Members I'm adding
    BrainCloudInterface bcInterface; 
    DataManager dataManager; 
    List<BCScreen> bcScreens;

    //AnthonyTODO: UI Elements
    [SerializeField] Text appDataText; 


    public enum eBCFunctionType : int
    {
        FN_ENTITY = 0,
        FN_ENTITY_CUSTOM_CLASS,
        FN_PLAYER_XP_CURRENCY,
        FN_PLAYER_STATS,
        FN_GLOBAL_STATS,
        FN_CLOUD_CODE,
		FN_IDENTITY
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
		"Identity"

    };
    eBCFunctionType currentBCFunction = eBCFunctionType.FN_ENTITY;

    // Use this for initialization
    void Start()
    {
        _bc = BCConfig.GetBrainCloud();

        bcInterface = BrainCloudInterface.instance;
        dataManager = DataManager.instance; 
        
        _entityInterface = GetComponent<EntityInterface>();
        _entityInterface.Wrapper = _bc;
        _customEntityInterface = GetComponent<CustomEntityInterface>();
        _customEntityInterface.Wrapper = _bc;

        bcScreens = new List<BCScreen>(FindObjectsOfType<BCScreen>());
        bcScreens.Sort((BCScreen screen1, BCScreen screen2) => screen1.transform.GetSiblingIndex().CompareTo(screen2.transform.GetSiblingIndex()));

        for(int i = 0; i < bcScreens.Count; i++)
        {
            bcScreens[i].SetFunctionType((eBCFunctionType)i);
        }
        
        MoveToScreen(currentBCFunction);
        SetGameData();
    }

    public void OnSelectBCFunction(int val)
    {
        currentBCFunction = (eBCFunctionType)val; 
        MoveToScreen(currentBCFunction);
    }

    private void MoveToScreen(eBCFunctionType in_fn)
    {
        foreach(BCScreen screen in bcScreens)
        {
            if(screen.GetFunctionType() == in_fn)
            {
                screen.gameObject.SetActive(true);
                screen.SetMainScene(this);
                screen.Activate(_bc);
            }
            else
            {
                screen.gameObject.SetActive(false); 
            }
        }

        #region Old scene switching code
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
        string appID = bcInterface.GetAppID();
        string gameVersion = bcInterface.GetAppVersion();
        string profileID = bcInterface.GetAuthenticatedProfileID();

        appDataText.text = "AppID:" + appID + "  AppVersion:" + gameVersion + "  ProfileID:" + profileID;
    }
    
    //Deprecated Authentication code
    public void TwitterCoroutine(IEnumerator coroutine)
    {
        StartCoroutine(coroutine);
    }

    #region onGui Toolbar for screen selection
    // lays out the top toolbar + player info
    void OnGUITopButtons()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Box("Select a BrainCloud Function:");
        eBCFunctionType fn = (eBCFunctionType)GUILayout.Toolbar((int)currentBCFunction, m_bcFuncLabels);
        GUILayout.EndHorizontal();

        // if user selected another screen, move to it
        if (fn != currentBCFunction)
        {
            MoveToScreen(fn);
        }
    }
    #endregion



    #region onGUI Game Data
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
    #endregion

    #region Old OnGui method
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

    #region onGUI log stuff
    // lays out the right hand pane for the log
    void OnGUILog()
    {
        m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUILayout.TextArea(m_log);
        GUILayout.EndScrollView();
        
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Clear Log", GUILayout.Height(25), GUILayout.Width(100)))
        {
            m_log = "";
        }
        GUILayout.EndHorizontal();
        
        //GUILayout.Space(20);
    }

    public void AddLog(string log)
    {
        m_log += log;
        m_log += "\n";
        m_scrollPosition = new Vector2(m_scrollPosition.x, Mathf.Infinity);
    }

    public void RealLogging(string in_log)
    {
        m_log = in_log;
        Debug.Log($"My Log: {in_log}");
    }

    public void AddLogNoLn(string log)
    {
        m_log += log;
        m_scrollPosition = new Vector2(m_scrollPosition.x, Mathf.Infinity);
    }

    public void AddLogJson(string json)
    {
        StringBuilder sb = new StringBuilder();
        JsonWriter writer = new JsonWriter(sb);
        writer.PrettyPrint = true;
        JsonMapper.ToJson(JsonMapper.ToObject(json), writer);
        AddLog(sb.ToString());
    }
    #endregion
}
