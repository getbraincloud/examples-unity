using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StatesEnum{SignIn,LoggedIn,Lobby,Match,Connecting}

public class State : MonoBehaviour
{
    public StatesEnum CurrentState;
   

}
