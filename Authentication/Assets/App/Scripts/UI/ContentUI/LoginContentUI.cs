using UnityEngine;

/// <summary>
/// Used for functionality on the Login screen.
/// </summary>
public class LoginContentUI : ContentUIBehaviour
{
    [Header("Main")]
    [SerializeField] private MainLoginPanelUI MainLoginPanel = default;

    #region Unity Messages

    private void OnEnable()
    {
        MainLoginPanel.IsInteractable = true;
    }

    private void OnDisable()
    {
        MainLoginPanel.IsInteractable = false;
    }

    #endregion

    #region UI

    public void ResetRememberUserPref()
    {
        MainLoginPanel.SetRememberMePref(false);
    }

    protected override void InitializeUI()
    {
        MainLoginPanel.ResetUI();
    }

    #endregion
}
