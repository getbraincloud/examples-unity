using UnityEngine;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using BrainCloud.Entity;
using UnityEngine.UI;

public class ScreenEntity : BCScreen
{
    private static string ENTITY_TYPE_PLAYER = "player";
    
    public ScreenEntity(BrainCloudWrapper bc) : base(bc) { }

    EntityInstance m_player;

    string entityName = "";
    int entityAge = 0; 

    //UI elements 
    [SerializeField] Text entityIDText;
    [SerializeField] Text entityTypeText;
    [SerializeField] InputField nameInput;
    [SerializeField] InputField ageInput;
    [SerializeField] Button createEntityButton;
    [SerializeField] Button saveEntityButton;
    [SerializeField] Button deleteEntityButton;

    private void Start()
    {
        
    }

    public override void Activate(BrainCloudWrapper bc)
    {
        GameEvents.instance.onCreateUserEntitySuccess += OnCreateEntitySuccess;
        GameEvents.instance.onDeleteUserEntitySuccess += OnDeleteEntitySuccess;
        GameEvents.instance.onGetUserEntityPageSuccess += OnGetUserEntityPageSuccess;

        _bc = bc;
        
        m_mainScene.EntityInterface.GetPage(); 
        //m_mainScene.AddLogNoLn("[ReadPlayerState]... ");
    }

    void DisplayEntityID()
    {
        entityIDText.text = (m_player != null ? m_player.EntityId : "---");
    }

    void DisplayEntityType()
    {
        entityTypeText.text = (m_player != null ? m_player.EntityType : "---");
    }

    void DisplayEntityName()
    {
        nameInput.text = (m_player != null ? m_player.Name : "---");
    }

    void DisplayEntityAge()
    {
        ageInput.text = (m_player != null ? m_player.Age : "---");
    }

    void DisplayEntityInfo()
    {
        DisplayEntityID();
        DisplayEntityType();
        DisplayEntityName();
        DisplayEntityAge(); 
    }

    void SetActiveButtons(bool isActive)
    {
        createEntityButton.gameObject.SetActive(isActive);
        saveEntityButton.gameObject.SetActive(!isActive);
        deleteEntityButton.gameObject.SetActive(!isActive);
    }


    //*************** UI Event Subscribed Methods ***************
    public void OnCreateEntity()
    {
        m_mainScene.EntityInterface.CreateEntity();
        //m_mainScene.RealLogging("Creating Entity....");
        Debug.Log("Creating Entity...");
    }

    public void OnSaveEntity()
    {
        m_mainScene.EntityInterface.UpdateEntity();
        //m_mainScene.RealLogging("Updating Entity...");
        Debug.Log("Updating Entity..."); 
    }

    public void OnDeleteEntity()
    {
        m_mainScene.EntityInterface.DeleteEntity();
        m_player = null;
        //m_mainScene.RealLogging("Deleting Entity...");
        Debug.Log("Deleting Entity...");
    }

    public void OnEntityNameEndEdit(string name)
    {
        entityName = name;
        if(m_player != null)
            m_player.Name = entityName;  
    }

    public void OnEntityAgeEndEdit(string age)
    {
        if (m_player == null)
            return;

        if(!int.TryParse(age, out entityAge))
        {
            Debug.LogWarning("Entity Age -- You must enter a number in this field");
            return;
        }

        m_player.Age = entityAge.ToString();
    }


    //*************** Game Event Subscribed Methods ***************
    private void OnCreateEntitySuccess()
    {
        m_player = m_mainScene.EntityInterface.Player;

        DisplayEntityInfo();
        SetActiveButtons(false); 

    }

    private void OnDeleteEntitySuccess()
    {
        DisplayEntityInfo();
        SetActiveButtons(true); 
    }

    private void OnGetUserEntityPageSuccess()
    {
        m_player = m_mainScene.EntityInterface.Player;
        DisplayEntityInfo();

        bool bsetActive = m_player == null ? true : false;

        SetActiveButtons(bsetActive);
    }

    protected override void OnDisable()
    {
        GameEvents.instance.onCreateUserEntitySuccess -= OnCreateEntitySuccess;
        GameEvents.instance.onDeleteUserEntitySuccess -= OnDeleteEntitySuccess;
        GameEvents.instance.onGetUserEntityPageSuccess -= OnGetUserEntityPageSuccess;
    }


    #region Stuff to Remove
    //public override void OnScreenGUI()
    //{
    //    //AnthonyTODO: Moved this to Activate. Not sure if that's where it should go.
    //    //EntityInstance m_player = null;
    //    //if (m_mainScene.EntityInterface.PlayerAssigned)
    //    //{
    //    //    m_player = m_mainScene.EntityInterface.Player;    
    //    //}
        
    //    GUILayout.BeginVertical();
    //    //GUILayout.Box("Player Entity");
        
    //    int minLabelWidth = 60;
        
    //    // entity id
    //    GUILayout.BeginHorizontal();
    //    GUILayout.Label("Id", GUILayout.Width(minLabelWidth));
    //    GUILayout.Box(m_player != null ? m_player.EntityId : "---");
    //    GUILayout.EndHorizontal();
        
    //    // entity type
    //    GUILayout.BeginHorizontal();
    //    GUILayout.Label("Type", GUILayout.Width(minLabelWidth));
    //    GUILayout.Box(m_player != null ? m_player.EntityType : "---");
    //    GUILayout.EndHorizontal();
        
    //    // entity property 'name'
    //    GUILayout.BeginHorizontal();
    //    GUILayout.Label("Name", GUILayout.Width(minLabelWidth));
    //    if (m_player != null)
    //    {
    //        m_player.Name = GUILayout.TextField(m_player.Name);
    //    } 
    //    else
    //    {
    //        GUILayout.Box("---");
    //    }
    //    GUILayout.EndHorizontal();
        
    //    // entity property 'age'
    //    GUILayout.BeginHorizontal();
    //    GUILayout.Label("Age", GUILayout.Width(minLabelWidth));
    //    if (m_player != null)
    //    {
    //        string ageStr = GUILayout.TextField(m_player.Age);
    //        int ageInt = 0;
    //        if (int.TryParse(ageStr, out ageInt))
    //        {
    //            m_player.Age = ageInt.ToString();
    //        }
    //    } else
    //    {
    //        GUILayout.Box("---");
    //    }
    //    GUILayout.EndHorizontal();
        
    //    GUILayout.BeginHorizontal();
    //    if (m_player == null)
    //    {
    //        GUILayout.FlexibleSpace();
    //        if (GUILayout.Button("Create Entity"))
    //        {
    //            m_mainScene.EntityInterface.CreateEntity();
    //            m_mainScene.RealLogging("Creating Entity....");
    //        }
    //    }
    //    if (m_player != null)
    //    {
    //        GUILayout.FlexibleSpace();
    //        if (GUILayout.Button("Save Entity"))
    //        {
    //            m_mainScene.EntityInterface.UpdateEntity();
    //            m_mainScene.RealLogging("Updating Entity...");
    //        }
    //        if (GUILayout.Button("Delete Entity"))
    //        {
    //            m_mainScene.EntityInterface.DeleteEntity();
    //            m_player = null;
    //            m_mainScene.RealLogging("Deleting Entity...");
    //        }
    //    }
    //    GUILayout.EndHorizontal();
        
    //    GUILayout.EndVertical();
    //}
    #endregion
}
