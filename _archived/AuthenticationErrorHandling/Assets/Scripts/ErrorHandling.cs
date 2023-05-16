/**
 * Error handling
 */

using BrainCloud;
using UnityEngine;

public abstract class ErrorHandling
{
    /// <summary>
    /// Error handling that can occur on any call
    /// 
    /// Return true if the reasonCode is handled
    /// 
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="reasonCode"></param>
    /// <param name="statusMessage"></param>
    /// <param name="cbObject"></param>
    /// <param name="parentDialog"></param>
    /// <returns></returns>
    public static bool SharedErrorHandling(int statusCode, int reasonCode, string statusMessage, object cbObject,
        GameObject parentDialog)
    {
        string response = reasonCode + ":" + statusMessage;

        switch (reasonCode)
        {
            case ReasonCodes.NO_SESSION:
            {
                // User session has expired, or they have no session
                // They will need to authenticate

                if (parentDialog != null)
                {
                    Object.Destroy(parentDialog);
                }

                ErrorDialog.DisplayErrorDialog("Your session has expired, or your not logged in. Please log in again",
                    response);

                return true;
            }
            case ReasonCodes.USER_SESSION_LOGGED_OUT:
                {
                    // User session has expired by the same login being used on another device 
                    // They will need to authenticate

                    // The amount of session allowed can be adjusted on the 'Core App Info - Advanced Settings' page

                    if (parentDialog != null)
                    {
                        Object.Destroy(parentDialog);
                    }

                    ErrorDialog.DisplayErrorDialog("You have logged in with another device. Please switch to that device, or log in again",
                        response);

                    return true;
                }
            case ReasonCodes.PLATFORM_NOT_SUPPORTED:
            {
                // User is using an unsupported platform 
                // If the platform is meant to be supported, it needs to be enabled via 'Core App Info - Platforms' on the brainCloud dashboard

                if (parentDialog != null)
                {
                    Object.Destroy(parentDialog);
                }

                ErrorDialog.DisplayErrorDialog("The current platform is not supported.", response);

                return true;
            }
            case ReasonCodes.GAME_VERSION_NOT_SUPPORTED:
            {
                // User game version is out of date, 
                // Display a dialog to update their app to the latest version you have supplied

                // This version number is set in the 'Core App Info - Platforms' on the brainCloud dashboard
                // And is compared locally against GameVersion set in the BrainCloudSettings config

                if (parentDialog != null)
                {
                    Object.Destroy(parentDialog);
                }

                ErrorDialog.DisplayErrorDialog(
                    "Your app version is out of date. Please update to the latest Error Handling demo app",
                    response);

                return true;
            }
            case ReasonCodes.CLIENT_NETWORK_ERROR_TIMEOUT:
            {
                // User cannot connect to brainCloud. 
                // Display a connection error, and ask them if they wish to try again now or later

                if (parentDialog != null)
                {
                    Object.Destroy(parentDialog);
                }

                ErrorDialog.DisplayErrorDialog("Can't connect to server. Try again later.", response);

                return true;
            }

            default:
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="reasonCode"></param>
    /// <param name="statusMessage"></param>
    /// <param name="cbObject"></param>
    /// <param name="parentDialog"></param>
    public static void UncaughtError(int statusCode, int reasonCode, string statusMessage, object cbObject,
        GameObject parentDialog)
    {
        string response = reasonCode + ":" + statusMessage;

        if (parentDialog)
        {
            Object.Destroy(parentDialog);
        }

        // log the reasonCode to your own internal error checking
        ErrorDialog.DisplayErrorDialog("Untracked error", response);
    }
}