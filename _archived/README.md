# Archived Examples

Can't find an example you once used and need to reference it? In an effort to reorganize and be able to properly maintain these Unity Example projects, several examples that were no longer maintained have been removed from the repository. However, to be able to preserve and reference them, we have tagged the commit which last included these projects.

These examples can be found within the `_archived` folder for the tag alongside the project's original readme notes. Since they are no longer maintained, they make use of older versions of Unity and brainCloud. See below to find the example you might be looking for.

---

# Newly Archived

## BCAmazonIAP

This example demonstrates **Amazon In-App Purchases** and verification with brainCloud. Inside there is a [PDF](./BCAmazonIAP/amazonIAPTutorial.pdf) describing the steps you will need to take to get going.

## GoogleIAP

An example of making a purchase with Google, and verifying it with braincloud. Refer to the [PDF](./GoogleIAP/GooglePurchasesTutorial.pdf) for proper setup.

## PushNotifications

An example of connecting **Firebase FCM Notifications** to brainCloud.

Read more information here: http://getbraincloud.com/apidocs/portal-usage/push-notification-setup-firebase/

### Additional Information

This code example is based on the [Firebase Unity Quickstart Project](https://github.com/firebase/quickstart-unity).

You can see more FCM documentation here: https://firebase.google.com/docs/cloud-messaging/android/client

Project currently only contains a push notifications example for Android FCM
- Add your google-services.json file to the asset folder
    - Found on the Firebase dashboard
- Update the brainCloud AppId and Secret on BrainCloudSettings to match yours
    - Id and Secret found on the brainCloud Dashboard
- Download and import the Auth and Messaging Firebase Unity SDK
- Set-up Instructions: https://firebase.google.com/docs/unity/setup

---

## [Archive Date: June 8th, 2023](https://github.com/getbraincloud/examples-unity/tree/archive-06-08-2023)

##### [Pull Request 147 - Archive Old Examples](https://github.com/getbraincloud/examples-unity/pull/147)

##### [README.md](https://github.com/getbraincloud/examples-unity/tree/archive-06-08-2023/_archived#readme)

The following examples were removed on June 8th, 2023:

- AppleAuthentication
- Authentication (pre-BC v4.14.0)
- AuthenticationErrorHandling
- Old_Authentication
- OldGoogleAuth
- OpenIdGoogle
