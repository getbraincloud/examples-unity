// ============================================================================
//  AuthenticateXsollaPostHook
//
//  POST-hook on  Authenticate | Authenticate
//
//  NOTE: Written by Claude.
//
//  Writes the brainCloud profileId onto the Xsolla user as a server-side
//  attribute, so the account link is visible from Xsolla's side too.
//
//  Pairs with AuthenticateXsolla: it only acts on logins made through
//  AuthenticateExternal with the "Xsolla" external auth name. By the time this
//  runs, that script has already proved the externalId is the Xsolla user the
//  access token belongs to, so the externalId is safe to use as-is.
//
//  Link state lives on the brainCloud profile in a singleton user entity of
//  type "xsollaAttributes", in the boolean field "xsollaProfileLinked". The
//  link is written whenever that singleton is missing or the flag is not true,
//  and the flag is only set after Xsolla accepts the write. So a failed link is
//  retried on the next login instead of being lost, and once linked, returning
//  users skip both Xsolla calls entirely.
//
//  The singleton is meant to be extended: any other fields developers add to
//  "xsollaAttributes" are preserved when the flag is written.
//
//  This has to be a POST-hook, not a pre-hook: the profileId does not exist
//  until authentication has actually run, so a pre-hook has nothing to write.
//  Returning a non-200 here still fails the call from the client's point of
//  view (no session token reaches it), but the brainCloud profile will already
//  have been created server-side.
//
//  Hook parameters (Design | Cloud Code | API Hooks -> "parms"):
//  {
//    "serverClientId":     "<Server OAuth 2.0 client id>",
//    "serverClientSecret": "<Server OAuth 2.0 client secret>",
//    "publisherId":        <PublisherID>,
//    "publisherProjectId": <ProjectID>
//  }
//
//  Web service (Design | Cloud Code | Web Services):
//    code: xsollaLogin    base URL: https://login.xsolla.com/api
// ============================================================================

var SERVICE_CODE = "xsollaLogin";
var EXTERNAL_AUTH_NAME = "Xsolla";
var ATTRIBUTE_KEY = "BCProfileId";

var ENTITY_TYPE = "xsollaAttributes";
var LINKED_FLAG = "xsollaProfileLinked";

function main()
{
    var parms    = data.parms || {};
    var response = data.message || {};         // the Authenticate result
    var request  = data.callingMessage || {};  // the original Authenticate args

    // Only Xsolla logins made through AuthenticateExternal. For those, the
    // externalId *is* the Xsolla user ID.
    var isXsollaLogin = request.authenticationType === "External" &&
                        request.externalAuthName === EXTERNAL_AUTH_NAME;

    var xsollaUserId = isXsollaLogin ? request.externalId : null;
    var profileId    = response.profileId;

    // Not an Xsolla-bridged login — leave it completely alone.
    if (!xsollaUserId)
    {
        return null;
    }

    if (!profileId)
    {
        return finish(50001, "Authenticate returned no profileId to link.");
    }

    // Act explicitly as the profile that just logged in, rather than relying
    // on whichever session the hook happens to run under.
    var entityProxy;
    try
    {
        entityProxy = bridge.getEntityServiceProxy(bridge.getSessionForProfile(profileId));
    }
    catch (e)
    {
        return finish(50004, "Could not open a session for profile " + profileId + ": " + e);
    }

    var state = getLinkState(entityProxy);
    if (state.error)
    {
        return finish(50004, state.error);
    }

    // Already linked — nothing to do, and no dependency on Xsolla for this login.
    if (state.linked)
    {
        return null;
    }

    var token = getServerToken(parms);
    if (token.error)
    {
        return finish(50002, token.error);
    }

    var write = writeAttribute(parms, token.value, xsollaUserId, ATTRIBUTE_KEY, profileId);
    if (write.error)
    {
        return finish(50003, write.error);
    }

    var mark = markLinked(entityProxy, state.fields);
    if (mark.error)
    {
        return finish(50005, mark.error);
    }

    // Success: return null so the Authenticate response passes through untouched.
    return null;
}

// ----------------------------------------------------------------------------
//  Read the "xsollaAttributes" singleton.
//  getSingleton returns 200 with data: null when the singleton does not exist.
// ----------------------------------------------------------------------------
function getLinkState(entityProxy)
{
    var result = entityProxy.getSingleton(ENTITY_TYPE);

    if (result.status !== 200)
    {
        return { error: "Could not read the " + ENTITY_TYPE + " singleton: " + JSON.stringify(result) };
    }

    var fields = (result.data && result.data.data) || {};

    return {
        linked: fields[LINKED_FLAG] === true,
        fields: fields
    };
}

// ----------------------------------------------------------------------------
//  Set the flag on the singleton, creating it if needed. updateSingleton
//  replaces the whole data object, so the existing fields are carried over.
// ----------------------------------------------------------------------------
function markLinked(entityProxy, fields)
{
    // Named entityData so it doesn't shadow the hook's global 'data'.
    var entityData = {};
    for (var key in fields)
    {
        if (fields.hasOwnProperty(key))
        {
            entityData[key] = fields[key];
        }
    }

    entityData[LINKED_FLAG] = true;

    var result = entityProxy.updateSingleton(ENTITY_TYPE, entityData, { "other": 0 }, -1);

    if (result.status !== 200)
    {
        return { error: "Linked on Xsolla, but could not set " + LINKED_FLAG + " on the " + ENTITY_TYPE + " singleton: " + JSON.stringify(result) };
    }

    return { value: true };
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

    var result = http.postFormResponseJson(SERVICE_CODE, "/oauth2/token", {}, {}, form);

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

    var result = http.postJsonResponseJson(SERVICE_CODE, path, {}, headers, body);

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
function finish(reasonCode, message)
{
    return {
        status:       500,
        reasonCode:   reasonCode,
        errorMessage: message
    };
}

main();
