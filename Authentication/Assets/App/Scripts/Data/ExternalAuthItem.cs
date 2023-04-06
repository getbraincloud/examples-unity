using UnityEngine;

/// <summary>
/// <see cref="ScriptableObject"/> on setting up external authentication methods.
/// </summary>
[CreateAssetMenu(fileName = "AuthItem", menuName = "ScriptableObjects/External Authentication Item")]
public class ExternalAuthItem : ScriptableObject
{
    [SerializeField] private string AuthName = string.Empty;
    [SerializeField] private Sprite AuthIcon = default;
    [SerializeField] private Color ButtonLabelColor = Color.white;
    [SerializeField] private Color ButtonIconColor = Color.white;
    [SerializeField] private Color ButtonBackgroundColor = Color.black;

    /// <summary>
    /// Title of the external authentication method.
    /// </summary>
    public string Name => AuthName;

    /// <summary>
    /// Icon for the external authentication method.
    /// </summary>
    public Sprite Icon => AuthIcon;

    /// <summary>
    /// Color for the text label.
    /// </summary>
    public Color LabelColor => ButtonLabelColor;

    /// <summary>
    /// Color for the button icon.
    /// </summary>
    public Color IconColor => ButtonIconColor;

    /// <summary>
    /// Color of the button.
    /// </summary>
    public Color BackgroundColor => ButtonBackgroundColor;
}
