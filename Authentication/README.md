# Authentication

An example project that showcases authentication on [brainCloud](https://getbraincloud.com/) and as well as several services that brainCloud supports.

---

### Getting Started
- Open up **Main.unity**
- Log into your brainCloud account via `brainCloud > Settings` and select your Team & App from the dropdowns to get started
    - For the ready-made experience, be sure to duplicate the Authentication app from the brainCloud examples to your Team

## brainCloud Integration
- This app makes heavy use of [BCManager.cs](https://github.com/getbraincloud/examples-unity/blob/master/Authentication/Assets/App/Scripts/Core/BCManager.cs), [UserHandler.cs](https://github.com/getbraincloud/examples-unity/blob/master/Authentication/Assets/App/Scripts/Core/UserHandler.cs), and [ErrorResponse.cs](https://github.com/getbraincloud/examples-unity/blob/master/Authentication/Assets/App/Scripts/Core/ErrorResponse.cs) for brainCloud service integration
    - The **Main** scene has the GameObject **brainCloudManager** with **BCManager** attached as a Component
    - These files, under `App > Scripts > Core` can be copied to your own Unity projects to get quickly started on brainCloud integration

### Data Objects
- [Entity.cs](https://github.com/getbraincloud/examples-unity/blob/master/Authentication/Assets/App/Scripts/Data/Entity.cs) integrates user entities
- [CustomEntity.cs](https://github.com/getbraincloud/examples-unity/blob/master/Authentication/Assets/App/Scripts/Data/CustomEntity.cs) integrates custom entities
    - [UserData.cs](https://github.com/getbraincloud/examples-unity/blob/master/Authentication/Assets/App/Scripts/Data/UserData.cs), [HockeyStatsData.cs](https://github.com/getbraincloud/examples-unity/blob/master/Authentication/Assets/App/Scripts/Data/HockeyStatsData.cs), and [RPGData.cs](https://github.com/getbraincloud/examples-unity/blob/authentication/unity-bc-update/Authentication/Assets/App/Scripts/Data/RPGData.cs) are all examples of how data can be attached to `Entity.Data` and `CustomEntity.Data`
    
### Services
Services that this example showcases:
- [Custom Entities](https://getbraincloud.com/apidocs/apiref/?csharp#capi-customentity)
- [Entities](https://getbraincloud.com/apidocs/apiref/?csharp#capi-entity)
- [Global Statistics](https://getbraincloud.com/apidocs/apiref/?csharp#capi-globalstats)
- [Identity](https://getbraincloud.com/apidocs/apiref/?csharp#capi-identity)
- [Player Statistics](https://getbraincloud.com/apidocs/apiref/?csharp#capi-playerstats)
- [Script](https://getbraincloud.com/apidocs/apiref/?csharp#capi-script)
- [Virtual Currency](https://getbraincloud.com/apidocs/apiref/?csharp#capi-virtualcurrency)

More service examples to come!

---

For more information on brainCloud and its services, please checkout the brainCloud [API Reference](https://getbraincloud.com/apidocs/apiref/?csharp#introduction).
