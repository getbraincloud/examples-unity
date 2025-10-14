# Authentication

<p align="center">
    <img  src="../_screenshots/x_Authentication.png?raw=true">
</p>

This showcases the use of the [Authentication](https://docs.braincloudservers.com/api/capi/authentication/) service available on [brainCloud](https://getbraincloud.com/) as well as several other services that brainCloud supports.

---

## brainCloud Integration

This app makes heavy use of [BCManager.cs](./Assets/App/Scripts/Core/BCManager.cs), [UserHandler.cs](./Assets/App/Scripts/Core/UserHandler.cs), and [ErrorResponse.cs](./Assets/App/Scripts/Core/ErrorResponse.cs) for brainCloud integration.

These scripts, under `Assets > App > Scripts > Core`, can be copied into your own Unity projects to get a quick start on brainCloud integration:

- **BCManager** should be attached to an empty GameObject in one of your initial scenes to be able to make use of BCManager as a singleton
    - It is able to access the many services and functionalities within `BrainCloudWrapper`
    - You can use `BCManager.Wrapper` to access any functionality not exposed by BCManager
    - It also includes uniform and consistent static methods to create `SuccessCallbacks` and `FailureCallbacks` for responses from brainCloud, using `BCManager.HandleSuccess()` and `BCManager.HandleFailure()` respectively

- **UserHandler** is a static class that handles multiple methods of authentication and stores some of the user's data
    - This class can be expanded as needed for your own app to store more user data, such as entity data
    - It contains various scripting symbols to enable functionality for [external authentication methods](./README.md#external-authentication-methods) for when their plugins are integrated
        - These can be edited or removed as needed for your own app

- **JSONHelper** contains several helper methods to help with serializing and deserializing generic JSON objects
    - `IJSON` is an interface that can be used for classes and structs that will make use of these helper methods for easier management with more strongly-typed JSON objects
    - Includes extension methods for dealing with `Dictionary<string, object>` objects in-particular
    - Add `using BrainCloud.JSONHelper` in your scripts to make use of `IJSON` and these helper methods
        - Take a look at the various service scripts and data objects (such as `UserData`, `HockeyStatsData`, and `RPGData`) to see examples of how JSONHelper gets used

- **ErrorResponse** is a struct that gives you an easy way to deserialize data in error responses received from brainCloud
    - `BCManager.HandleFailure()` will return an ErrorResponse

Be sure to check out the various **Data**, **ServiceUI**, and **ContentUI** scripts to see how the three scripts above are used!

## Getting Started

1. In the [brainCloud server portal](https://portal.braincloudservers.com/), you will need to set up your own app for the API calls in this project to return responses
    - For a quick start, within the Unity brainCloud settings window, you can create a new app and enable **Create using Tutorial template**; choose **Authentication** from the list
    - Ensure that your app has enabled support for the platforms you intend on testing on, such as Facebook, Google Android, Windows, etc.
2. In the Unity Authentication project, open up `Main.unity`
3. Log into your brainCloud account via `brainCloud > Settings` in the dropdown menu and select your Team & App from the dropdowns in the brainCloud window
    - This will create the `BrainCloudSettings.asset` and the `BrainCloudEditorSettings.asset` files required for the brainCloud client to function properly
    - Toggle `Enable Logging` to see brainCloud responses in the Unity Console logs
4. The **Main** scene has the GameObject **brainCloudManager** with **BCManager** attached as a component; running this scene will allow you to use & test the app

### Data Objects

- [Entity.cs](./Assets/App/Scripts/Data/BrainCloud/Entity.cs) integrates user entities
- [CustomEntity.cs](./Assets/App/Scripts/Data/BrainCloud/CustomEntity.cs) integrates custom entities
    - [UserData.cs](./Assets/App/Scripts/Data/BrainCloud/UserData.cs), [HockeyStatsData.cs](./Assets/App/Scripts/Data/BrainCloud/HockeyStatsData.cs), and [RPGData.cs](./Assets/App/Scripts/Data/BrainCloud/RPGData.cs) are all examples of how data can be attached to `Entity.Data` and `CustomEntity.Data`
    
### Services

Services that this example project makes use of:

- [Custom Entity](https://docs.braincloudservers.com/api/capi/customentity/)
- [Entity](https://docs.braincloudservers.com/api/capi/entity/)
- [Global Statistics](https://docs.braincloudservers.com/api/capi/globalstats/)
- [Identity](https://docs.braincloudservers.com/api/capi/identity/)
- [Player Statistics](https://docs.braincloudservers.com/api/capi/playerstats/)
- [Script](https://docs.braincloudservers.com/api/capi/script/)
- [Virtual Currency](https://docs.braincloudservers.com/api/capi/virtualcurrency/)

### External Dependency Manager

This Unity example project by default includes Google's **External Dependency Manager** plugin to resolve dependencies on Android and iOS. When importing any plugin into the project that also includes this plugin, do not import `ExternalDependencyManager` or any files within that folder as it will conflict with the already included plugin.

### External Authentication Methods

This example app also has various external authentication methods integrated to showcase how it can be done with your own app. This functionality requires third-party plugins to make use of, which are **not** included with this example app. Once they are integrated in your local copy, you can enable external authentication using certain **Scripting Define Symbols**.

Currently integrated external authentication methods:
| Instructions                                        | Scripting Define Symbol |
| --------------------------------------------------- | :---------------------: |
| [AuthenticateApple](./_docs/Apple.md)               | `APPLE_SDK`             |
| [AuthenticateGameCenter](./_docs/GameCenter.md)     | `GAMECENTER_SDK`        |
| [AuthenticateFacebook](./_docs/Facebook.md)         | `FACEBOOK_SDK`          |
| [AuthenticateGoogle](./_docs/Google.md)             | `GOOGLE_SDK`            |
| [AuthenticateGoogleOpenId](./_docs/GoogleOpenId.md) | `GOOGLE_OPENID_SDK`     |
| [AuthenticateSteam](./_docs/Steam.md)               | `STEAMWORKS_NET`        |

---

For more information on brainCloud and its services, please check out [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/) and [API Reference](https://docs.braincloudservers.com/api/introduction).
