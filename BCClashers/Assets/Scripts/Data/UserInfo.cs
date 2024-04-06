
public class UserInfo
{
    //Used to know if local user is hosting
    public string ProfileId;
    //Used for displaying and identifying users
    public string Username;
    
    public int Rating = 0;
    public int MatchesPlayed = 0;
    public int ShieldTime = 0;
    public int GoldAmount = 0;
    public ArmyDivisionRank InvaderSelected = 0;
    public ArmyDivisionRank DefendersSelected = 0;
    public string EntityId;
    
    public UserInfo() { }
}

public class StreamInfo
{
    public string PlaybackStreamID;
    public int SlayCount;
    public int DefeatedTroops;
    public bool DidInvadersWin;
    public string InvaderPlayerName;

    public StreamInfo() { }
}
