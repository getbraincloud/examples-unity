using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using System.Text;
using BrainCloud.LitJson;
using UnityEngine.UI;

public class BCFuncScreenHandler : MonoBehaviour
{
    public static BCFuncScreenHandler instance { get; private set; } 

    [SerializeField] Text appDataText;
    [SerializeField] Dropdown funcDropdown;
    [SerializeField] GameObject helpPanelObject;
    [SerializeField] Button helpButton;

    public BCConfig BCConfig;
    private BrainCloudWrapper _bc;
    private EntityInterface _entityInterface;
    List<BCScreen> bcScreens;
    Dictionary<eBCFunctionType, BCScreen> bcScreenDict;
    BCScreen currentlyActiveScreen = null;

    HelpPanel helpPanel; 

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
		FN_IDENTITY 
        //etc
    }

    void Awake()
    {
        instance = this;

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

        helpPanel = helpPanelObject.GetComponent<HelpPanel>(); 
    }

    private void MoveToScreen(eBCFunctionType in_fn)
    {
        if(currentlyActiveScreen != null)
        {
            currentlyActiveScreen.gameObject.SetActive(false);
            currentlyActiveScreen = null; 
        }

        bcScreenDict.TryGetValue(in_fn, out currentlyActiveScreen);

        if(currentlyActiveScreen != null)
        {
            currentlyActiveScreen.gameObject.SetActive(true);
            currentlyActiveScreen.Activate();

            string screenname = currentlyActiveScreen.gameObject.name;
            string helpmessage = currentlyActiveScreen.HelpMessage;
            string url = currentlyActiveScreen.HelpURL;

            helpPanel.SetHelpPanel(screenname, helpmessage, url); 
        }
    }

    void SetGameData()
    {
        string appID = BrainCloudInterface.instance.GetAppID();
        string gameVersion = BrainCloudInterface.instance.GetAppVersion();
        string profileID = BrainCloudInterface.instance.GetAuthenticatedProfileID();

        appDataText.text = "AppID:" + appID + "  AppVersion:" + gameVersion + "  ProfileID:" + profileID;
    }

    void SetHelpPanelActive(bool isActive)
    {
        helpPanelObject.SetActive(isActive);
    }

    private void OnEnable()
    {
        if(funcDropdown.value == 0)
        {
            MoveToScreen(eBCFunctionType.FN_ENTITY);
        }
        else
        {
            //Calling this invokes OnSelectBCFunction() if not already set to 0.
            funcDropdown.value = 0;
        }

        SetGameData();
        SetHelpPanelActive(false);
    }

    //*************** UI Methods ***************
    public void OnSelectBCFunction(int val)
    {
        MoveToScreen((eBCFunctionType)val);
    }

    public void OnLogoutClick()
    {
        BrainCloudInterface.instance.Logout();
    }

    public void OnHelpClick()
    {
        Debug.Log("Help Button Clicked on " + currentlyActiveScreen);

        SetHelpPanelActive(!helpPanelObject.activeSelf);
    }

    public void OnExitHelpClick()
    {
        if (helpPanelObject.activeSelf)
        {
            SetHelpPanelActive(false);
        }
    }

    public void TwitterCoroutine(IEnumerator coroutine)
    {
        StartCoroutine(coroutine);
    }

}
