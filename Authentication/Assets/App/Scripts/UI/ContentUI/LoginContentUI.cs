using System.Collections;
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

    protected override void Start()
    {
        IsInteractable = false;

        StartCoroutine(InitialAppLoad());

        base.Start();
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

    private IEnumerator InitialAppLoad()
    {
        yield return null;

        if (MainLoginPanel.GetRememberMePref())
        {
            MainLoginPanel.HandleAutomaticLogin();
        }
        else
        {
            IsInteractable = true;
        }
    }

    #endregion
}
