#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;

#if !DISABLESTEAMWORKS
using Steamworks;
#endif

public class SteamUtils : MonoBehaviour
{
#if !DISABLESTEAMWORKS
    // Start is called before the first frame update
    private static byte[] m_ticket = new byte[1024];
    private static uint m_ticketSize;
    private static Action<string> authTicketCallback;


    public static void SetupSteamManager()
    {
        Callback<GetAuthSessionTicketResponse_t>.Create(OnGetAuthSessionTicketResponse);
        Callback<GetTicketForWebApiResponse_t>.Create(OnGetTicketForWebAPIResponse);
    }

    private static void OnGetAuthSessionTicketResponse(GetAuthSessionTicketResponse_t pCallback)
    {
        CSteamID steamId = SteamUser.GetSteamID();
        if (pCallback.m_hAuthTicket != HAuthTicket.Invalid && m_ticketSize != 0)
        {
            string authToken = BitConverter.ToString(m_ticket, 0, (int)m_ticketSize).Replace("-", string.Empty);
            authTicketCallback(authToken);
        }
    }

    private static void OnGetTicketForWebAPIResponse(GetTicketForWebApiResponse_t pCallback)
    {
        CSteamID steamId = SteamUser.GetSteamID();
        if (pCallback.m_hAuthTicket != HAuthTicket.Invalid && m_ticketSize != 0)
        {
            string authToken = BitConverter.ToString(pCallback.m_rgubTicket, 0, (int)pCallback.m_rgubTicket.Length).Replace("-", string.Empty);
            authTicketCallback(authToken);
        }
    }

    public static bool IsSteamInitialized()
    {
        return SteamManager.Initialized;
    }

    public static string GetSteamUsername()
    {
        return IsSteamInitialized() ? SteamFriends.GetPersonaName() : string.Empty;
    }

    public static CSteamID GetSteamID()
    {
        return IsSteamInitialized() ? SteamUser.GetSteamID() : CSteamID.Nil;
    }

    public static string GetSteamAuthTicket(Action<string> callback)
    {
        HAuthTicket hAuthTicket = HAuthTicket.Invalid;
        if (!IsSteamInitialized()) return string.Empty;


        SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
        identity.SetSteamID(GetSteamID());
        hAuthTicket = SteamUser.GetAuthSessionTicket(m_ticket, m_ticket.Length, out m_ticketSize, ref identity);


        authTicketCallback = callback;

        string authToken = BitConverter.ToString(m_ticket, 0, (int)m_ticketSize).Replace("-", string.Empty);
        return authToken;
    }

    public static void GetSteamAuthTicketWebAPI(Action<string> callback)
    {
        //Callback<GetAuthSessionTicketResponse_t>.Create(callback);
        authTicketCallback = callback;
        HAuthTicket ticket = SteamUser.GetAuthTicketForWebApi("brainCloud");
    }
#endif
}
