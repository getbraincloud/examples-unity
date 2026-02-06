using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Managers;

public class PlayerResultItem : MonoBehaviour
{
	public Text placementText;
	public Text nameText;
    public Text timeText;

    // used in the leaderboard as a result display, could be used in the end screen as well
    public Image kartDisplay;
    public Texture2D[] kartDisplayReferences;
        

    public void SetResult(string name, float time, int place, int kartId = -1)
    {
        placementText.text = 
            place == 1 ? "1st" :
            place == 2 ? "2nd" :
            place == 3 ? "3rd" :
            $"{place}th";   
        nameText.text = name;
        timeText.text = $"{(int)(time / 60):00}:{time % 60:00.000}";

        // if kartDisplay is to be set
        if (kartDisplay != null && kartId >= 0)
        {
            Texture2D tex = kartDisplayReferences[kartId];
            if (tex != null)
            {
                kartDisplay.sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f)
                );
            }
        }
    }
}
