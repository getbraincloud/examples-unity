// ============================================================================
//  XsollaVerifyPreHook
//
//  PRE-hook on  Authenticate | Authenticate
//
//  NOTE: Written by Claude.
//
//  This is the piece that makes the Universal ("custom") auth trustworthy.
//  Without it, anyone who knows the userId/password pair can log in as that
//  profile; the Xsolla access token sitting in extraJson is never checked.
//
//  Here we hand the token back to Xsolla and make Xsolla tell us who it
//  belongs to. If Xsolla disagrees with what the client claimed, the
//  authentication is aborted before a profile is created or resumed.
//
//  Hook parameters (Design | Cloud Code | API Hooks -> "parms"):
//  {
//    "serviceCode":  "xsollaLogin",
//    "userIdPrefix": "xsolla_"
//  }
//
//  Web service (Design | Cloud Code | Web Services):
//    code: xsollaLogin    base URL: https://login.xsolla.com/api
// ============================================================================

function main()
{
    var parms   = data.parms || {};
    var message = data.message || {};
    var extra   = message.extraJson || {};

    var prefix = parms.userIdPrefix || "xsolla_";

    // Only gate the Xsolla-bridged logins. Anonymous, email, Steam etc. pass through.
    if (!extra.xsollaUserId)
    {
        return { status: 200 };
    }

    // An Xsolla-bridged login that brought no token is not acceptable — that is
    // exactly the forgery this hook exists to stop.
    if (!extra.xsollaAccessToken)
    {
        return reject(50010, "Xsolla login attempted without an access token.");
    }

    var me = getXsollaUser(parms, extra.xsollaAccessToken);
    if (me.error)
    {
        return reject(50011, me.error);
    }

    // Xsolla's answer must match the identity the client claimed...
    if (!me.value.id || me.value.id !== extra.xsollaUserId)
    {
        return reject(50012, "Xsolla token does not belong to the claimed user.");
    }

    // ...and the brainCloud user ID must be derived from that same Xsolla user,
    // otherwise a valid token for user A could be used to log in as user B.
    if (message.externalId !== prefix + me.value.id)
    {
        return reject(50013, "externalId does not match the Xsolla user this token belongs to.");
    }

    return { status: 200 };
}

// ----------------------------------------------------------------------------
//  GET /users/me  with the user's own JWT -> { id, email, is_anonymous, ... }
// ----------------------------------------------------------------------------
function getXsollaUser(parms, accessToken)
{
    var http = bridge.getHttpClientServiceProxy();

    var headers = { "Authorization": "Bearer " + accessToken };

    var result = http.getResponseJson(parms.serviceCode, "/users/me", {}, headers);

    if (result.status !== 200 || !result.data)
    {
        return { error: "Xsolla identity check did not complete: " + JSON.stringify(result) };
    }

    if (result.data.statusCode !== 200)
    {
        return { error: "Xsolla rejected the access token (HTTP " + result.data.statusCode + ")." };
    }

    return { value: result.data.json || {} };
}

function reject(reasonCode, message)
{
    return {
        status:       403,
        reasonCode:   reasonCode,
        errorMessage: message
    };
}

main();
