using UnityEngine;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using BrainCloud.Entity;
using UnityEngine.UI; 

public class ScreenEntityCustomClass : BCScreen
{
    public class Hobby
    {
        public string Name
        {
            get { return ""; }
        }
    }

    #region Old custom entity class
    //public class Player : BCUserEntity
    //{
    //    public static string ENTITY_TYPE = "player";

    //    public Player(BrainCloudEntity in_bcEntityService) : base(in_bcEntityService)
    //    {
    //        // set up some defaults
    //        m_entityType = "player";
    //        Name = "";
    //        Age = 0;
    //        Hobbies = new List<Hobby>();
    //    }

    //    public string Name
    //    {
    //        get { return (string) this ["name"]; }
    //        set { this ["name"] = value; }
    //    }

    //    public int Age
    //    {
    //        get { return (int) this ["age"]; }
    //        set { this ["age"] = value; }
    //    }

    //    public IList<Hobby> Hobbies
    //    {
    //        get { return this.Get<IList<Hobby>>("hobbies"); }
    //        set { this["hobbies"] = value; }
    //    }
    //}
    #endregion

    //AnthonyTODO: members I'm adding.
    CustomEntityInstance m_player = null;
    //UI elements 
    [SerializeField] Text entityIDText;
    [SerializeField] Text entityTypeText;
    [SerializeField] InputField firstNameInput;
    [SerializeField] InputField positionInput;
    [SerializeField] Button createEntityButton;
    [SerializeField] Button saveEntityButton;
    [SerializeField] Button deleteEntityButton; 

    public ScreenEntityCustomClass(BrainCloudWrapper bc) : base(bc) { }

    public override void Activate(BrainCloudWrapper bc)
    {
        GameEvents.instance.onCreateCustomEntitySuccess += OnCreateCustomEntitySuccess;
        GameEvents.instance.onDeleteCustomEntitySuccess += OnDeleteCustomEntitySuccess;

        _bc = bc; 
        m_mainScene.CustomEntityInterface.ReadCustomEntity();
        m_mainScene.RealLogging("[ReadPlayerState]... ");

        //m_player = m_mainScene.CustomEntityInterface.CustomPlayer;
        DisplayEntityInfo();

        SetActiveButtons(m_player == null ? true : false); 
    }

    void DisplayEntityID()
    {
        entityIDText.text = (m_player != null ? m_player.EntityId : "---");
    }

    void DisplayEntityType()
    {
        entityTypeText.text = (m_player != null ? m_player.EntityType : "---");
    }

    void DisplayEntityFirstName()
    {
        firstNameInput.text = (m_player != null ? m_player.FirstName : "---");
    }

    void DisplayEntityPosition()
    {
        positionInput.text = (m_player != null ? m_player.Position : "---");
    }

    void DisplayEntityInfo()
    {
        DisplayEntityID();
        DisplayEntityType();
        DisplayEntityFirstName();
        DisplayEntityPosition();
    }

    public void OnCreateEntity()
    {
        if (m_player == null)
        {
            m_mainScene.CustomEntityInterface.CreateCustomEntity();
            m_mainScene.RealLogging("Creating Entity....");
            Debug.Log("Creating Entity...");
        }
    }

    //Event subscribed to onCreateCustomEntitySuccess
    private void OnCreateCustomEntitySuccess()
    {
        m_player = m_mainScene.CustomEntityInterface.CustomPlayer;

        DisplayEntityInfo();

        SetActiveButtons(false);
    }

    public void OnSaveEntity()
    {
        if (m_player != null)
        {
            m_mainScene.CustomEntityInterface.UpdateCustomEntity();
            m_mainScene.RealLogging("Updating Entity...");
            Debug.Log("Updating Entity...");
        }
    }

    public void OnDeleteEntity()
    {
        if(m_player != null)
        {
            m_mainScene.CustomEntityInterface.DeleteCustomEntity();
            m_player = null;
            m_mainScene.RealLogging("Deleting Entity...");
            Debug.Log("Deleting Entity...");
        }
    }

    //Event subscribed to onDeleteCustomEntitySuccess
    private void OnDeleteCustomEntitySuccess()
    {
        DisplayEntityInfo();

        SetActiveButtons(true); 
    }

    public void OnEntityFirstNameEndEdit(string name)
    {
        //entityName = name;
        if (m_player != null)
            m_player.FirstName = name;
    }

    public void OnEntityPositionEndEdit(string position)
    {
        if (m_player != null)
            m_player.Position = position; 
    }

    private void SetActiveButtons(bool isActive)
    {
        createEntityButton.gameObject.SetActive(isActive);
        saveEntityButton.gameObject.SetActive(!isActive);
        deleteEntityButton.gameObject.SetActive(!isActive);
    }

    private void OnDisable()
    {
        //AnthonyTODO: Figure out why _player in Entity interface gets deleted but the same issue doesn't happen here.
        //if (m_player != null)
        //{
        //    OnDeleteEntity();
        //}

        GameEvents.instance.onCreateUserEntitySuccess -= OnCreateCustomEntitySuccess;
        GameEvents.instance.onDeleteUserEntitySuccess -= OnDeleteCustomEntitySuccess;
    }

    #region OnGuiLogic
    public override void OnScreenGUI()
    {
        //CustomEntityInstance m_player = null;
        if (m_mainScene.CustomEntityInterface.PlayerAssigned)
        {
            m_player = m_mainScene.CustomEntityInterface.CustomPlayer;    
        }
        
        GUILayout.BeginVertical();

        int minLabelWidth = 60;

        // entity id
        GUILayout.BeginHorizontal();
        GUILayout.Label("Id", GUILayout.Width(minLabelWidth));
        GUILayout.Box(m_player != null ? m_player.EntityId : "---");
        GUILayout.EndHorizontal();

        // entity type
        GUILayout.BeginHorizontal();
        GUILayout.Label("Type", GUILayout.Width(minLabelWidth));
        GUILayout.Box(m_player != null ? m_player.EntityType : "---");
        GUILayout.EndHorizontal();

        // entity property 'name'
        GUILayout.BeginHorizontal();
        GUILayout.Label("Name", GUILayout.Width(minLabelWidth));
        if (m_player != null)
        {
            m_player.FirstName = GUILayout.TextField(m_player.FirstName);
        } 
        else
        {
            GUILayout.Box("---");
        }
        GUILayout.EndHorizontal();

        // entity property 'age'
        GUILayout.BeginHorizontal();
        GUILayout.Label("Position", GUILayout.Width(minLabelWidth));
        if (m_player != null)
        {
            m_player.Position = GUILayout.TextField(m_player.Position);
        } 
        else
        {
            GUILayout.Box("---");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (m_player == null)
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Create Entity"))
            {
                m_mainScene.CustomEntityInterface.CreateCustomEntity();
                m_mainScene.RealLogging("Creating Entity....");
            }
        }
        if (m_player != null)
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save Entity"))
            {
                m_mainScene.CustomEntityInterface.UpdateCustomEntity();
                m_mainScene.RealLogging("Updating Entity...");
            }
            if (GUILayout.Button("Delete Entity"))
            {
                m_mainScene.CustomEntityInterface.DeleteCustomEntity();
                m_player = null;
                m_mainScene.RealLogging("Deleting Entity...");
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }
    #endregion
}
