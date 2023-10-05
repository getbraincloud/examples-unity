# Push Notifications & Marketplace

This showcases the use of the [Push Notification](https://docs.braincloudservers.com/api/capi/pushnotification/) and [App Store](https://docs.braincloudservers.com/api/capi/appstore/) services available on [brainCloud](https://getbraincloud.com/).

---

## Push Notification

Push Notifications make use of the [Firebase Messaging](https://firebase.google.com/docs/unity/setup) plugin. You will have to set up a project on the [Firebase Console](https://console.firebase.google.com/) and enable Messaging to make use of this plugin. Follow the instructions on the setup page and then checkout the main [ExampleApp](./Assets/App/Scripts/ExampleApp.cs) script to see how push notifications are sent in the `SendPushNotification(Action)` function.

---

## Marketplace

This example makes use of Unity's In-App Purchasing service to initiate purchases and then verify through brainCloud's App Store service to keep purchases synced to the user's account. [BrainCloudMarketplace](./Assets/App/Scripts/Store/BrainCloudMarketplace.cs) will handle the process for you making use of JSON objects defined in [BCProduct](./Assets/App/Scripts/Store/BCProduct.cs). These scripts can be dropped into your own project to help get started on integrating in-app purchases for your app!

Currently this example only makes use of the **Google Play Store** for Android devices.

### Google Play Store

You will have to setup a project on both the [Google Cloud Console](https://console.cloud.google.com/) and the [Google Play Console](https://play.google.com/console/developers) in order to make use of in-app purchasing in your project.

#### Google Cloud Console

- Enable **Google Cloud APIs**, **Google Play Android Developer API**, **Google Play Game Services**, **Service Usage API** and **Token Service API** for your app
- Create an **OAuth 2.0 Client ID** account for your app
    - This will give you the Client ID and Client Secret needed for your app on both brainCloud and the Google Play Console
- Create a **Service Account** under **Credentials**
    - This is crucial as its required for brainCloud to have access to your app on the Google Play Store
    - You will also need to also create a **Google Service Account p12 Certificate** under **Keys**; download the .p12 certificate and keep it somewhere safe

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

### brainCloud Marketplace

1. In the [brainCloud server portal](https://portal.braincloudservers.com/) for your app, navigate to `Design > Core App Info > Application IDs`
2. Click **Configure Google**
3. You will need to fill out all of the fields using the information in your Google Cloud project for your app in order to allow brainCloud to have access to your app:
    - Google Service Account Email
        - This is the same service account created from above
    - Google Package Name
    - Google App ID
    - Google Client ID
    - Google Client Secret
        - These are from the OAuth 2.0 client ID account
4. You will also need to upload the .p12 certificate created for the service account here
5. Navigate to `Design > Marketplace > Products` to set up your in-app purchases
    - Each product here is meant to reflect a product that your users can purchase; setting these up will automatically facilitate currencies and items that your in-app purchases can redeem on brainCloud once the purchases are verified
    - You will need to ensure that when adding the Google product info for your products that the **Google Product ID** matches the product ID in your app's Google Play Console
    - The prices here are for your own references and is not the same as what is set up for your Google Play Store app
    - It is recommended that you use the localized titles, descriptions, and prices that you can obtain through the product's metadata in Unity to display the proper information to your users
    - See [StorePanel](./Assets/App/Scripts/UI/StorePanel.cs), [BrainCloudMarketplace](./Assets/App/Scripts/Store/BrainCloudMarketplace.cs), and [BCProduct](./Assets/App/Scripts/Store/BCProduct.cs) for how this example makes use of brainCloud's Marketplace features

### Unity In-App Purchasing

Follow Unity's [setup](https://docs.unity3d.com/Manual/UnityIAPSettingUp.html) guide to enable the service for the project.

---

Check out [brainCloud Monetization](https://docs.braincloudservers.com/learn/key-concepts/monetization/) for more information on how to make use of brainCloud monetization features. For more information on other brainCloud services, check out [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/) and [API Reference](https://docs.braincloudservers.com/api/introduction).
