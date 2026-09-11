using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProfileImagePickerModal : MonoBehaviour
{
    [SerializeField]
    private RectTransform _optionsContainer;

    [SerializeField]
    private ProfileImageOption _optionPrefab;

    [SerializeField]
    private Button _closeButton, _backgroundButton;

    private Animator _anim;
    private Action _onClosed;
    private readonly List<ProfileImageOption> _spawnedOptions = new();

    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        _closeButton.onClick.AddListener(CloseModal);
        _backgroundButton.onClick.AddListener(CloseModal);
    }

    private void OnDisable()
    {
        _closeButton.onClick.RemoveAllListeners();
        _backgroundButton.onClick.RemoveAllListeners();
    }

    public void SetData(Sprite[] images, Action<int> onSelected, Action onClosed = null)
    {
        _onClosed = onClosed;

        foreach (var option in _spawnedOptions)
        {
            if (option != null)
                Destroy(option.gameObject);
        }
        _spawnedOptions.Clear();

        if (images == null)
            return;

        for (int i = 0; i < images.Length; i++)
        {
            ProfileImageOption option = Instantiate(_optionPrefab, _optionsContainer);
            option.transform.localScale = Vector3.one;
            option.SetData(i, images[i], (int selectedIndex) =>
            {
                onSelected?.Invoke(selectedIndex);
                CloseModal();
            });
            _spawnedOptions.Add(option);
        }
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
