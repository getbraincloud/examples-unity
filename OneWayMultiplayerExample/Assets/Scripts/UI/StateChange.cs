using UnityEngine;

public class StateChange : MonoBehaviour
{
    //Called from Unity Button
    public void StateButtonChange() => MenuManager.Instance.ButtonPressChangeState();

    public void CancelToMainMenu() => MenuManager.Instance.ChangeState(MenuStates.MainMenu);

    public void ReplayLastBattle() => NetworkManager.Instance.ReplayStream();
}