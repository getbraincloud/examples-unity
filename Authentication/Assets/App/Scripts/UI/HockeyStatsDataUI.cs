using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HockeyStatsDataUI : ContentUIBehaviour
{
    public static string DataType => HockeyStatsData.DataType;

    private const string DEFAULT_POINTS_FIELD = "---";

    [Header("Main")]
    [SerializeField] private TMP_InputField NameField = default;
    [SerializeField] private TMP_Dropdown PositionDropdown = default;
    [SerializeField] private TMP_Text PointsField = default;
    [SerializeField] private TMP_InputField GoalsField = default;
    [SerializeField] private TMP_InputField AssistsField = default;

    [Header("CustomEntityServiceUI Buttons")]
    [SerializeField] private Button UpdateButton = default;
    [SerializeField] private Button DeleteButton = default;

    #region Unity Messages

    protected override void Awake()
    {
        NameField.text = string.Empty;
        PointsField.text = DEFAULT_POINTS_FIELD;
        GoalsField.text = string.Empty;
        AssistsField.text = string.Empty;

        base.Awake();
    }

    private void OnEnable()
    {
        if (UpdateButton.onClick != null || DeleteButton.onClick != null)
        {
            OnDisable();
        }

        UpdateButton.onClick.AddListener(OnUpdateButton);
        DeleteButton.onClick.AddListener(OnDeleteButton);
    }

    protected override void Start()
    {
        InitializeUI();

        base.Start();
    }

    private void OnDisable()
    {
        UpdateButton.onClick.RemoveAllListeners();
        DeleteButton.onClick.RemoveAllListeners();
    }

    protected override void OnDestroy()
    {
        //

        base.OnDestroy();
    }

    #endregion

    #region UI

    public void UpdateUI(HockeyStatsData hockeyStatsData)
    {
        NameField.text = hockeyStatsData.Name;
        NameField.DisplayNormal();
        PointsField.text = hockeyStatsData.GetPoints().ToString();
        GoalsField.text = hockeyStatsData.Goals.ToString();
        GoalsField.DisplayNormal();
        AssistsField.text = hockeyStatsData.Assists.ToString();
        AssistsField.DisplayNormal();
    }

    protected override void InitializeUI()
    {
        UpdateUI(new HockeyStatsData());

        UpdateButton.DisplayNormal();
        DeleteButton.DisplayNormal();
    }

    private void OnUpdateButton()
    {
        //
    }

    private void OnDeleteButton()
    {
        //
    }

    #endregion

    #region brainCloud

    private void OnServiceFunction()
    {
        //
    }

    #endregion
}
