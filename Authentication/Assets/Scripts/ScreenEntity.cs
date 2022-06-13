using UnityEngine;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using BrainCloud.Entity;
using UnityEngine.UI;

public class ScreenEntity : BCScreen
{
    EntityInstance m_player;

    string entityName = "";
    int entityAge = 0; 

    //UI elements 
    [SerializeField] Text entityIDText;
    [SerializeField] Text entityTypeText;
    [SerializeField] InputField nameInput;
    [SerializeField] InputField ageInput;
    [SerializeField] GameObject createEntityButton;
    [SerializeField] GameObject saveEntityButton;
    [SerializeField] GameObject deleteEntityButton;

    private void Awake()
    {
        if (HelpMessage == null)
        {
            HelpMessage = "The entity screen demonstrates how a user entity can be created via the brainCloud client.\n\n" +
                          "By pressing Create, a default user entity is created for the user. " +
                          "Pressing Delete will delete the user entity while Save updates the user entity of the user.\n\n" +
                          "This entity can be monitored on the \"User Entites\" page under the \"User Monitoring\" tab in the brainCloud portal.";
        }

        if (HelpURL == null)
        {
            HelpURL = "https://getbraincloud.com/apidocs/apiref/?cloudcode#capi-entity";
        }

        SetFieldsInteractable(false);
    }

    public override void Activate()
    {
        GameEvents.instance.onCreateUserEntitySuccess += OnCreateEntitySuccess;
        GameEvents.instance.onDeleteUserEntitySuccess += OnDeleteEntitySuccess;
        GameEvents.instance.onGetUserEntityPageSuccess += OnGetUserEntityPageSuccess;

        BCFuncScreenHandler.instance.EntityInterface.GetPage(); 
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
        nameInput.text = (m_player != null ? m_player.Name : "");
    }

    void DisplayEntityAge()
    {
        ageInput.text = (m_player != null ? m_player.Age : "");
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
        createEntityButton.SetActive(isActive);
        saveEntityButton.SetActive(!isActive);
        deleteEntityButton.SetActive(!isActive);
    }

    void SetFieldsInteractable(bool isInteractable)
    {
        nameInput.interactable = isInteractable;
        ageInput.interactable = isInteractable; 
    }

    //*************** UI Event Subscribed Methods ***************
    public void OnCreateEntity()
    {
        BCFuncScreenHandler.instance.EntityInterface.CreateEntity();
        Debug.Log("Creating Entity...");
    }

    public void OnSaveEntity()
    {
        BCFuncScreenHandler.instance.EntityInterface.UpdateEntity();
        Debug.Log("Updating Entity..."); 
    }

    public void OnDeleteEntity()
    {
        BCFuncScreenHandler.instance.EntityInterface.DeleteEntity();
        m_player = null;
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
            TextLogger.instance.AddLog("Entity Age -- You must enter a number in this field");
            Debug.LogWarning("Entity Age -- You must enter a number in this field");
            return;
        }

        m_player.Age = entityAge.ToString();
    }

    //*************** Game Event Subscribed Methods ***************
    private void OnCreateEntitySuccess()
    {
        m_player = BCFuncScreenHandler.instance.EntityInterface.Player;

        SetFieldsInteractable(true);

        DisplayEntityInfo();
        SetActiveButtons(false); 
    }

    private void OnDeleteEntitySuccess()
    {
        DisplayEntityInfo();

        SetFieldsInteractable(false);

        SetActiveButtons(true); 
    }

    private void OnGetUserEntityPageSuccess()
    {
        m_player = BCFuncScreenHandler.instance.EntityInterface.Player;
        DisplayEntityInfo();

        bool bsetActive = m_player == null ? true : false;

        SetFieldsInteractable(!bsetActive);

        SetActiveButtons(bsetActive);
    }

    protected override void OnDisable()
    {
        GameEvents.instance.onCreateUserEntitySuccess -= OnCreateEntitySuccess;
        GameEvents.instance.onDeleteUserEntitySuccess -= OnDeleteEntitySuccess;
        GameEvents.instance.onGetUserEntityPageSuccess -= OnGetUserEntityPageSuccess;
    }
}
