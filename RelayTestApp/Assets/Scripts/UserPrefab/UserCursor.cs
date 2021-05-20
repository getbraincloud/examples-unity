using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserCursor : MonoBehaviour
{
    public TMP_Text UsernameText;
    public Image CursorImage;
    public void AdjustVisibility(bool isActive)
    {
        UsernameText.enabled = isActive;
        CursorImage.enabled = isActive;
    }
    public void SetUpCursor(Color cursorColor,string username)
    {
        CursorImage.color = cursorColor;
        UsernameText.color = cursorColor;
        UsernameText.text = username;
    }
}
