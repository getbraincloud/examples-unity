using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmySelectionButton : MonoBehaviour
{
    public ArmyDivisionRank ArmyRank;
    public ArmyType ArmyType;
    
    public void OnArmySelected()
    {
        if (ArmyType == ArmyType.Invader)
        {
            GameManager.Instance.CurrentUserInfo.InvaderSelected = ArmyRank;
            GameManager.Instance.InvaderSpawnData.AssignSpawnList(ArmyRank);
        }
        else
        {
            GameManager.Instance.CurrentUserInfo.DefendersSelected = ArmyRank;
        }
        BrainCloudManager.Instance.UpdateEntity();
        MenuManager.Instance.UpdateButtonSelectorPosition(ArmyType);
    }
}
