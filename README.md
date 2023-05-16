# brainCloud Unity Examples

This repository contains example Unity projects that use the brainCloud client — an excellent place to start learning how the various brainCloud APIs are used.

These examples are meant to be used as code examples and references. These apps will NOT work outside of the box without proper set-up of a brainCloud app. Additionally, several apps also require you to set up a project on a platform back end, such as the Google Play console or Apple developer portal. Feel free to use our code as an example for your own code.

---

### Copying an example via brainCloud Templates

Some of our examples are a bit more complicated and require brainCloud dashboard side configurations. These examples include _Authentication_, _BombersRTT_, _SpaceShooterWithStats_, and _TicTacToe_.

Create a new app using a template of one of these examples:

1. In Unity select `brainCloud > Settings` from the menu

<p align="center">
    <img  src="./_screenshots/bcSettings.png?raw=true">
</p>

2. In the brainCloud Settings window, login to your current account or signup for the first time
    - Note: brainCloud is free during development!

3. With your team selected, select **Create New App ...** and check **Create with template?**

4. Select the template you wish to use. If using the SpaceShooter example, check the SpaceShooter template

<p align="center">
    <img  src="./_screenshots/bcTemplate.png?raw=true">
</p>

5. After creating the new app, you can review the imported data on the brainCloud dashboard
    - The SpaceShooter example imports stats which can be viewed on the `Design > Statistics Rules > User Statistics` [page](https://portal.braincloudservers.com/admin/dashboard?custom=null#/development/stats-player)

<p align="center">
    <img  src="./_screenshots/step_newstats.png?raw=true">
</p>

---

## Authentication

Authentication has been updated with a new look! Check out the [Authentication README.md](./Authentication/README.md) for more information. This app will be updated as new features and authentication methods are added to brainCloud.


## BCAmazonIAP

Example app demonstrating Amazon In-App Purchases and verification with BrainCloud! Inside there is a [PDF](./BCAmazonIAP/amazonIAPTutorial.pdf) describing the steps you will need to take to get going!


## BombersRTT

This example is a real-time multiplayer game implemented using brainCloud. brainCloud provides the backend services for storing data, as well as the **Matchmaking** and brainCloud's **Relay Multiplayer** server.

Go to the **SplashScreen** scene and run it to test it out in the editor!

For more information, check out the [BombersRTT README.md](./BombersRTT/README.md) file.

Demo Link: http://apps.braincloudservers.com/bombersrtt-demo/index.html

#### Enable Hosting

The BombersRTT example expects your app to have a brainCloud plus plan enabled.

This form of hosting requires our **plus** plans, so to entirely run the example on your copy, you need to enable the **DEVELOPMENT PLUS** plan or higher. Find the option on the `Team > Manage > Apps` [page](https://portal.braincloudservers.com/admin/dashboard#/support/apps), and click **[Go Live!]**.

#### Compiler Flags

- `DEBUG_LOG_ENABLED` - Enable logs via GDebug Class

- `STEAMWORKS_ENABLED` - Enable Steam SDK integration, must have the steam client open on a desktop to use via the editor

- `BUY_CURRENCY_ENABLED` - Enable store product purchasing – PC / Mac / Standalone builds require the `STEAMWORKS_ENABLED` flag


## GoogleIAP

An example of making a purchase with Google, and verifying it with braincloud. Refer to the [PDF](./GoogleIAP/GooglePurchasesTutorial.pdf) for proper setup.


## PushNotifications

An example of connecting **Firebase FCM Notifications** to brainCloud.

Read more information here: http://getbraincloud.com/apidocs/portal-usage/push-notification-setup-firebase/

#### Additional Information

This code example is based on the [Firebase Unity Quickstart Project](https://github.com/firebase/quickstart-unity).

You can see more FCM documentation here: https://firebase.google.com/docs/cloud-messaging/android/client

Project currently only contains a push notifications example for Android FCM
- Add your google-services.json file to the asset folder. Found on the Firebase dashboard
- Update the brainCloud AppId and Secret on BrainCloudSettings to match yours. Id and Secret found on the brainCloud Dashboard
- Download and import the Auth and Messaging Firebase Unity SDK
- Read more: https://firebase.google.com/docs/unity/setup


## SpaceShooterWithStats

The _Getting Started With Unity_ video uses the Space Shooter example as a backing project.

Go to the **BrainCloudConnect** scene and run the app to test it out in the editor!

Find more information, including the video itself, here: http://getbraincloud.com/apidocs/tutorials/unity-tutorials/unity-tutorial-1-getting-started/


## TicTacToe

Showcases brainCloud's **Async Multiplayer** and **Cloud Code** features.

Open up the two player scene to see it operate in side-by-side action, or build the one player scene and run two different instances of the game to test it out!

For more information, see the [TicTacToe README.md](./TicTacToe/README.md) file.

---

For more information on brainCloud and its services, please checkout the [brainCloud Docs](https://getbraincloud.com/apidocs/) and [API Reference](https://getbraincloud.com/apidocs/apiref/?csharp#introduction).
