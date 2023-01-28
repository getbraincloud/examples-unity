using BrainCloud;
using BrainCloud.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Entity Interface class demonstrates how to handle JSON requests and responses from braincloud
/// when handling User Entities.
/// 
/// This includes:
/// - How to create entity
/// - How to read entity with ID received from a JSON response
/// - How to update entity 
/// - How to delete entity
/// 
/// For more info:
/// https://getbraincloud.com/apidocs/portal-usage/user-monitoring/user-entities/
/// https://getbraincloud.com/apidocs/cloud-code-central/cloud-code-tutorials/cloud-code-tutorial3-working-with-entities/
/// </summary>
public class EntityServiceUI : MonoBehaviour, IServiceUI
{
    private const string DEFAULT_EMPTY_FIELD = "---";
    private const string DEFAULT_ENTITY_ID = "user";

    [Header("Main")]
    [SerializeField] private CanvasGroup UICanvasGroup = default;
    [SerializeField] private TMP_Text IDField = default;
    [SerializeField] private TMP_Text TypeField = default;
    [SerializeField] private TMP_InputField NameField = default;
    [SerializeField] private TMP_InputField AgeField = default;
    [SerializeField] private Button FetchButton = default;
    [SerializeField] private Button CreateButton = default;
    [SerializeField] private Button SaveButton = default;
    [SerializeField] private Button DeleteButton = default;

    public bool IsInteractable
    {
        get
        {
            return UICanvasGroup.interactable;
        }
        set
        {
            UICanvasGroup.interactable = value;
        }
    }

    private Entity userEntityData = default;
    private BrainCloudEntity entityService = default;

    #region Unity Messages

    private void Awake()
    {
        IDField.text = DEFAULT_EMPTY_FIELD;
        TypeField.text = DEFAULT_EMPTY_FIELD;
        NameField.text = string.Empty;
        AgeField.text = string.Empty;

        entityService = BCManager.EntityService;
    }

    private void OnEnable()
    {
        FetchButton.onClick.AddListener(OnFetchButton);
        CreateButton.onClick.AddListener(OnCreateButton);
        SaveButton.onClick.AddListener(OnSaveButton);
        DeleteButton.onClick.AddListener(OnDeleteButton);
    }

    private void Start()
    {
        entityService.ToString();

        userEntityData = Entity.CreateEmpty();

        // TODO: Get Entity Data
    }

    private void OnDisable()
    {
        FetchButton.onClick.RemoveAllListeners();
        CreateButton.onClick.RemoveAllListeners();
        SaveButton.onClick.RemoveAllListeners();
        DeleteButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        entityService = null;
    }

    #endregion

    #region UI Functionality

    private void OnFetchButton()
    {

    }

    private void OnCreateButton()
    {

    }

    private void OnSaveButton()
    {

    }

    private void OnDeleteButton()
    {

    }

    #endregion

    #region Entity Functionality

    private void HandleServiceFunction()
    {

    }

    #endregion
}
