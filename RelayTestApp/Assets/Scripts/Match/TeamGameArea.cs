using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamGameArea : GameArea
{

    // Update is called once per frame
    void Update()
    {
        if (IsPointerOverUIElement())
        {
            if (Cursor.visible)
            {
                Cursor.visible = false;
                LocalUserCursor.AdjustVisibility(true);
            }
            
            SendMousePosition();
            if (Input.GetMouseButtonDown(0))
            {
                //Save position locally for us to spawn in UpdateAllShockwaves()
                _localShockwavePositions.Add(_newPosition+ _cursorOffset);
                
                //Send Position to local players team
                BrainCloudManager.Instance.SendShockwaveToTeam(_newPosition + _cursorOffset);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                //Save position locally for us to spawn in UpdateAllShockwaves()
                _localShockwavePositions.Add(_newPosition+ _cursorOffset);
                
                //Send Position to opposite team
                BrainCloudManager.Instance.SendShockwaveToOpponents(_newPosition + _cursorOffset);
            }
        }
        else
        {
            if (!Cursor.visible)
            {
                Cursor.visible = true;
                LocalUserCursor.AdjustVisibility(false);
            }
        }
        UpdateAllCursorsMovement();
        UpdateAllShockwaves();
    }
}
