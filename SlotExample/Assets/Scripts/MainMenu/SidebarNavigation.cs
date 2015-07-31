using UnityEngine;

public class SidebarNavigation : MonoBehaviour
{
    public enum Buttons { SlotGames, Account, Offers }

    private NavButton[] _navButtons;
    private NavButton _activeButton;

    private bool _init;

    void Awake()
    {
        _navButtons = GetComponentsInChildren<NavButton>();
        _init = true;
        Reset();
    }

    void OnEnable()
    {
        if (!_init) return;
        Reset();
    }
    private void Reset()
    {
        if (_activeButton) _activeButton.SetEnabled(false);
        _activeButton = _navButtons[0];
        _activeButton.SetEnabled(true);
    }

    private void OnButtonPress(Buttons pressedButton)
    {
        if (_navButtons[(int)pressedButton] == _activeButton) return;

        _activeButton.SetEnabled(false);
        _navButtons[(int)pressedButton].SetEnabled(true);
        _activeButton = _navButtons[(int)pressedButton];
    }

    public void SetActiveButton(Buttons pressedButton)
    {
        if (_activeButton) _activeButton.SetEnabled(false);
        _activeButton = _navButtons[(int)pressedButton];
        _activeButton.SetEnabled(true);
    }

    #region Buttton Click Callbacks
    public void SlotGamesBtnOnClick()
    {
        OnButtonPress(Buttons.SlotGames);
    }

    public void AccountBtnOnClick()
    {
        OnButtonPress(Buttons.Account);
    }

    public void OffersBtnOnClick()
    {
        OnButtonPress(Buttons.Offers);
    }
    #endregion
}