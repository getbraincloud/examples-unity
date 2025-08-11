using System;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;

/// <summary>
/// Data for what information the popup should contain.
/// </summary>
public readonly struct PopupInfo
{
    private const string DEFAULT_DISMISS_TEXT = "Cancel";

    /// <summary>
    /// The title of the popup, shown in the header. Set to <see cref="string.Empty"/> to hide the header instead.
    /// </summary>
    public readonly string Title;

    /// <summary>
    /// A collection of what body texts the popup should contain.
    /// </summary>
    public readonly PopupInfoBody[] BodyTexts;

    /// <summary>
    /// A collection of what buttons the popup should contain.
    /// </summary>
    public readonly PopupInfoButton[] Buttons;

    /// <summary>
    /// Whether or not the popup can be dismissed.
    /// </summary>
    public readonly bool CanDismiss;

    /// <summary>
    /// If the popup can be dismissed, what the dismiss button text should be. Set to <see cref="string.Empty"/> for the default text (<see cref="DEFAULT_DISMISS_TEXT"/>).
    /// </summary>
    private readonly string AlternateDismissButtonText;

    /// <summary>
    /// A check for if the popup has a title or not.
    /// </summary>
    public readonly bool HasTitle => !Title.IsNullOrEmpty();

    /// <summary>
    /// Use this to get the text for the dismiss button. Will either be the default or <see cref="AlternateDismissButtonText"/> if it has been set.
    /// </summary>
    public readonly string DismissButtonText => AlternateDismissButtonText.IsNullOrEmpty() ? DEFAULT_DISMISS_TEXT : AlternateDismissButtonText;

    /// <summary>
    /// Display a default Popup UI which can be dismissed.
    /// </summary>
    /// <param name="title">The title of the popup, shown in the header. Set to <see cref="string.Empty"/> to hide the header instead.</param>
    /// <param name="bodyTexts">A collection of what body texts the popup should contain.</param>
    /// <param name="buttons">A collection of what buttons the popup should contain.</param>
    public PopupInfo(string title, PopupInfoBody[] bodyTexts, PopupInfoButton[] buttons)
    {
        Title = title;
        BodyTexts = bodyTexts;
        Buttons = buttons;
        CanDismiss = true;
        AlternateDismissButtonText = string.Empty;
    }

    /// <summary>
    /// Display a Popup UI where you can choose if it can be dismissed or not.
    /// </summary>
    /// <param name="title">The title of the popup, shown in the header. Set to <see cref="string.Empty"/> to hide the header instead.</param>
    /// <param name="bodyTexts">A collection of what body texts the popup should contain.</param>
    /// <param name="buttons">A collection of what buttons the popup should contain.</param>
    /// <param name="canDismiss">Whether or not the popup can be dismissed.</param>
    /// <param name="dismissButtonText">If the popup can be dismissed, what the dismiss button text should be. Set to <see cref="string.Empty"/> for the default text (<see cref="DEFAULT_DISMISS_TEXT"/>).</param>
    public PopupInfo(string title, PopupInfoBody[] bodyTexts, PopupInfoButton[] buttons, bool canDismiss, string dismissButtonText = "")
    {
        Title = title;
        BodyTexts = bodyTexts;
        Buttons = buttons;
        CanDismiss = canDismiss;
        AlternateDismissButtonText = dismissButtonText;
    }
}

/// <summary>
/// Data for what a body of text in the popup should look like.
/// </summary>
public readonly struct PopupInfoBody
{
    /// <summary>
    /// <para>Centered will have the text be centered.</para>
    /// <para>Justified will have the text be justified (fill the width).</para>
    /// <para>Error will have the text be centered and colored red.</para>
    /// </summary>
    public enum Type
    {
        Centered,
        Justified,
        Error
    }

    /// <summary>
    /// The text used for the body.
    /// </summary>
    public readonly string Text;

    /// <summary>
    /// What type the popup body text is.
    /// </summary>
    public readonly Type BodyType;

    /// <summary>
    /// Create body text for the popup.
    /// </summary>
    /// <param name="text">The text used for the body.</param>
    /// <param name="bodyType">What type the popup body text is.</param>
    public PopupInfoBody(string text, Type bodyType)
    {
        Text = text;
        BodyType = bodyType;
    }
}

/// <summary>
/// Data for what a button in the popup should look like.
/// </summary>
public readonly struct PopupInfoButton
{
    /// <summary>
    /// <para>Plain is a white button with a blue border and black text.</para>
    /// <para>The three colors are the three basic button colors used throughout the app.</para>
    /// <para>Note: The Red color is used for the Dismiss button.</para>
    /// </summary>
    public enum Color
    {
        Plain,
        Red,
        Blue,
        Green
    }

    /// <summary>
    /// The text used for the button's label.
    /// </summary>
    public readonly string Label;

    /// <summary>
    /// The color type of button.
    /// </summary>
    public readonly Color ButtonColor;

    /// <summary>
    /// What the button should do. If set to null, the default action is to dismiss the popup.
    /// </summary>
    public readonly Action OnButtonAction;

    /// <summary>
    /// Create a button for the popup.
    /// </summary>
    /// <param name="label">The text used for the button's label.</param>
    /// <param name="buttonColor">The color type of button.</param>
    /// <param name="onButtonAction">What the button should do. If set to null, the default action is to dismiss the popup.</param>
    public PopupInfoButton(string label, Color buttonColor, Action onButtonAction)
    {
        Label = label;
        ButtonColor = buttonColor;
        OnButtonAction = onButtonAction;
    }
}