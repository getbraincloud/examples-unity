using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserCursor : MonoBehaviour
{
    public TMP_Text _usernameText;
    public Image _cursor;
    
    public Image Cursor => _cursor;

    public void SetUpCursor(Color cursorColor,string username)
    {
        _cursor.color = cursorColor;
        _usernameText.color=cursorColor;
        _usernameText.text = username;
        
    }
}
