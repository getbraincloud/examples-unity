# Facebook SDK Integration
| Supported Platforms   |
| :-------------------: |
| Android \| iOS \| Web |

- [Download & Install Plugin](https://developers.facebook.com/docs/unity/)
- [Follow instructions for proper integration](https://developers.facebook.com/docs/unity/gettingstarted)

### External Dependency Manager
This Unity example project by default includes Google's **External Dependency Manager** plugin to resolve dependencies on Android and iOS. When importing Facebook's SDK into the project, do not import `ExternalDependencyManager` or any files within that folder as it will conflict with the already included plugin.

## Set-Up
Once the Facebook SDK is integrated into this example project, add `FACEBOOK_SDK` to your **Scripting Define Symbols** under `Project Settings > Player > Other Settings`.

You will need to provide your own **Facebook App Id** and **Client Token** for your `FacebookSettings.asset` file.

In `UserHandler.AuthenticateFacebook()` and `UserHandler.AuthenticateFacebookLimited()`, you can set up the Facebook permissions as it relates to your app.

### Additional Instructions
- [Android](https://developers.facebook.com/docs/unity/getting-started/android)
- [iOS](https://developers.facebook.com/docs/unity/getting-started/ios)
- [Web](https://developers.facebook.com/docs/unity/getting-started/canvas)

### brainCloud Configuration
1. In the [brainCloud server portal](https://portal.braincloudservers.com/) for your app, navigate to `Design > Core App Info > Application IDs`
2. Click **Configure Facebook**
3. Fill out the following required fields using the information in the Facebook developer portal for your app:
    - Facebook App ID
    - Facebook App Secret

---

#### Read More
- [brainCloud Portal Tutorial – Authentication - Apple](https://getbraincloud.com/apidocs/portal-usage/basic-configuration-facebook/)
- [Unity Documentation – Set up a Facebook account sign-in](https://docs.unity.com/authentication/en/manual/set-up-facebook-signin)
