using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerResultItem : MonoBehaviour
{
	public Text placementText;
	public Text nameText;
	public Text timeText;

    public void SetResult(string name, float time, int place)
    {
        placementText.text = 
            place == 1 ? "1st" :
            place == 2 ? "2nd" :
            place == 3 ? "3rd" :
            $"{place}th";   
        nameText.text = name;
        timeText.text = $"{(int)(time / 60):00}:{time % 60:00.000}";
    }
}
