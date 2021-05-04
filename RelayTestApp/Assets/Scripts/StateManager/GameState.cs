using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameStatesEnum{SignIn,LoggedIn,Lobby,Match,Connecting}

public class GameState : MonoBehaviour
{
    public GameStatesEnum currentGameState;
   

}
