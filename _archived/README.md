# Archived Examples

These examples are no longer maintained and are archived here for reference. They may make use of older versions of Unity and brainCloud. The original readme notes are included below.

---

## AppleAuthentication

*This is a code example and reference. This app will NOT work out of the box, because you will need your own ios developers console app and braincloud app integrated and setup. You must follow the tutorial pdf that is provided. Use our code as an example for your own code.*

Example of authenticating with brainCloud using an Apple account. Inside there is a pdf tutorial describing the steps you will need to take in order to properly connect your Apple developer's app and braincloud app. This is for IOS only.

---

## Authentication

*This app is a code example*

This example demonstrates how to call methods in the following modules:

- Authentication
    - Email
    - Universal
    - Anonymous
    - [Google](http://getbraincloud.com/apidocs/portal-usage/authentication-google/)
- Player Entities
- XP/Currency
- Player Statistics
- Global Statistics
- Cloud Code

You can find more information about how to run the example here:
http://getbraincloud.com/apidocs/tutorials/unity-tutorials/unity-authentication-example/

---

## AuthenticationErrorHandling

*This app is a code example*

This example demonstrates various error handling cases around authentication.
Use it to experiment with authentication error states.

---

## OldGoogleAuth

*This is a code example and reference. This app will NOT work out of the box, because you will need your own google console app and braincloud app integrated and setup. You must follow the tutorial pdf that is provided. Use our code as an example for your own code.*

Example of authenticating with brainCloud using a Google account. Inside there is a pdf tutorial describing how to go about setting up a google console app, and using firebase to successfully connect the apps to your brainCloud app!

This method of authenticating with google is NOT recommended, and only works for Android. We highly recommend you looking into our GoogleOpenId example instead, and setting up your google authentication using the googleOpenId.

--

## OpenIdGoogle

*This is a code example and reference. This app will NOT work out of the box, because you will need your own google console app and braincloud app integrated and setup. You must follow the tutorial pdf that is provided. Use our code as an example for your own code.* 

Example of authenticating with brainCloud using the GoogleOpenId. Inside there is a pdf tutorial describing how to go about setting up a google console app, and successfully connect the app to brainCloud. Instead of a google account specifically, you will be signing in with your googleOpenId. This is more flexible and can be used for IOS and Android devices as well. Setup is explained in the tutorial.

Inside this project folder you will also find a project folder for the same project made on an Apple device. Mostly everything is the same in Unity, but the apple project needs to build through Xcode. Refer to the tutorial for further details.
