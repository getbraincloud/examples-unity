README

Overview

	This example describes an approach for implementing a Clash of Clans, Boom Beach, etc. type of game.
	There are many ways to solve this functionality, but in the following example, the player creates ReadOnly User Entities to define their defense and invasion information and then records specific events during gameplay to then replay the raid. In this example we only save one playback stream ID but it is possible to do this for more than one Playback Stream.

	In this example, users can select which set of troops for your invaders team and/or defenders team for their profile. 
	For simplicity the sets will be labeled similar to difficulty (easy,medium,hard). 
	Once selections have been made, user can look for other players to invade as long as they're in the same player rating range as other players, otherwise this will show up blank. 

	Once a player has been selected, user will load to a game scene level. 

	Game Scene Overview: 
	Here the objective is to destroy all the houses by summoning troops. Troops are set based on the Invader Difficulty selection setting. 
	- User can view how many troops for each type can be summoned during this raid and click on a type to select a type to summon. Once troop is selected, click any empty space on the ground.
		- Note - Users cant summon troops ontop of other troops or near the houses.
	- Each raid has a countdown timer.
	- After a raid, you can replay the raid by clicking "Replay Stream" on the game over screen OR from the main menu, you can replay the last raid by clicking "Replay Last Game".


Specific Classes

	NetworkManager.cs
		All the braincloud specific calls will be here which includes:
			- Login and switching users
			- Getting User entities from local or other users
			- Match Making
			- Creating/Modifying User Entities
			- Adjusting User Ratings
			- Recording Playback Stream events
			- Reading a Playback Stream from an ID

	PlaybackStreamManager.cs
		This class demonstrates how to perform a playback stream with specific events. You're welcome to add more events if needed.
			Events used during gameplay are:
				- Spawn
				- Destroy
				- Target Assignment
			After the game has completed, these events will execute:
				- Troop & Structure ID's 
				- Defender selection

Important Notes:

	- If you want players to see other players while they're online, be sure to enable that feature within the braincloud portal under Multiplayer -> Matchmaking -> Allow attack while online = TRUE.
	- Defining new user entities types can only be achieved with the CreateEntity() call. 

For more information:
	Article about designing offline multiplayer
	https://help.getbraincloud.com/en/articles/3272700-design-multiplayer-matchmaking
	API examples
	https://getbraincloud.com/apidocs/api-modules/multiplayer/one-way-offline-multiplayer-example/

	API References
		MatchMaking
		https://getbraincloud.com/apidocs/apiref/#capi-matchmaking

		Playback Stream
		https://getbraincloud.com/apidocs/apiref/#capi-playbackstream

		One-Way Match
		https://getbraincloud.com/apidocs/apiref/#capi-onewaymatch