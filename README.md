# UnityExamples

This repository contains example Unity projects that use the brainCloud client — an excellent place to start learning how the various brainCloud APIs are used.

---

## Copying an example via templates

Some of our examples are a bit more complicated and require brainCloud dashboard side configurations.
These examples include Bombers, BombersRTT, SpaceShooterWithState, and TicTacToe.

Create a new app using a template of one of these examples.

In Unity select **brainCloud | Select Settings** from the menu.

<p align="center">
  <img  src="./screenshots/step_selectsettings.png?raw=true">
</p>

In the brainCloud Settings, login to your current account or signup for the first time. _brainCloud is free during development :)_

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

---

## Authentication

This example demonstrates how to call methods in the following modules:

-   Authentication

    -   Email
    -   Universal
    -   Anonymous
    -   Google - http://getbraincloud.com/apidocs/portal-usage/authentication-google/

-   Player Entities
-   XP/Currency
-   Player Statistics
-   Global Statistics
-   Cloud Code

You can find more information about how to run the example here:

http://getbraincloud.com/apidocs/tutorials/unity-tutorials/unity-authentication-example/

---

## AuthenticationErrorHandling

This example demonstrates various error handling cases around authentication.
Use it to experiment with authentication error states.

---

## BombersRTT

Note: the BombersRTT example expects your app to have a [brainCloud plus plan enabled](#enabling-hosting-on-the-bombersrtt-example).

BombersRTT is a real-time multiplayer game implemented using brainCloud. brainCloud provides the backend services for storing data, as well as the Matchmaking and brainCloud's Relay Multiplayer server.

[Demo Link](http://apps.braincloudservers.com/bombersrtt-demo/index.html)

For more information, see this [README.mb](https://github.com/getbraincloud/examples-unity/blob/master/BombersRTT/README.md) file.

**Note** Compiler Flags

-   DEBUG_LOG_ENABLED - Enable logs via GDebug Class.
-   STEAMWORKS_ENABLED - Enable Steam SDK integration, must have the steam client open on a desktop to use via the editor
-   BUY_CURRENCY_ENABLED - Enable store product purchasing. PC / Mac / standalone builds require STEAMWORKS_ENABLED flag

---

## SpaceShooterWithStats

The Getting Started With Unity video uses the Space Shooter example as a backing project.

Find more information, including the video itself here:

http://getbraincloud.com/apidocs/tutorials/unity-tutorials/unity-tutorial-1-getting-started/

---

## TicTacToe

Async multiplayer and cloud code example.

For more information, see this [README.mb](https://github.com/getbraincloud/examples-unity/blob/master/TicTacToe/README.md) file.

---

## PushNotifications

Example of connecting Firebase FCM Notifications to brainCloud.

Find more information here:

http://getbraincloud.com/apidocs/portal-usage/push-notification-setup-firebase/

## OldGoogleAuth

Example of authenticating with brainCloud using a Google account. Inside there is a pdf tutorial describing how to go about setting up a google console app, and using firebase to successfully connect the apps to your brainCloud app!

This method of authenticating with google is NOT recommended, and only works for Android. We highly recommend you looking into our GoogleOpenId example instead, and setting up your google authentication using the googleOpenId. 

## GoogleOpenId

Example of authenticating with brainCloud using the GoogleOpenId. Inside there is a pdf tutorial describing how to go about setting up a google console app, and successfully connect the app to brainCloud. Instead of a google account specifically, you will be signing in with your googleOpenId. This is more flexible and can be used for IOS and Android devices as well. Setup is explained in the tutorial.

Inside this project folder you will also find a project folder for the same project made on an Apple device. Mostly everything is the same in Unity, but the apple project need to build through Xcode. Refer to the tutorial for further details. 

## AppleAuthentication

Example of authenticating with brainCloud using an Apple account. Inside there is a pdf tutorial describing the steps you will need to take in order to properly connect your Apple developer's app and braincloud app. This is for IOS only.

## BCAmazonIAP

Example app demonstrating Amazon In-App Purchases and verification with BrainCloud! Inside there is a pdf tutorial describing the steps you will need to take to get going!

### Additonal information

This code example is based on the Firebase Unity Quickstart Project
https://github.com/firebase/quickstart-unity

Can see more FCM documentation here: https://firebase.google.com/docs/cloud-messaging/android/client

Project currently only contains a push notifications example for Android FCM

-   Add your google-services.json file to the asset folder. Found on the Firebase dashboard
-   Update the brainCloud AppId and Secret on BrainCloudSettings to match yours. Id and Secret found on the brainCloud Dashboard
-   Download and import the Auth and Messaging Firebase Unity SDK
    https://firebase.google.com/docs/unity/setup
