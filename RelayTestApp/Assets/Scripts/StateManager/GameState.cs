using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameStates{SignIn,LoggedIn,Lobby,Match,Connecting}

public class GameState : MonoBehaviour
{
    public GameStates currentGameState;
   

}
