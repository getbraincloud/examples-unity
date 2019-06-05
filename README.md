# UnityExamples

This repository contains example Unity projects that use the brainCloud client — an excellent place to start learning how the various brainCloud APIs are used.

---

## Authentication

This example demonstrates how to call methods in the following modules:

- Authentication 
    - Email
    - Universal
    - Anonymous
    - Google -  http://getbraincloud.com/apidocs/portal-usage/authentication-google/

- Player Entities
- XP/Currency
- Player Statistics
- Global Statistics
- Cloud Code

You can find more information about how to run the example here:

http://getbraincloud.com/apidocs/tutorials/unity-tutorials/unity-authentication-example/

---

## AuthenticationErrorHandling

This example demonstrates various error handling cases around authentication. 
Use it to experiment with authentication error states.

---

## Bombers

Bombers is a real-time multiplayer game implemented using Photon and brainCloud. brainCloud provides the backend services for storing data, and Photon supplies the Matchmaking and Multiplayer server.

You can find more information on Bombers here:

http://getbraincloud.com/apidocs/tutorials/unity-tutorials/braincloud-bombers-example-game/

---

## BombersRTT

BombersRTT is a real-time multiplayer game implemented using brainCloud. brainCloud provides the backend services for storing data, as well as the Matchmaking and brainCloud's Relay Multiplayer server.

You can find more information on Bombers here:

http://getbraincloud.com/apidocs/tutorials/unity-tutorials/braincloud-bombers-example-game/

**Note** Compiler Flags

- DEBUG_LOG_ENABLED - Enable logs via GDebug Class.
- STEAMWORKS_ENABLED - Enable Steam SDK integration, must have the steam client open on a desktop to use via the editor
- BUY_CURRENCY_ENABLED - Enable store product purchasing. PC / Mac / standalone builds require STEAMWORKS_ENABLED flag

---

## SpaceShooterWithStats

The Getting Started With Unity video uses the Space Shooter example as a backing project.

Find more information, including the video itself here:

http://getbraincloud.com/apidocs/tutorials/unity-tutorials/unity-tutorial-1-getting-started/

---

## TicTacToe

Async multiplayer and cloud code example.

For more information see this [README.mb](https://github.com/getbraincloud/examples-unity/blob/master/TicTacToe/README.md) file.

---

## PushNotifications

Example of connecting Firebase FCM Notifications to brainCloud.

Find more information here:

http://getbraincloud.com/apidocs/portal-usage/push-notification-setup-firebase/

### Additonal information

This code example is based on the Firebase Unity Quickstart Project
https://github.com/firebase/quickstart-unity

Can see more FCM documentation here: https://firebase.google.com/docs/cloud-messaging/android/client


Project currently only contains a push notifications example for Android FCM

- Add your google-services.json file to the asset folder. Found on the Firebase dashboard
- Update the brainCloud AppId and Secret on BrainCloudSettings to match yours. Id and Secret found on the brainCloud Dashboard
- Download and import the Auth and Messaging Firebase Unity SDK
https://firebase.google.com/docs/unity/setup


---

## Copying an example via templates

Some of our examples are a bit more complicated and require brainCloud dashboard side configurations.
These examples include Bombers, BombersRTT, SpaceShooterWithState, and TicTacToe.

Create a new app using a template of one of these examples.

In Unity select **brainCloud | Select Settings** from the menu.

<p align="center">
  <img  src="./screenshots/step_selectsettings.png?raw=true">
</p>


In the brainCloud Settings, login to your current account or signup for the first time. *brainCloud is free during development :)*

With your team selected, select **Create New App ...** and check **Create with template?**
Select the template you wish to use. If using the SpaceShooter example, check the SpaceShooter template.

<p align="center">
  <img  src="./screenshots/step_spaceshootertemplate.png?raw=true">
</p>

After creating the new app, you can review the imported data on the brainCloud dashboard. The SpaceShooter example imports stats which can be view on the **Design | Statistics Rules | [User Statistics](https://portal.braincloudservers.com/admin/dashboard?custom=null#/development/stats-player)** page.

<p align="center">
  <img  src="./screenshots/step_newstats.png?raw=true">
</p>

### Enabling hosting on the BombersRTT example

The BombersRTT demonstrates an example using our relay server hosting.

This form of hosting requires our **plus** plans, so to entirely run the example on your copy, you need to enable the **DEVELOPMENT PLUS** plan or higher. Find the option on the **Team | Manage | [Apps](https://portal.braincloudservers.com/admin/dashboard#/support/apps)** page, and click **[Go Live!]**.