using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HockeyStatsDataUI : ContentUIBehaviour
{
    private const int MINIMUM_NAME_LENGTH = 4;
    private const string DEFAULT_POINTS_FIELD = "---";

    [Header("Main")]
    [SerializeField] private TMP_InputField NameField = default;
    [SerializeField] private TMP_Dropdown PositionDropdown = default;
    [SerializeField] private TMP_Text PointsField = default;
    [SerializeField] private TMP_InputField GoalsField = default;
    [SerializeField] private TMP_InputField AssistsField = default;

    [Header("Buttons")]
    [SerializeField] private Button UpdateButton = default;
    [SerializeField] private Button DeleteButton = default;

    public Action<HockeyStatsData> UpdateButtonAction = default;
    public Action DeleteButtonAction = default;

    private HockeyStatsData hockeyStats = default;

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
        NameField.onEndEdit.AddListener((name) => CheckNameVerification(name));
        PositionDropdown.onValueChanged.AddListener(OnPositionsDropdown);
        GoalsField.onEndEdit.AddListener((goals) => CheckPointsVerification(goals, GoalsField));
        AssistsField.onEndEdit.AddListener((assists) => CheckPointsVerification(assists, AssistsField));
        UpdateButton.onClick.AddListener(OnUpdateButton);
        DeleteButton.onClick.AddListener(OnDeleteButton);
    }

    protected override void Start()
    {
        PositionDropdown.AddOptions(new List<string>(HockeyStatsData.FieldPositions.Values));

        InitializeUI();

        base.Start();
    }

    private void OnDisable()
    {
        NameField.onEndEdit.RemoveAllListeners();
        PositionDropdown.onValueChanged.RemoveAllListeners();
        GoalsField.onEndEdit.RemoveAllListeners();
        AssistsField.onEndEdit.RemoveAllListeners();
        UpdateButton.onClick.RemoveAllListeners();
        DeleteButton.onClick.RemoveAllListeners();
    }

    protected override void OnDestroy()
    {
        UpdateButtonAction = null;
        DeleteButtonAction = null;

        base.OnDestroy();
    }

    #endregion

    #region UI

    public void UpdateUI(bool isOwned, HockeyStatsData hockeyStatsData)
    {
        hockeyStats = hockeyStatsData;
        NameField.text = hockeyStats.Name;
        PositionDropdown.value = (int)hockeyStats.Position;
        UpdatePointsDisplay();
        GoalsField.text = hockeyStats.Goals.ToString();
        AssistsField.text = hockeyStats.Assists.ToString();

        IsInteractable = isOwned;

        if (isOwned)
        {
            NameField.DisplayNormal();
            PositionDropdown.DisplayNormal();
            GoalsField.DisplayNormal();
            AssistsField.DisplayNormal();
            UpdateButton.DisplayNormal();
            DeleteButton.DisplayNormal();
        }
    }

    protected override void InitializeUI()
    {
        UpdateUI(false, new HockeyStatsData());
    }

    private void DisplayError(string error, Selectable problemSelectable = null)
    {
        if (problemSelectable != null)
        {
            problemSelectable.DisplayError();
        }

        Debug.LogError(error);
    }

    private bool CheckNameVerification(string value)
    {
        NameField.text = value.Trim();
        if (!NameField.text.IsEmpty())
        {
            if (NameField.text.Length < MINIMUM_NAME_LENGTH)
            {
                DisplayError($"Please use a name with at least {MINIMUM_NAME_LENGTH} characters.", NameField);
                return false;
            }

            hockeyStats.Name = value;
            return true;
        }

        return false;
    }

    private void OnPositionsDropdown(int option)
    {
        foreach (HockeyStatsData.FieldPosition pos in HockeyStatsData.FieldPositions.Keys)
        {
            if ((HockeyStatsData.FieldPosition)option == pos)
            {
                PositionDropdown.value = option;
                hockeyStats.PositionValue = option;
                return;
            }
        }

        OnPositionsDropdown(0); // If we somehow entered a value that doesn't exist, go to default
    }

    private void UpdatePointsDisplay()
    {
        PointsField.text = hockeyStats.GetPoints().ToString();
    }

    private bool CheckPointsVerification(string value, TMP_InputField inputField)
    {
        inputField.text = value.Trim();
        if (!inputField.text.IsEmpty())
        {
            if (int.TryParse(inputField.text, out int result))
            {
                result = result < 0 ? 0 : result;
                inputField.text = result.ToString();

                if (inputField == GoalsField)
                {
                    hockeyStats.Goals = result;
                }
                else // AssitsField
                {
                    hockeyStats.Assists = result;
                }
                
                UpdatePointsDisplay();
                return true;
            }

            DisplayError("Please enter a valid number.", inputField);
        }

        return false;
    }

    private void OnUpdateButton()
    {
        if (CheckNameVerification(NameField.text) &&
            CheckPointsVerification(GoalsField.text, GoalsField) &&
            CheckPointsVerification(AssistsField.text, AssistsField))
        {
            UpdateButtonAction?.Invoke(hockeyStats);
        }
    }

    private void OnDeleteButton()
    {
        DeleteButtonAction?.Invoke();
    }

    #endregion
}
