// ============================================================================
//  AuthenticateXsolla
//
//  External Authentication script for  Design | Authentication | External
//
//  NOTE: Written by Claude.
//
//  Register this script under the external auth name "Xsolla". The client
//  then logs in with:
//
//    BC.AuthenticateExternal(xsollaUserId, xsollaAccessToken, "Xsolla", forceCreate, ...)
//
//  brainCloud passes in:
//    data.externalId           - the Xsolla user ID (the access token's 'sub')
//    data.authenticationToken  - the Xsolla access token (JWT)
//
//  The token is handed back to Xsolla, which tells us who it belongs to. The
//  login is only valid if that matches the externalId the client claimed -
//  otherwise a valid token for one user could open another user's profile.
//
//  Web service (Design | Cloud Code | Web Services):
//    code: xsollaLogin    base URL: https://login.xsolla.com/api
// ============================================================================

var SERVICE_CODE = "xsollaLogin";

// Xsolla reports bans on GET /users/me. Set to false to let banned Xsolla
// users into the game anyway.
var REJECT_BANNED_USERS = true;

function main()
{
    var externalId  = data.externalId;
    var accessToken = data.authenticationToken;

    if (!externalId || !accessToken)
    {
        return reject("MISSING_CREDENTIALS", "Both the Xsolla user ID and the Xsolla access token are required.");
    }

    var me = getXsollaUser(accessToken);
    if (me.error)
    {
        return reject(me.code, me.error);
    }

    var user = me.value;

    // The token is genuine, but it has to belong to the user being logged in.
    if (!user.id || user.id !== externalId)
    {
        return reject("USER_MISMATCH", "The Xsolla access token does not belong to the claimed user.");
    }

    if (REJECT_BANNED_USERS && user.ban)
    {
        return reject("USER_BANNED", "This Xsolla account is banned" + (user.ban.date_to ? " until " + user.ban.date_to : "") + ".");
    }

    return {
        valid: true,

        // Returned to the client on a successful login.
        authPayload: {
            xsollaUserId: user.id,
            isAnonymous:  !!user.is_anonymous   // true for Device ID logins with no other auth method
        }
    };
}

// ----------------------------------------------------------------------------
//  GET /users/me  with the user's own JWT -> { id, ban, is_anonymous, ... }
// ----------------------------------------------------------------------------
function getXsollaUser(accessToken)
{
    var http = bridge.getHttpClientServiceProxy();

    var headers = { "Authorization": "Bearer " + accessToken };

    var result = http.getResponseJson(SERVICE_CODE, "/users/me", {}, headers);

    if (result.status !== 200 || !result.data)
    {
        return { code: "HTTP_FAILED", error: "Xsolla identity check did not complete: " + JSON.stringify(result) };
    }

    if (result.data.statusCode === 401 || result.data.statusCode === 403)
    {
        return { code: "TOKEN_REJECTED", error: "Xsolla rejected the access token (HTTP " + result.data.statusCode + ")." };
    }

    if (result.data.statusCode !== 200)
    {
        return { code: "HTTP_" + result.data.statusCode, error: "Xsolla identity check failed (HTTP " + result.data.statusCode + ")." };
    }

    return { value: result.data.json || {} };
}

function reject(code, message)
{
    return {
        valid: false,
        extraErrorJson: {
            reason:  code,
            message: message
        }
    };
}

main();
