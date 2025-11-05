# Apple Unity Plugins Integration

| Supported Platforms |
| :-----------------: |
| iOS                 |
| macOS               |

- [Download & Install Plugins](https://github.com/apple/unityplugins)
- [Follow instructions for proper integration](https://github.com/apple/unityplugins/blob/main/Documentation/Quickstart.md)

## Set-Up

We recommend installing the Apple plugins as [compressed tarball packages](https://github.com/apple/unityplugins/blob/main/Documentation/Quickstart.md#installing-compressed-tarball-packages) within Unity's package manager. You will need to install both the **Apple.Core** package and **Apple.GameKit** package to enable Game Center authentication.

Once both plugins are installed, add `GAMECENTER_SDK` to your **Scripting Define Symbols** under `Project Settings > Player > Other Settings`.

These packages will add **Apple Build Settings** to your Player Settings. Give it a review to make sure everything applies to your project as expected.

You'll need to ensure that your app on App Store Connect also has Game Center entitlements. If everything looks to be set up properly but it hangs upon authentication, you might need to add Achievements and/or a default Leaderboard as Game Center doesn't seem to initialize fully if your app on App Store Connect doesn't have these configured.

### brainCloud Configuration

1. In the [brainCloud server portal](https://portal.braincloudservers.com/) for your app, navigate to `Design > Core App Info > Application IDs`
2. Click **Configure Apple**
3. Fill out the **Bundle Id** and **Signin Client Id** fields using the Identifier attached to your app from the Apple Developer portal.

---

#### Read More

- [Apple - GameKit](https://github.com/apple/unityplugins/blob/main/plug-ins/Apple.GameKit/Apple.GameKit_Unity/Assets/Apple.GameKit/Documentation~/Apple.GameKit.md)
- [GameKit Developer Documentation](https://developer.apple.com/documentation/gamekit/)
