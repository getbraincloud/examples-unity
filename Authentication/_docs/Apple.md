# Sign in with Apple Plugin Integration

| Supported Platforms |
| :-----------------: |
| iOS                 |
| macOS               |

- [Download & Install Plugin](https://github.com/lupidan/apple-signin-unity)
- [Follow instructions for proper integration](https://github.com/lupidan/apple-signin-unity#installation)

## Set-Up

Once the Sign in with Apple plugin is integrated into this example project, add `APPLE_SDK` to your **Scripting Define Symbols** under `Project Settings > Player > Other Settings`.

In `UserHandler.AuthenticateApple()` you can add `LoginOptions.IncludeEmail` and/or `LoginOptions.IncludeFullName` to your `AppleAuthLoginArgs()`. You will only receive the user's email and name the first time the user logs in. Future logins will only return null unless the user revokes access to this app. If you require those to be stored in your app, ensure that you store them when the user first logs in.

This example app makes use of `AppleAuthManager` from within `UserHandler` itself. If you include this script in your own app, it is highly recommended to create a custom manager to handle `AppleAuthManager` and all of its features.

### brainCloud Configuration

1. In the [brainCloud server portal](https://portal.braincloudservers.com/) for your app, navigate to `Design > Core App Info > Application IDs`
2. Click **Configure Apple**
3. Fill out the **Signin Client Id** field using the Identifier attached to your app from the Apple Developer portal.

### Building for macOS

macOS builds require code signing for the app to be able to make use of this plugin. You will need to create an [.entitlements](./Apple.md#entitlements-template) file to sign the app with as well as a provioning profile. You will find the information needed for signing in the Apple Developer portal for your app. Additional in-depth instructions are on the [Sign in with Apple GitHub](https://github.com/lupidan/apple-signin-unity/blob/master/docs/macOS_NOTES.md).

#### .entitlements Template

```
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>com.apple.security.cs.allow-jit</key>
	<true/>
	<key>com.apple.security.cs.disable-executable-page-protection</key>
	<true/>
	<key>com.apple.developer.applesignin</key>
	<array>
		<string>Default</string>
	</array>
	<key>com.apple.developer.team-identifier</key>
	<string>YOUR_TEAM_IDENTIFIER_HERE</string>
	<key>com.apple.application-identifier</key>
	<string>TEAM_ID.YOUR_APP_IDENTIFIER_HERE</string>
</dict>
</plist>
```

---

#### Read More

- [Apple Developer – Sign in with Apple](https://developer.apple.com/sign-in-with-apple/get-started/)
- [brainCloud Portal Tutorial – Authentication - Apple](https://docs.braincloudservers.com/learn/portal-tutorials/authentication-apple/)
- [Unity Documentation – Set up an Apple sign-in](https://docs.unity.com/authentication/en/manual/set-up-apple-signin)
