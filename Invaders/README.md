# Invaders

<p align="center">
    <img  src="../_screenshots/x_Invaders.png?raw=true">
</p>

---

For more information on brainCloud and its services, please check out [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/) and [API Reference](https://docs.braincloudservers.com/api/introduction).

---

To change this demo's version label, navigate to **Edit** > **Project Settings...** > **Player** > **Version**.

---

# Using brainCloud�s Replay System to Add Virtual Teammates or Opponent Ghosts to Any Multiplayer Game

In the Invaders demo, recordings of player gameplay are saved to brainCloud, and then played back in future games to provide assistance to the live players. This gives the player the benefit of help from co-op multiplayer teammates even when no other players are readily available.

![GameWithActorsPreview](../_screenshots/_invadersReplay/_0_GameWithActorsPreview.png)

In our demonstration, each player�s personal best is saved, with the best players� replays offered back to the player by way of a leaderboard. When players start a round they will be able to bring replays of the top players into their live game, who will reenact the same gameplay that earned their high score.

![SelectorMenuPreview](../_screenshots/_invadersReplay/_1_SelectorMenuPreview.png)

There are 4 essential steps in this Playback Stream implementation:

1. [Record player gameplay](#record-player-gameplay)
2. [Send the recording to brainCloud for later use](#send-the-recording-to-braincloud)
3. [Retrieve the recording when it is needed](#retrieve-the-recording)
4. [Act out the recording during the game to create a teammate](#act-out-the-recording)

![ActorGetsDestroyed](../_screenshots/_invadersReplay/_2_ActorGetsDestroyed.gif)

## Record Player Gameplay

In the Invaders game, players are only allowed to move along the horizontal axis and shoot upwards. The less actions players have available to them, the easier this becomes to implement.

First we create a new object to hold the data we want to record each frame:
![FrameStruct](../_screenshots/_invadersReplay/_3_FrameStruct.png)

-   `xDelta` represents the direction the player moved each frame: left, right, or staying still.
-   `createBullet` represents whether or not the player shot out a bullet this frame.
-   `frameID` starts at `0` and increases by `1` every frame. This can be used for debugging.

We also create an object that holds some general information about the recording:
![RecordClass](../_screenshots/_invadersReplay/_4_RecordClass.png)

Next we create these objects inside our player controller when the game starts. We can also take this time to include some of the general information, such as the name of the player or the position the player starts in.
![RecordingAwake](../_screenshots/_invadersReplay/_5_RecordingAwake.png)
![RecordingStart](../_screenshots/_invadersReplay/_6_RecordingStart.png)

Now we need to keep track of which actions happen every frame. We do this in FixedUpdate() because this let us make sure the timing between frames stays consistent:
![RecordingFixedUpdate](../_screenshots/_invadersReplay/_7_RecordingFixedUpdate.png)

-   We get the player�s move direction by measuring where they are compared to where they were last frame.
-   We detect whether the player shot or not using shotRecently. This boolean is set to true when the player shoots and is reset back to false at the end of every frame.
-   _**Note**: We also want to keep track of the player�s score during gameplay to post to the leaderboard. In this demo, a player�s score is increased whenever they destroy an alien._

When the gameplay is over, we grab the score and record and move on to the next step. The gameplay could end for multiple reasons such as the player dying or the enemies reaching the bottom of the screen. To catch all of the different cases, the function can be called from `OnDestroy()`.  
![EndAndSubmitRecording](../_screenshots/_invadersReplay/_8_EndAndSubmitRecording.png)
![RecordingOnDestroy](../_screenshots/_invadersReplay/_9_RecordingOnDestroy.png)

## Send The Recording To brainCloud

This implementation makes frequent use of brainCloud�s Leaderboard service. The IDs of the saved recordings are put into the Extra Data of each player�s leaderboard entry. We only need to submit a recording if the score that it earned is the player�s highest so far.

To make the leaderboard for the app, navigate to App > Design > Leaderboards > Leaderboard Configs in the brainCloud portal. Add a new leaderboard config and remember your Leaderboard ID for later. In this demo it is �InvaderHighScore�.
![CreateLeaderboard](../_screenshots/_invadersReplay/_10_CreateLeaderboard.png)

We will first grab the player�s previous score from the leaderboard. If the recording�s score is less than the previous best, we break out of the function. This will save unnecessary API calls.
![SubmitRecordCoroutine](../_screenshots/_invadersReplay/_11_SubmitRecordCoroutine.png)
![GetPreviousBest](../_screenshots/_invadersReplay/_12_GetPreviousBest.png)

-   The game object this script is attached to persists throughout a session, so previousHighScore will only reset back to its default value of -1 when the game is closed and reopened.
-   _**Note:** In this case we compare previousScore to newScore using_ `>` _instead of_ `>=`. _This will favour the most recent recording in the case of a tie. Consider carefully how you filter which recordings you save._

Now that we know we want to save this stream, we create it on brainCloud using `StartStream()` and attach it to our leaderboard entry by putting the stream ID in the third argument of `PostScoreToLeaderboard()`.
![StartStreamAndPostScore](../_screenshots/_invadersReplay/_13_StartStreamAndPostScore.png)
![StartStream](../_screenshots/_invadersReplay/_14_StartStream.png)

We also need to add the gameplay that we recorded to the stream that was just created. To minimize the amount of data sent to brainCloud, we will compress multiple similar frames into one event.
![StartAddEvevnts](../_screenshots/_invadersReplay/_15_StartAddEvevnts.png)
![AddEventsCompression](../_screenshots/_invadersReplay/_16_AddEventsCompression.png)

-   The list `runLengths` will keep track of how many consecutive frames are similar to each other.
-   Players can only ever create a bullet for one frame at a time, so we will consider the frame a bullet was created to be similar to the immediately following frames. We must remember that only one bullet was created per event when we retrieve the stream from brainCloud later!
-   _**Note:** This type of compression may not be as effective for every type of gameplay. An effective compression algorithm takes advantage of expected patterns and assumptions in its data._

After calculating which frames will be grouped into events, we format the data and send the events to brainCloud. They must be written in JSON format.  
![AddEventsStrings](../_screenshots/_invadersReplay/_17_AddEventsStrings.png)
![AddEventsSendToCloud](../_screenshots/_invadersReplay/_18_AddEventsSendToCloud.png)
![AddEventsCounter](../_screenshots/_invadersReplay/_19_AddEventsCounter.png)

-   Each event will look like the following on the brainCloud portal:  
    ![EventExample](../_screenshots/_invadersReplay/_20_EventExample.png)

Now that we have created our stream and sent all of our recorded gameplay to brainCloud, we end the stream and delete the stream that was previously attached to the leaderboard.
![SubmitRecordCleanUp](../_screenshots/_invadersReplay/_21_SubmitRecordCleanUp.png)

-   We reset `createdRecordId` and `finishedAddingEvents` back to their default values so that the `SubmitRecord()` coroutine can be used again this session.
-   `previousHighScore` and `previousRecordId` are set to the score and ID we just submitted so that we don�t have to fetch them again next time!

The essential steps are halfway complete! At this point whenever a player completes a game and achieves a personal high score, you should be able to see their leaderboard entry with the ID of their replay under the Extra Data column. Find the leaderboard entries on the portal under App > Global > Leaderboards > Leaderboards.

To find the recordings and their events go to the User tab and browse for your desired user. Find the streams under User > Multiplayer > One-Way MP. The Match ID should be the same as the Extra Data in the leaderboard entry.
![FindReplayIdOnPortal](../_screenshots/_invadersReplay/_22_FindReplayIdOnPortal.png)

## Retrieve The Recording

When the app needs to retrieve a recording, we use the ReadStream operation from the Playback Stream service to get the data from brainCloud. We need to have the ID of the replay that will be read from. The Invaders demo gets the stream IDs by letting players select from leaderboard entries on a menu in the lobby before the game begins. Keep in mind that the way your recordings are selected is a design choice that will be unique to your app or game�s purpose.

-   _**Note:** An example of a different way to get replay IDs would be through the_ `GetRecentStreamsForInitiatingPlayer()` _operation._

The server will be in charge of instantiating the actors that play out the recordings, so we will read the stream data on the server. We send the replay IDs to the server using an RPC function.  
![SendReplayIdsToServer](../_screenshots/_invadersReplay/_23_SendReplayIdsToServer.png)

-   If your game uses peer to peer multiplayer or is singleplayer, this can be done client-side instead.

We will be using a cloud code script to read the streams, as there could be almost a dozen streams that need to be read at the same time. Grouping multiple operations together with a cloud script is cheaper than calling each operation individually from the app. We will send the replay IDs to the cloud script as an array called �stream_ids�.
![RequestReadMultipleStreams](../_screenshots/_invadersReplay/_24_RequestReadMultipleStreams.png)

-   If your app will only read one stream at a time, it may be more convenient to get the streams in the app and skip making a cloud script.
-   This request is being made by the server, which means it must use S2S instead of the Client API. Requests using S2S must be formatted in JSON.
-   The service and operation are �script� and �RUN� respectively. The data of the request is the parameters of the run operation, which includes the data that will be used by our cloud script.

To make a cloud script navigate to Design > Cloud Code > Scripts and press the �Create Script� button. For this demo, the script needs to be callable by S2S. The other settings can be left as their default values.
![CreateCloudScript](../_screenshots/_invadersReplay/_25_CreateCloudScript.png)
![CloudScriptSettings](../_screenshots/_invadersReplay/_26_CloudScriptSettings.png)

Cloud scripts access the parameters that were sent to them using properties of �data�. In our cloud script `data.stream_ids` is the array of IDs that will be turned into streams. The function `main()` returns `response`, which is the JSON that our app will receive. We add each stream to the response under the key �streams�.
![CloudScript](../_screenshots/_invadersReplay/_27_CloudScript.png)

-   This script uses `sysReadStream()` instead of `readStream()` because it is called from the server. Only operations that are supported by S2S can be called in cloud scripts that are run from a server.

Make sure to save the script when you are finished editing. If you already have some IDs from playtesting, you can test if the script works by pressing �DEBUG� and putting the IDs into the parameters section of the page. Press �QUICK AUTH� and then �EXECUTE�.
![CloudScriptDebugging](../_screenshots/_invadersReplay/_28_CloudScriptDebugging.png)

-   Make sure the parameters are formatted in the way the script expects them to be: inside an array with a key of �stream_ids�.

In the script�s callback function, we find the array of streams and loop through each one to start parsing out their data.
![ReceiveCloudScriptResponse](../_screenshots/_invadersReplay/_29_ReceiveCloudScriptResponse.png)

Here we will start parsing the data to transform it from JSON to something usable. We begin by checking if there is enough data to work with, and log a warning if we do not.
![ParseStreamCheckForNull](../_screenshots/_invadersReplay/_30_ParseStreamCheckForNull.png)

We fill out the general data from the summary first, then add in all of the frames from the events.
![ParseStreamDecompression](../_screenshots/_invadersReplay/_31_ParseStreamDecompression.png)

-   Within each stream is multiple events, and within each event is multiple frames. The quantity of frames in an event is determined by `runLength`. This is why the function includes one loop nested inside another.
-   A frame only creates a bullet when it is the first of its run length group, expressed here as `ii == 0`. This is one of the assumptions we made for higher compression in the previous step.

We hand the data that we parsed out to the next step, where it will be used to instantiate a "ghost actor".
![SendDataToSpawner](../_screenshots/_invadersReplay/_32_SendDataToSpawner.png)

## Act Out The Recording

After instantiating the prefab that will act out the recording, we pass the recording it will act out as well as how much time has passed since the start of the game.
![InstantiateActor](../_screenshots/_invadersReplay/_33_InstantiateActor.png)

-   The cloud may take a few milliseconds to respond to our requests, which will delay the instantiation of the ghost. We will compensate for this by keeping track of how many frames have passed and skipping those frames while acting.  
    ![AccountForActorDelay](../_screenshots/_invadersReplay/_34_AccountForActorDelay.png)
-   Fortunately, in the Invaders demo the first 150 frames will always be dedicated to waiting for the 3 second countdown to pass. This is plenty of time to ensure the delay will never affect gameplay.
-   _**Note:** Your game or app should also take into consideration a small amount of delay from cloud responses. This might be done via a loading screen or making predictions on the client._

There are two variables we can immediately use: `startPosition` and `username`. We also start a coroutine which will handle acting out each of the frames.  
![StartActing](../_screenshots/_invadersReplay/_35_StartActing.png)
![SetActorUsername](../_screenshots/_invadersReplay/_36_SetActorUsername.png)

-   Unity�s Netcode for GameObjects includes a component that automatically syncs the transform of the prefab. We apply the start position before the prefab is spawned on the network to avoid seeing the position interpolated.
-   We apply the username after the prefab is spawned because unlike the transform, we need to sync the text component through an RPC function.

For every frame we need to copy the direction the player moved as well as shoot a bullet if the player shot a bullet. We then wait for the next fixed update to act out the following frame.
![Acting](../_screenshots/_invadersReplay/_37_Acting.png)

-   The for loop starts at `startFrame`. This is what accounts for the delay from the cloud responses.

Once the recording has run out of frames, we call the retreat function. When the actor retreats, it moves downwards until it is sufficiently below the camera, then gets destroyed. Your game or app should have some fallback behaviour in case the data ends.
![ActorRetreat](../_screenshots/_invadersReplay/_38_ActorRetreat.png)

## Schedule a cloud code script to protect replays from deletion for top 10 and featured players

As per our protocol, replays are automatically purged once they reach a playbackStreamIntervalDays threshold of 180 days or older. However, in order to retain the replays of our top 10 dynamic players and featured players, it is imperative that we execute a nightly cloud code script. This script will update and safeguard the replays by invoking the `sysProtectStreamUntil` method. The script should resemble the code provided below.

```js
"use strict";

function main() {
    var response = {};

    bridge.logDebugJson("Script Inputs", data);

    const scriptName = data.scriptName;
    const args = data.args;
    const interval = data.args.interval;
    const numDays = data.args.daysToProtect;
    const searchSpan = interval < 60 ? 60 : 60 * 24;

    // schedule checking and re-schedule itself at the next day 23:59
    var scriptProxy = bridge.getScriptServiceProxy();

    var dateTimeSpanMinsFromNowInMillis =
        new Date().getTime() + searchSpan * 60 * 1000;
    var result = scriptProxy.getScheduledCloudScripts(
        dateTimeSpanMinsFromNowInMillis
    );

    var nowTime = new Date().getTime();

    if (result.status == 200 && result.data !== null) {
        for (var i = 0; i < result.data.scheduledJobs.length; i++) {
            if (
                result.data.scheduledJobs[i].scriptName === scriptName &&
                result.data.scheduledJobs[i].scheduledStartTime > nowTime
            ) {
                scriptProxy.cancelScheduledScript(
                    result.data.scheduledJobs[i].jobId
                );
            }
        }
    }

    const targetHour = 23;
    const targetMinute = 59;
    const minutesDifference = getMinutesDifference(targetHour, targetMinute);
    let minutesFromNow = minutesDifference + interval;
    bridge.logInfo(
        `minutesFromNow between the script calling time and the time of 23:59 in a same day${minutesFromNow}`
    );
    response.scheduleJob = scriptProxy.scheduleRunScriptMinutes(
        scriptName,
        args,
        minutesFromNow
    );

    // retrieve leaderboard to get the top 10 players profileId/replayStreamId to apply protection
    var leaderboardId = "InvaderHighScore";
    var sortOrder = "HIGH_TO_LOW";
    var startIndex = 0;
    var endIndex = 9;
    var leaderboardProxy = bridge.getLeaderboardServiceProxy();
    var playbackStreamProxy = bridge.getPlaybackStreamServiceProxy();
    var arrProfileIds = [];

    var postResult = leaderboardProxy.getGlobalLeaderboardPage(
        leaderboardId,
        sortOrder,
        startIndex,
        endIndex
    );
    if (postResult.status == 200) {
        // Success!
        response.top10ProtectCount = 0;
        postResult.data.leaderboard.forEach((item) => {
            arrProfileIds.push(item.playerId);
            var playbackStreamId = item.data.replay;
            playbackStreamProxy.sysProtectStreamUntil(
                playbackStreamId,
                numDays
            );
            response.top10ProtectCount++;
        });
    }

    // retrieve featured playerId and get player's replay streamId to apply protection if the featured player's replay is not saved from above step
    var propertyNames = ["FeaturedPlayer"];
    var globalAppProxy = bridge.getGlobalAppServiceProxy();

    var postResult = globalAppProxy.readSelectedProperties(propertyNames);
    if (postResult.status == 200) {
        // Success!
        var FeaturedPlayerID = postResult.data.FeaturedPlayer.value;
        bridge.logInfo(`FeaturedPlayerID: ${FeaturedPlayerID}`);

        if (arrProfileIds.indexOf(FeaturedPlayerID) !== -1) {
            response.featuredProtectCount = 0;
        } else {
            bridge.logInfo(
                `FeaturedPlayerID is not on the list of top 10 player`
            );
            var session = bridge.getSessionForProfile(FeaturedPlayerID);
            var playbackStreamProxy =
                bridge.getPlaybackStreamServiceProxy(session);
            var recentStreamResult =
                playbackStreamProxy.getRecentStreamsForInitiatingPlayer(
                    FeaturedPlayerID,
                    5
                );
            recentStreamResult.data.streams.forEach((item) => {
                var playbackStreamId = item.playbackStreamId;
                playbackStreamProxy.sysProtectStreamUntil(
                    playbackStreamId,
                    numDays
                );
                response.featuredProtectCount = 1;
            });
        }
    }

    return response;
}

function getMinutesDifference(targetHour, targetMinute) {
    const now = new Date();
    const targetTime = new Date(
        now.getFullYear(),
        now.getMonth(),
        now.getDate(),
        targetHour,
        targetMinute
    );
    const differenceInMilliseconds = targetTime - now;
    bridge.logInfo(
        `differenceInMilliseconds between the script calling time and the target time: ${differenceInMilliseconds}`
    );
    const differenceInMinutes = Math.floor(
        differenceInMilliseconds / (1000 * 60)
    );
    return differenceInMinutes;
}

main();
```
