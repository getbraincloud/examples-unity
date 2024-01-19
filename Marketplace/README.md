# Push Notifications & Marketplace

This showcases the use of the [Push Notification](https://docs.braincloudservers.com/api/capi/pushnotification/) and [App Store](https://docs.braincloudservers.com/api/capi/appstore/) services available on [brainCloud](https://getbraincloud.com/).

Push Notifications and In-App Purchases are set up differently for both Android and iOS so be sure to pay attention to the instructions for the platform you want to make use of.

---

## Push Notification

The [ExampleApp](./Assets/App/Scripts/ExampleApp.cs) script will register device tokens (either through Firebase or the App Store) and the `SendPushNotification(Action)` function will send Push Notifications via brainCloud's [SendRawPushNotification](https://docs.braincloudservers.com/api/capi/pushnotification/schedulerawpushnotificationutc) API call. This sends JSON data to be used by their respectivce remote push notification services so be sure to look up how the JSON is supposed to be set up for [Firebase](https://github.com/firebase/firebase-admin-dotnet/blob/db55e58ee591dab1f90a399336670ae84bab915b/FirebaseAdmin/FirebaseAdmin.Snippets/FirebaseMessagingSnippets.cs) and [Apple Push Services](https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/generating_a_remote_notification).

Try the different [PushNotificationService](https://docs.braincloudservers.com/api/capi/pushnotification/) calls from here to see how each one works.

### Android Setup

Push Notifications on Android devices make use of the [Firebase Messaging](https://firebase.google.com/docs/unity/setup) plugin. You will have to set up a project on the [Firebase Console](https://console.firebase.google.com/) and enable **Messaging** to make use of this plugin.

You will need to include the `google-services.json` file in your projects `Assets` folder for Firebase to initialize properly.

### iOS Setup

You will need to set up a bundle identifier on [Apple's developer portal](https://developer.apple.com/account/resources/identifiers/list) and enable **Push Notifications** for this bundle identifier (if you set up one up for In-App Purchases you will need to use the same one). Be sure to generate an [Apple Push Services certificate](https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/establishing_a_certificate-based_connection_to_apns) using the bundle identifier you set up and export it as a `.p12` certificate (you can do so either with [OpenSSL](https://www.openssl.org/) or the Keychain Access app on MacOS).

The `.p12` certificate will be need to be uploaded to your app's **Notifications** settings on brainCloud in order to be able to send remote push notifications to registered devices.

If you're having trouble receiving remote push notifications, try changing the **Push Notification Environment** to `Development` in your settings.

Note: When building for iOS, you might receive errors from Firebase in Unity after successful builds. This won't impact your app. The Dependency Resolver will, however, include Firebase in your `Podfile` but if you don't have any other plugins included in your build, you don't need to install any external dependencies for the project to build. If you do, you can simply remove Firebase from your `Podfile` before running CocoaPods.

---

## Marketplace

Both Android and iOS makes use of Unity's In-App Purchasing service to initiate purchases and then it will verify through brainCloud's App Store service to keep purchases synced to the user's account. Android handles purchases through the [Google Play Store](https://play.google.com/console/about/in-appproductssetup/) while iOS uses the [App Store](https://developer.apple.com/in-app-purchase/). Both services have differences from each other so be sure to read up on their developer portals to see how they should be set up.

[BrainCloudMarketplace](./Assets/App/Scripts/Store/BrainCloudMarketplace.cs) will handle the process for you making use of JSON objects defined in [BCProduct](./Assets/App/Scripts/Store/BCProduct.cs). These two scripts can be dropped into your own project to help get started on integrating in-app purchases for your app!

### Unity In-App Purchasing

You will need to enable Unity's **In-App Purchasing** services to make use of in-app purchases in your Unity projects. You will not be charged for this service as it'll only be used as an interface for your respective platforms to get, initiate, and validate purchases.

Follow Unity's [setup](https://docs.unity3d.com/Manual/UnityIAPSettingUp.html) guide to enable the service for the project. You will need to include the Google Play **License Key** for your Unity services project's dashboard for Android in-app purchases.

### Google Play Store

You will have to setup a project on both the [Google Cloud Console](https://console.cloud.google.com/) and the [Google Play Console](https://play.google.com/console/developers) in order to make use of in-app purchasing in your project.

#### Google Cloud Console

- Enable **Google Cloud APIs**, **Google Play Android Developer API**, **Google Play Game Services**, **Service Usage API** and **Token Service API** for your app
- Create an **OAuth 2.0 Client ID** account for your app
    - This will give you the Client ID and Client Secret needed for your app on both brainCloud and the Google Play Console
- Create a **Service Account** under **Credentials**
    - This is crucial as its required for brainCloud to have access to your app on the Google Play Store
    - You will also need to also create a **Google Service Account p12 Certificate** under **Keys**; download the `.p12` certificate and keep it somewhere safe

#### Google Play Console

- Ensure that you have billing enabled for your developer account
- Give the service account you have set up under the Google Cloud Console **API access** to your developer account
- Under **Users and permissions**, add the same service account app as a user and give it permissions to your app and enable the following permissions:
    - View app information (read-only)
    - View financial data
    - Manage orders and subscriptions
    - Manage store presence
    - Manage policy declarations
- Also add the client ID account as a user and give it **View app information (read-only)** permissions
- For your app, go through the **Monetization setup** to enable monetization
    - You can find the Base64-encoded RSA public key here to enable Google Play Store purchasing in the Unity IAP service
- You will need to have **In-app products** and/or **Subscriptions** added to your app

#### brainCloud Marketplace

1. In the [brainCloud server portal](https://portal.braincloudservers.com/) for your app, navigate to `Design > Core App Info > Application IDs`
2. Click **GOOGLE** under **Configure Platforms**
3. You will need to fill out all of the fields using the information in your Google Cloud project for your app in order to allow brainCloud to have access to your app:
    - Google Service Account Email
        - This is the same service account created from above
    - Google Package Name
    - Google App ID
    - Google Client ID
    - Google Client Secret
        - These are from the OAuth 2.0 client ID account
4. You will also need to upload the `.p12` certificate created for the service account here
5. Navigate to `Design > Marketplace > Products` to set up your in-app purchases
    - Each product here is meant to reflect a product that your users can purchase; setting these up will automatically facilitate currencies and items that your in-app purchases can redeem on brainCloud once the purchases are verified
    - You will need to ensure that when adding the Google product info for your products that the **Google Product ID** matches the product ID in your app's Google Play Console
    - The prices here are for your own references and is not the same as what is set up for your Google Play Store app
    - It is recommended that you use the localized titles, descriptions, and prices that you can obtain through the product's metadata in your scripts to display the proper information to your users
    - See the [StorePanel](./Assets/App/Scripts/UI/StorePanel.cs), [BrainCloudMarketplace](./Assets/App/Scripts/Store/BrainCloudMarketplace.cs), and [BCProduct](./Assets/App/Scripts/Store/BCProduct.cs) scripts for how this example makes use of brainCloud's Marketplace features

### App Store

You will need to set up a **Bundle Identifier** on [Apple's developer portal](https://developer.apple.com/account/resources/identifiers/list) (if you set up one up for Push Notifications you will need to use the same one). You will also need to create an app in [App Store Connect](https://appstoreconnect.apple.com/apps); make sure to use the Bundle ID you created for this app.

#### App Store Connect

1. After creating your app, under `App Information > Bundle ID` make sure you have selected the proper Bundle ID
2. You can create in-app purchase products under `Features > In-App Purchases` and subscriptions under `Features > Subscriptions`
    - You will only be able to create one in-app purchase product and one subscription until you upload your app for the first time; you can do so as a test if you need to test more than one

#### brainCloud Marketplace

1. In the [brainCloud server portal](https://portal.braincloudservers.com/) for your app, navigate to `Design > Core App Info > Application IDs`
2. Click **APPLE** under **Configure Platforms**
3. Fill out **Bundle Id** with the bundle identifier you created for your app
4. Navigate to `Design > Marketplace > Products` to set up your in-app purchases
    - When configuring for Apple, you will need to make sure your **Product ID** matches what you set up in the App Store Connect
    - You should use the same Product ID for iPhone, iPad and Apple TV (at one point Apple required all three to be different but this is no longer the case)
    - Read [step 5](#braincloud-marketplace) of the Google Play Store brainCloud Marketplace instructions for more information

---

Check out [brainCloud Monetization](https://docs.braincloudservers.com/learn/key-concepts/monetization/) for more information on how to make use of brainCloud monetization features. For more information on other brainCloud services, check out [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/) and [API Reference](https://docs.braincloudservers.com/api/introduction).
