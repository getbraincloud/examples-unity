using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpModal : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _levelText;

    [SerializeField]
    private Button _continueButton;

    private Button _bgButton;

    private Animator _anim;
    private Action _onClosed;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _bgButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        _continueButton.onClick.AddListener(CloseModal);
        _bgButton.onClick.AddListener(CloseModal);
    }

    private void OnDisable()
    {
        _continueButton.onClick.RemoveAllListeners();
        _bgButton.onClick.RemoveAllListeners();
    }

    public void SetData(int newLevel, Action onClosed = null)
    {
        _levelText.text = newLevel.ToString();
        _onClosed = onClosed;
    }

    private void CloseModal()
    {
        _anim.SetBool("fadeOut", true);
    }

    public void OnFadeOutComplete()
    {
        _onClosed?.Invoke();
        Destroy(gameObject);
    }
}
