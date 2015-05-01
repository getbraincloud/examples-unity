using UnityEngine;
using System.Collections;
using LitJson;
using System.Text;

public abstract class BCScreen 
{
    protected MainScene m_mainScene = null;
    
    public void SetMainScene(MainScene in_scene)
    {
        m_mainScene = in_scene;
    }
        
    public abstract void Activate();
    public abstract void OnScreenGUI();
    
    
    public virtual void Success_Callback(string json, object cbObject)
    {
        m_mainScene.AddLog("SUCCESS");
        m_mainScene.AddLogJson(json);
        m_mainScene.AddLog("");
    }
    
    public virtual void Failure_Callback(string json, object cbObject)
    {
        m_mainScene.AddLog("FAILURE");
        m_mainScene.AddLogJson(json);
        m_mainScene.AddLog("");
    }
}
