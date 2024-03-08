# brainCloud Unity Examples

This repository contains example Unity projects that make use of the [brainCloud client](https://github.com/getbraincloud/braincloud-csharp) — an excellent place to start learning how the various [brainCloud APIs](https://getbraincloud.com/) are used!

These projects are meant to be used as code examples and references. Feel free to use our code as an example for your own code. All projects, unless stated otherwise, make use of **Unity 2022.3**.

Note: These projects will not work outside of the box without proper set-up of a brainCloud app. Additionally, several projects also require you to set up a project on a platform backend, such as the Google Play console or Apple Developer portal.

---

### Copying an example via brainCloud Templates

Some of our examples are a bit more complicated and require brainCloud dashboard side configurations. These examples include _Authentication_, _BombersRTT_, _SpaceShooterWithStats_, and _TicTacToe_.

Create a new app using a template of one of these examples:

1. In Unity select `brainCloud > Settings` from the menu

<p align="center">
    <img  src="./_screenshots/1_bcSettings.png?raw=true">
</p>

2. In the brainCloud Settings window, login to your current account or signup for the first time
    - Note: brainCloud is free during development!

3. With your team selected, select `-- Create New App --` and check `Create using Tutorial template`

4. Select the template you wish to use
    - If using the SpaceShooter example, select the SpaceShooter template

<p align="center">
    <img  src="./_screenshots/2_bcTemplate.png?raw=true">
</p>

5. After creating the new app, you can review the imported data on the brainCloud dashboard
    - The SpaceShooter example imports stats which can be viewed on the `Design > Cloud Data > User Statistics` page

<p align="center">
    <img  src="./_screenshots/3_bcStats.png?raw=true">
</p>

---

## Authentication

Authentication has been updated with a new look! Check out the [Authentication README.md](./Authentication/README.md) for more information. This example will be updated as new features and authentication methods are added to brainCloud.

---

## BCChat

An example demonstrating the **Chat** service on brainCloud, which works on apps making use of brainCloud RTT.

Be sure to enable RTT on your app in brainCloud in order to test the example properly.

---

## BCClashers

This example showcases the **One-Way Match** and **Playback Stream** services in brainCloud. Check out the [Clashers README.md](./brainCloud%20Clashers/README.md) for more information.

---

## BombersRTT

This example is a real-time multiplayer game implemented using brainCloud. brainCloud provides the backend services for storing data, as well as the **Matchmaking** service and brainCloud's **Relay Multiplayer** server.

Go to the **SplashScreen** scene and run it to test it out in the editor!

For more information, check out the [BombersRTT README.md](./BombersRTT/README.md) file.

- [Demo Link](http://apps.braincloudservers.com/bombersrtt-demo/index.html)

### Enable Hosting

This form of hosting requires our **plus** plans, so to entirely run the example on your copy, you will need to enable the **DEVELOPMENT PLUS** plan or higher. Find the option on the `Team > Manage > Apps` [page](https://portal.braincloudservers.com/admin/dashboard#/support/apps), and click **[Go Live!]**.

### Compiler Flags

- `DEBUG_LOG_ENABLED` - Enable logs via GDebug Class
- `STEAMWORKS_ENABLED` - Enable Steam SDK integration, must have the steam client open on a desktop to use via the editor
- `BUY_CURRENCY_ENABLED` - Enable store product purchasing – PC / Mac / Standalone builds require the `STEAMWORKS_ENABLED` flag

---

## Invaders

New example showcasing how to use brainCloud to faciliate connections between users and a server. Check out the [Invaders README.md](./Invaders/README.md) for more information.

---

## Push Notifications & Marketplace

This example showcases how to push notifications through brainCloud using the Firebase Messaging plugin for Android and Apple Push Services for iOS. It also showcases brainCloud's Marketplace features for In-App Purchases. Check out the [Marketplace README.md](./Marketplace/README.md) for more information.

---

## RelayTestApp

An example that showcases the **Matchmaking** and **Relay** services in brainCloud.

To set up lobby types as a **Global Property**, in the [brainCloud server portal](https://portal.braincloudservers.com/), navigate to `Design > Cloud Data > Global Properties`:
1. Press the **+** on the right side to create a new Global Property
2. **Name** and **Category** can be set to what you prefer
3. Ensure **Type** is set to `String`
4. **Value** should look like the following JSON:
```
{
  "0":{
    "lobby":"FreeForAllParty"
  },
  "1":{
    "lobby":"TeamParty"
  }
}
```

RelayTestApp is set up to look for the word **Team** in the lobby types, so if you want to test Team Mode in the example app, ensure your lobby type has the word **Team** in it. It will otherwise use **Free For All** mode by default.

## Disconnect/Reconnect Feature

You can follow the example code on how to reconnect a user that lost connection. There are also disconnect buttons that can be brought up:
- Logout and disconnect everything (RTT, Relay, and wipe any authenticated info)
- Re-initialize and Re-authenticate to then join back to the same room the User was disconnected from

There is also a button to just disconnect the RTT connection and reconnect only RTT. To set this up for your app, go to your lobby settings under `Design > Multiplayer > Lobbies` and add `{"enableDisconnectButton":true}` to the **Custom Config** for your lobby.

---

## SpaceShooterWithStats

The _Getting Started With Unity_ video uses the Space Shooter example as a backing project.

Go to the **BrainCloudConnect** scene and run the game to test it out in the editor!

Find more information, including the video itself, here: https://docs.braincloudservers.com/learn/sdk-tutorials/unity-tutorials/unity-getting-started/

---

## TicTacToe

Showcases brainCloud's **Async Multiplayer** and **Cloud Code** services.

Open up the **Start - TwoPlayer** scene to see it operate in side-by-side action, or build the **Start - OnePlayer** scene and run two different instances of the game to test it out!

For more information, see the [TicTacToe README.md](./TicTacToe/README.md) file.

---

For more information on brainCloud and its services, please check out [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/) and [API Reference](https://docs.braincloudservers.com/api/introduction).
