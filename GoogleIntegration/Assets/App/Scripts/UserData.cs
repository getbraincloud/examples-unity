using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrainCloud.JsonFx.Json;

public static class UserData
{
    public static int EnergyAmount { get; private set; } = 0;

    public static int CurrencyAmount { get; private set; } = 0;

    public static bool HasSpecialItem { get; private set; } = false;

    public static void GetDataFromResponse(string response)
    {
        // TODO: Deserialize JSON
    }
}
