using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the content of brainCloud's <see cref="ChannelInfo"/> in a Prefab.
/// </summary>
public class ChannelButton : MonoBehaviour
{
    [SerializeField] private Button OpenButton = default;
    [SerializeField] private TMP_Text ChannelNameText = default;

    public ChannelInfo ChannelInfo { get; private set; }
    public Action<ChannelInfo> ButtonAction { get; private set; }

    public bool IsActiveChannel
    {
        get => (ChannelNameText.fontStyle & FontStyles.Bold) != 0;
        set => ChannelNameText.fontStyle = value ? ChannelNameText.fontStyle | FontStyles.Bold :
                                                ChannelNameText.fontStyle & ~FontStyles.Bold;
    }

    #region Unity Messages

    private void Awake()
    {
        ChannelNameText.text = string.Empty;
    }

    private void OnEnable()
    {
        OpenButton.onClick.AddListener(OnOpenChannelButton);
    }

    private void OnDisable()
    {
        OpenButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        ButtonAction = null;
    }

    #endregion

    #region UI

    public void SetChannelInfo(ChannelInfo info, Action<ChannelInfo> onChannelButtonAction)
    {
        gameObject.SetName("{0}{1}", info.name, "ChannelButton");
        ChannelNameText.text = $"#{info.name}";
        ButtonAction = onChannelButtonAction;
        ChannelInfo = info;
    }

    private void OnOpenChannelButton()
    {
        ButtonAction?.Invoke(ChannelInfo);
    }

    #endregion
}
