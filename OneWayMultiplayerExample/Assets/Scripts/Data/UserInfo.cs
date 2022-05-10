using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserInfo
{
    //Used to know if local user is hosting
    public string ProfileId;
    //Used for displaying and identifying users
    public string Username;
    
    public int Rating = 0;
    public int MatchesPlayed = 0;
    public int ShieldTime = 0;
    public ArmyDivisionRank InvaderSelected = 0;
    public ArmyDivisionRank DefendersSelected = 0;
    public string EntityId;
    
    public UserInfo() { }
}
