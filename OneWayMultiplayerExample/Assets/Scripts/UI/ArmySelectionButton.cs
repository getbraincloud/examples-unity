using UnityEngine;

public class ArmySelectionButton : MonoBehaviour
{
    public ArmyDivisionRank ArmyRank;
    public ArmyType ArmyType;
    
    public void OnArmySelected()
    {
        if (ArmyType == ArmyType.Invader)
        {
            GameManager.Instance.OnReadSetInvaderList(ArmyRank);
        }
        else
        {
            GameManager.Instance.CurrentUserInfo.DefendersSelected = ArmyRank;
        }
        NetworkManager.Instance.UpdateEntity();
        MenuManager.Instance.UpdateButtonSelectorPosition(ArmyType);
    }
}
