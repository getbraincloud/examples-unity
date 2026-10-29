using System;
using UnityEngine;
using UnityEngine.UI;

public class ProfileImageOption : MonoBehaviour
{
    [SerializeField]
    private Image _image;

    [SerializeField]
    private Button _button;

    private int _index;
    private Action<int> _onSelected;

    private void OnEnable()
    {
        _button.onClick.AddListener(OnClicked);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(OnClicked);
    }

    public void SetData(int index, Sprite sprite, Action<int> onSelected)
    {
        _index = index;
        _image.sprite = sprite;
        _onSelected = onSelected;
    }

    private void OnClicked()
    {
        _onSelected?.Invoke(_index);
    }
}
