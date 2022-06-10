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

    bool bAttemptedFetchEntity = false;

    //UI elements 
    [SerializeField] Text entityIDText;
    [SerializeField] Text entityTypeText;
    [SerializeField] InputField firstNameInput;
    [SerializeField] InputField positionInput;
    [SerializeField] GameObject fetchEntityButton; 
    [SerializeField] GameObject createEntityButton;
    [SerializeField] GameObject saveEntityButton;
    [SerializeField] GameObject deleteEntityButton;

    private void Awake()
    {
        if (HelpMessage == null)
        {
            HelpMessage = "<b>" + "Custom Entities are a premium feature available to Plus Plan customers. Additional usage fees apply.\n\n" + "</b>" +
                          "The custom entity screen demonstrates how a custom entity can be created via the brainCloud client.\n\n" +
                          "By pressing Create, a default custom entity is created for the user. " +
                          "Pressing Delete will delete the custom entity while Save updates the custom entity of the user.\n\n" +
                          "This custom entity can be monitored on the \"Custom Entities\" page under the \"User Monitoring\" tab in the brainCloud Portal.";
        }

        if (HelpURL == null)
        {
            HelpURL = "https://getbraincloud.com/apidocs/apiref/?cloudcode#capi-customentity";
        }
    }

    public override void Activate()
    {
        GameEvents.instance.onCreateCustomEntitySuccess += OnCreateCustomEntitySuccess;
        GameEvents.instance.onDeleteCustomEntitySuccess += OnDeleteCustomEntitySuccess;
        GameEvents.instance.onGetCustomEntityPageSuccess += OnGetCustomEntityPageSuccess;

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
        firstNameInput.text = (m_player != null ? m_player.FirstName : "");
    }

    void DisplayEntityPosition()
    {
        positionInput.text = (m_player != null ? m_player.Position : "");
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
        fetchEntityButton.SetActive(isActive); 
        createEntityButton.SetActive(isActive);
        saveEntityButton.SetActive(!isActive);
        deleteEntityButton.SetActive(!isActive);

        if (!createEntityButton.activeSelf)
            return;

        if (!bAttemptedFetchEntity)
            createEntityButton.GetComponent<Button>().interactable = false;
        else
            createEntityButton.GetComponent<Button>().interactable = true;
    }

    //*************** UI Subscribed Methods ***************
    public void OnFetchEntity()
    {
        BCFuncScreenHandler.instance.CustomEntityInterface.ReadCustomEntity();
    }
    
    public void OnCreateEntity()
    {
        if (m_player == null)
        {
            BCFuncScreenHandler.instance.CustomEntityInterface.CreateCustomEntity();
        }
    }

    public void OnSaveEntity()
    {
        if (m_player != null)
        {
            BCFuncScreenHandler.instance.CustomEntityInterface.UpdateCustomEntity();
        }
    }

    public void OnDeleteEntity()
    {
        if (m_player != null)
        {
            BCFuncScreenHandler.instance.CustomEntityInterface.DeleteCustomEntity();
            m_player = null;
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
        m_player = BCFuncScreenHandler.instance.CustomEntityInterface.CustomPlayer;

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
        m_player = BCFuncScreenHandler.instance.CustomEntityInterface.CustomPlayer;
        DisplayEntityInfo();

        bool bsetActive = m_player == null ? true : false;
        bAttemptedFetchEntity = true;

        SetActiveButtons(bsetActive); 
    }

    protected override void OnDisable()
    {
        bAttemptedFetchEntity = false;
        SetActiveButtons(true);
        m_player = null;
        DisplayEntityInfo(); 

        GameEvents.instance.onCreateUserEntitySuccess -= OnCreateCustomEntitySuccess;
        GameEvents.instance.onDeleteUserEntitySuccess -= OnDeleteCustomEntitySuccess;
        GameEvents.instance.onGetCustomEntityPageSuccess -= OnGetCustomEntityPageSuccess; 
    }
}
