﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using System.Text;
using BrainCloud.LitJson;

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

    enum BrainCloudFunction : int
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
    BrainCloudFunction m_bcFunc = BrainCloudFunction.FN_ENTITY;

    // Use this for initialization
    void Start()
    {
        _bc = BCConfig.GetBrainCloud();
        
        _entityInterface = GetComponent<EntityInterface>();
        _entityInterface.Wrapper = _bc;
        _customEntityInterface = GetComponent<CustomEntityInterface>();
        _customEntityInterface.Wrapper = _bc;
        
        MoveToScreen(m_bcFunc);
    }
    // Update is called once per frame
    void Update()
    {
    }

    private void MoveToScreen(BrainCloudFunction in_fn)
    {
        switch (in_fn)
        {
            case BrainCloudFunction.FN_ENTITY:
                m_screen = new ScreenEntity(_bc);
                break;
            case BrainCloudFunction.FN_ENTITY_CUSTOM_CLASS:
                m_screen = new ScreenEntityCustomClass(_bc);
                break;
            case BrainCloudFunction.FN_PLAYER_XP_CURRENCY:
                m_screen = new ScreenPlayerXp(_bc);
                break;
            case BrainCloudFunction.FN_PLAYER_STATS:
                m_screen = new ScreenPlayerStats(_bc);
                break;
            case BrainCloudFunction.FN_GLOBAL_STATS:
                m_screen = new ScreenGlobalStats(_bc);
                break;
            case BrainCloudFunction.FN_CLOUD_CODE:
                m_screen = new ScreenCloudCode(_bc);
                break;
			case BrainCloudFunction.FN_IDENTITY:
				m_screen = new ScreenIdentity(_bc);
				break;
        }
        m_bcFunc = in_fn;
        m_screen.SetMainScene(this);
        m_screen.Activate();
    }

    
    public void TwitterCoroutine(IEnumerator coroutine)
    {
        StartCoroutine(coroutine);
    }

    // lays out the top toolbar + player info
    void OnGUITopButtons()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Box("Select a BrainCloud Function:");
        BrainCloudFunction fn = (BrainCloudFunction)GUILayout.Toolbar((int)m_bcFunc, m_bcFuncLabels);
        GUILayout.EndHorizontal();

        // if user selected another screen, move to it
        if (fn != m_bcFunc)
        {
            MoveToScreen(fn);
        }
    }

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

    void OnGUIBottom()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Box("Game Id: " + _bc.Client.AppId);
        GUILayout.Box("Game Version: " + _bc.Client.AppVersion);
        GUILayout.Box("Profile Id: " + _bc.Client.AuthenticationService.ProfileId);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    public void OnGUI()
    {
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");

        OnGUITopButtons();
        
        GUILayout.BeginHorizontal();
        
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        GUILayout.Space(MIN_LEFT_SIDE_WIDTH);
        GUILayout.EndHorizontal();
        if (m_screen != null)
        {
            m_screen.OnScreenGUI();
        }
        GUILayout.EndVertical();
        
        GUILayout.Space(25);
        
        GUILayout.BeginVertical();
        OnGUILog();
        GUILayout.EndVertical();
        
        GUILayout.EndHorizontal();


        OnGUIBottom();
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
}
