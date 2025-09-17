# Apple Unity Plugins Integration

| Supported Platforms |
| :-----------------: |
| iOS                 |
| macOS               |

- [Download & Install Plugins](https://github.com/apple/unityplugins)
- [Follow instructions for proper integration](https://github.com/apple/unityplugins/blob/main/Documentation/Quickstart.md)

## Set-Up

We recommend installing the Apple plugins as [compressed tarball packages](https://github.com/apple/unityplugins/blob/main/Documentation/Quickstart.md#installing-compressed-tarball-packages) within Unity's package manager. You will need to install both the Apple.Core package and Apple.GameKit package to enable Game Center authentication.

Once both plugins are installed, add `GAMECENTER_SDK` to your **Scripting Define Symbols** under `Project Settings > Player > Other Settings`.

These packages will add **Apple Build Settings** to your Player Settings. Give it a review to make sure everything applies to your project as expected.

---

#### Read More

- [GameKit Developer Documentation](https://github.com/apple/unityplugins/blob/main/plug-ins/Apple.GameKit/Apple.GameKit_Unity/Assets/Apple.GameKit/Documentation~/Apple.GameKit.md)
- [GameKit SDK](https://developer.apple.com/documentation/gamekit/)
