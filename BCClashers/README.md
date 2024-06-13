# brainCloud Clashers

<p align="center">
    <img  src="../_screenshots/x_BCClashers.png?raw=true">
    One-Way Multiplayer and Playback Streams
</p>

---

This example describes an approach for implementing a Clash of Clans, Boom Beach, etc., type of game using brainCloud's **One-Way Match** service.

There are many ways to solve this functionality but in this example project, the player will create **ReadOnly User Entities** to define their defense and invasion information and then record specific events during gameplay to replay the raid. This example will only save one playback stream ID but it is possible to do this for more than one playback stream.

Users can select which set of troops for your invaders team and/or defenders team for their profile. For simplicity, the sets will be labeled as **Difficulty** (Easy, Medium, or Hard) and within each difficulty set holds information such as troop prefab references and number of troops that can be summoned for each type.

Once selections have been made, a user can look for other players to invade as long as they're in the same player rating range as other players in the matchmaking process. Once a player has been selected, the user will load to a game scene level. 

## Game Overview

The objective is to destroy all of the houses by summoning troops. Troops are set based on the Invader Difficulty selection setting. 
- User can view how many troops for each type can be summoned during this raid and click on a type to select a type to summon. Once the troop is selected, click any empty space on the ground.
    - Note: Users cannot summon troops ontop of other troops or near the houses.
- Each raid has a countdown timer.
- After a raid, you can view a replay of the raid by clicking **Replay Stream** on the game over screen or from the main menu by clicking **Replay Last Game**.

## Important Classes

### NetworkManager.cs

This contains all brainCloud specific calls which includes:
- Login and switching users
- Getting User Entities from local or other users
- Match Making
- Creating/Modifying User Entities
- Adjusting User Ratings
- Recording Playback Stream events
- Reading a Playback Stream from an ID

### PlaybackStreamManager.cs

This class demonstrates how to perform a playback stream with specific events. You're welcome to add more events as needed.
Events used during gameplay are:
- Spawn
- Destroy
- Target Assignment

After the game has completed, these events will execute:
- Troop & Structure IDs 
- Defender selection

---

### Notes
- If you want players to see other players while they're online, be sure to enable that feature within the brainCloud portal under:
    - **Multiplayer** > **Matchmaking** > **Allow attack while online = TRUE**
- Defining new User Entity types can only be achieved with the `CreateEntity()` call. 

### brainCloud Resources

- [Designing Offline Multiplayer](https://help.getbraincloud.com/en/articles/3272700-design-multiplayer-matchmaking)
- [One Way Offline Example](https://docs.braincloudservers.com/learn/key-concepts/multiplayer/#one-way-offline-multiplayer)

### API References

- [Matchmaking](https://docs.braincloudservers.com/api/capi/matchmaking/)
- [Playback Stream](https://docs.braincloudservers.com/api/capi/playbackstream/)
- [One-Way Match](https://docs.braincloudservers.com/api/capi/playbackstream/)

---

For more information on brainCloud and its services, please check out [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/) and [API Reference](https://docs.braincloudservers.com/api/introduction).
