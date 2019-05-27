# TicTacToe

Async multiplayer, cloud code and RTT example. 




# [brainCloud AyncMatchService + RTT](https://getbraincloud.com/apidocs/tutorials/unity-tutorials/braincloud-ayncmatchservice-rtt/)
Already have an existing app using brainCloud’s AsyncMatchService, and would like to get Real-Time Updates? Do you want to support offline Async matches, as well as give the ability of real-time updates to that game? 

Ie. You want to create, or have a Tic Tac Toe type game, whereby most turns are taken without the need for users to be BOTH online, but also allow the game to progress in near real-time if both all users are online?

A game exemplifying this feature set is, Wargroove. Wargroove allows offline matches to be created, acted upon, and completed while both users are not online together. However, if both users are online at the same time, gameplay acts out as if its a real time turn based game.

## Required Materials

### brainCloud 4.0 
https://github.com/getbraincloud/braincloud-csharp

### Unity 2018.3.9
https://unity3d.com/get-unity/download/archive

## Supporting Materials

Up to date and previous versions of Tic Tac Toe are available here. Tic Tac Toe, previously was a pure Async Match supported the game. All turns would be done, and the app would have to poll or provide a user interaction to get the updated status on matches. By extending brainCloud's RTT service to provide real-time updates to online users of a match, there is no need, for the app to poll, or need to provide a user interactable to refresh the status on matches since the app will receive real-time notification of updates. If the user was offline, all gameplay will be just as it was before.


## Tic Tac Toe Example
https://github.com/getbraincloud/examples-unity/tree/master/TicTacToe

## Enable RTT
From the brainCloud Portal, select the app you wish to add RTT to. Select the Real-time Tech (RTT) Enabled checkbox from **Design | Core App Info | Advanced Settings**. https://sharedprod.braincloudservers.com/admin/dashboard#/development/core-settings-advanced-settings 

```
// Enable RTT
private void enableRTT()
{
    // Only Enable RTT if it's not already started
    if (!BcWrapper.Client.IsRTTEnabled())
    {
        BcWrapper.Client.EnableRTT(eRTTConnectionType.WEBSOCKET, onRTTEnabled, onRTTFailure);
    }
    else
    {
        // its already started, let's call our success delegate 
        onRTTEnabled("", null);
    }
}

// RTT enabled, ensure we now request the updated match state
private void onRTTEnabled(string responseData, object cbPostObject)
{
    queryMatchState();
    // LISTEN TO THE ASYNC CALLS, when we get one of these calls, let's just refresh 
    // match state
    BcWrapper.Client.RegisterRTTAsyncMatchCallback(queryMatchStateRTT);
}

// the listener, can parse the json and request just the updated match 
// in this example, just re-request it all
private void queryMatchStateRTT(string in_json)
{
    queryMatchState();
}

private void queryMatchState()
{
    BcWrapper.MatchMakingService.FindPlayers(RANGE_DELTA, NUMBER_OF_MATCHES, OnFindPlayers);
}

private void onRTTFailure(int status, int reasonCode, string responseData, object cbPostObject)
{
    // TODO! Bring up a user dialog to inform of poor connection
    // for now, try to auto connect 
    Invoke("enableRTT", 5.0f);
}
```



Enabling RTT, and activating a listener for the Async Match Service, allows for real-time messages to be acted upon from within the client. `queryMatchState()` used to be controlled via user interaction. By connecting this to an RTT listener, we can provide the user with a SEAMLESS interaction into both a pure offline Async Match and its real-time updates. 
