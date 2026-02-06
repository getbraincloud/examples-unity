using System;
using UnityEngine;
using UnityEngine.UI;

public class MoveInButton : MonoBehaviour
{
    [SerializeField] private Button TriggerMysteryBoxButton;
    
    private ParentMenu _parentMenu;

    private void Awake()
    {
        TriggerMysteryBoxButton.onClick.AddListener(OnTriggerMysteryBoxButton);
        _parentMenu = FindFirstObjectByType<ParentMenu>();
    }
    
    private void OnTriggerMysteryBoxButton()
    {
        _parentMenu.OpenMysteryBoxPanel();
    }
}
