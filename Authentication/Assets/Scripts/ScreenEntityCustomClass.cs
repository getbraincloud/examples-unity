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
        GameEvents.instance.onGetCustomEntityPageSuccess += OnGetCustomEntityPageSuccess;

        _bc = bc; 

        m_mainScene.CustomEntityInterface.ReadCustomEntity();

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

    private void SetActiveButtons(bool isActive)
    {
        createEntityButton.gameObject.SetActive(isActive);
        saveEntityButton.gameObject.SetActive(!isActive);
        deleteEntityButton.gameObject.SetActive(!isActive);
    }

    //*************** UI Subscribed Methods ***************
    public void OnCreateEntity()
    {
        if (m_player == null)
        {
            m_mainScene.CustomEntityInterface.CreateCustomEntity();
            
            Debug.Log("Creating Entity...");
        }
    }

    public void OnSaveEntity()
    {
        if (m_player != null)
        {
            m_mainScene.CustomEntityInterface.UpdateCustomEntity();

            Debug.Log("Updating Entity...");
        }
    }

    public void OnDeleteEntity()
    {
        if (m_player != null)
        {
            m_mainScene.CustomEntityInterface.DeleteCustomEntity();
            m_player = null;

            Debug.Log("Deleting Entity...");
        }
    }

    public void OnEntityFirstNameEndEdit(string name)
    {
        if (m_player != null)
            m_player.FirstName = name;
    }

    public void OnEntityPositionEndEdit(string position)
    {
        if (m_player != null)
            m_player.Position = position;
    }

    //*************** Game Events Subscribed Methods ***************
    private void OnCreateCustomEntitySuccess()
    {
        m_player = m_mainScene.CustomEntityInterface.CustomPlayer;

        DisplayEntityInfo();

        SetActiveButtons(false);
    }

    private void OnDeleteCustomEntitySuccess()
    {
        DisplayEntityInfo();

        SetActiveButtons(true);
    }

    private void OnGetCustomEntityPageSuccess()
    {
        m_player = m_mainScene.CustomEntityInterface.CustomPlayer;
        DisplayEntityInfo();

        bool bsetActive = m_player == null ? true : false;

        SetActiveButtons(bsetActive); 
    }

    protected override void OnDisable()
    {
        GameEvents.instance.onCreateUserEntitySuccess -= OnCreateCustomEntitySuccess;
        GameEvents.instance.onDeleteUserEntitySuccess -= OnDeleteCustomEntitySuccess;
        GameEvents.instance.onGetCustomEntityPageSuccess -= OnGetCustomEntityPageSuccess; 
    }
}
