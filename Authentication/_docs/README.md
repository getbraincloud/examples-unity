### External Authentication Methods
This example app also has various external authentication methods integrated to showcase how it can be done with your own app. This functionality requires third-party plugins to make use of, which are **not** included with this example app. Once they are integrated in your local copy, you can enable external authentication using certain **Scripting Define Symbols**.

Currently integrated external authentication methods:
| Instructions                                  | Scripting Define Symbol |
| --------------------------------------------- | :---------------------: |
| [AuthenticateApple](./Apple.md)               | `APPLE_SDK`             |
| [AuthenticateFacebook](./Facebook.md)         | `FACEBOOK_SDK`          |
| [AuthenticateGoogle](./Google.md)             | `GOOGLE_SDK`            |
| [AuthenticateGoogleOpenId](./GoogleOpenId.md) | `GOOGLE_OPENID_SDK`     |

More external authentication methods to come!
