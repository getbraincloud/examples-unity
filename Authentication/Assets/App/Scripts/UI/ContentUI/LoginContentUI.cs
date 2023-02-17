using UnityEngine;

/// <summary>
/// Used for app navigation on the Login screen.
/// </summary>
public class LoginContentUI : ContentUIBehaviour
{
    [SerializeField] private MainLoginPanelUI MainLoginPanel = default;

    private void OnEnable()
    {
        MainLoginPanel.IsInteractable = true;
    }

    private void OnDisable()
    {
        MainLoginPanel.IsInteractable = false;
    }

    #region UI

    public void ResetRememberUserPref()
    {
        MainLoginPanel.SetRememberMePref(false);
    }

    protected override void InternalResetUI()
    {
        //
    }

    #endregion
}
