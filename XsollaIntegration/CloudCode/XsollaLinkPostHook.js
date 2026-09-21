// ============================================================================
//  XsollaLinkPostHook
//
//  POST-hook on  Authenticate | Authenticate
//
//  NOTE: Written by Claude.
//
//  Writes the brainCloud profileId onto the Xsolla user as a server-side
//  attribute, so the account link is visible from Xsolla's side too.
//
//  This has to be a POST-hook, not a pre-hook: the profileId does not exist
//  until authentication has actually run, so a pre-hook has nothing to write.
//  Returning a non-200 here still fails the call from the client's point of
//  view (no session token reaches it), but the brainCloud profile will already
//  have been created server-side.
//
//  Hook parameters (Design | Cloud Code | API Hooks -> "parms"):
//  {
//    "serviceCode":        "xsollaLogin",
//    "serverClientId":     "<Server OAuth 2.0 client id>",
//    "serverClientSecret": "<Server OAuth 2.0 client secret>",
//    "publisherId":        <PublisherID>,
//    "publisherProjectId": <ProjectID>,
//    "attributeKey":       "brainCloudProfileId",
//    "failAuthOnError":    true
//  }
//
//  Web service (Design | Cloud Code | Web Services):
//    code: xsollaLogin    base URL: https://login.xsolla.com/api
// ============================================================================

function main()
{
    var parms    = data.parms || {};
    var response = data.message || {};         // the Authenticate result
    var request  = data.callingMessage || {};  // the original Authenticate args
    var extra    = request.extraJson || {};

    var attributeKey    = parms.attributeKey || "brainCloudProfileId";
    var failAuthOnError = parms.failAuthOnError !== false;

    var xsollaUserId = extra.xsollaUserId;
    var profileId    = response.profileId;

    // Not an Xsolla-bridged login — leave it completely alone.
    if (!xsollaUserId)
    {
        return null;
    }

    if (!profileId)
    {
        return finish(50001, "Authenticate returned no profileId to link.", failAuthOnError);
    }

    var token = getServerToken(parms);
    if (token.error)
    {
        return finish(50002, token.error, failAuthOnError);
    }

    var write = writeAttribute(parms, token.value, xsollaUserId, attributeKey, profileId);
    if (write.error)
    {
        return finish(50003, write.error, failAuthOnError);
    }

    // Success: return null so the Authenticate response passes through untouched.
    return null;
}

// ----------------------------------------------------------------------------
//  Exchange the Server OAuth 2.0 client credentials for a server JWT.
//  POST /oauth2/token  (form encoded)
// ----------------------------------------------------------------------------
function getServerToken(parms)
{
    if (!parms.serverClientId || !parms.serverClientSecret)
    {
        return { error: "Hook is missing serverClientId / serverClientSecret." };
    }

    var http = bridge.getHttpClientServiceProxy();

    var form = {
        "grant_type":    "client_credentials",
        "client_id":     String(parms.serverClientId),
        "client_secret": String(parms.serverClientSecret)
    };

    var result = http.postFormResponseJson(parms.serviceCode, "/oauth2/token", {}, {}, form);

    if (result.status !== 200 || !result.data)
    {
        return { error: "Xsolla token request did not complete: " + JSON.stringify(result) };
    }

    if (result.data.statusCode !== 200)
    {
        return { error: "Xsolla rejected the server credentials (HTTP " + result.data.statusCode + "): " + JSON.stringify(result.data.json) };
    }

    var body = result.data.json || {};
    if (!body.access_token)
    {
        return { error: "Xsolla returned no access_token: " + JSON.stringify(body) };
    }

    return { value: body.access_token };
}

// ----------------------------------------------------------------------------
//  Write the attribute.
//  POST /attributes/users/{user_id}/update   -> 204 No Content on success
// ----------------------------------------------------------------------------
function writeAttribute(parms, serverToken, xsollaUserId, key, value)
{
    if (!parms.publisherId)
    {
        return { error: "Hook is missing publisherId (Xsolla requires it on this call)." };
    }

    var http = bridge.getHttpClientServiceProxy();

    var body = {
        "attributes": [{
            "key":        key,                  // [A-Za-z0-9_]+, max 256 chars
            "value":      String(value),        // max 256 chars
            "attr_type":  "server",             // read-only to the client
            "data_type":  "string",
            "permission": "private"
        }],
        "publisher_id": parms.publisherId
    };

    // Omit to write an attribute that is global across all the merchant's games.
    if (parms.publisherProjectId)
    {
        body.publisher_project_id = parms.publisherProjectId;
    }

    var headers = { "X-SERVER-AUTHORIZATION": serverToken };
    var path    = "/attributes/users/" + xsollaUserId + "/update";

    var result = http.postJsonResponseJson(parms.serviceCode, path, {}, headers, body);

    if (result.status !== 200 || !result.data)
    {
        return { error: "Xsolla attribute update did not complete: " + JSON.stringify(result) };
    }

    // 204 is the documented success. 200 is accepted too in case that ever changes.
    if (result.data.statusCode !== 204 && result.data.statusCode !== 200)
    {
        return { error: "Xsolla attribute update failed (HTTP " + result.data.statusCode + "): " + JSON.stringify(result.data.json) };
    }

    return { value: true };
}

// ----------------------------------------------------------------------------
//  Either abort the call or swallow the error and let the login through.
// ----------------------------------------------------------------------------
function finish(reasonCode, message, failAuthOnError)
{
    if (!failAuthOnError)
    {
        // Link failed but the user still gets in. The failure is visible in the
        // API log; flip failAuthOnError to true to make it fatal instead.
        return null;
    }

    return {
        status:       500,
        reasonCode:   reasonCode,
        errorMessage: message
    };
}

main();
