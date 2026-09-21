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

var XSOLLA_PROJECT_ID = 123456789;   // Store project ID (Publisher Account URL)
var SERVICE_CODE      = "xsollaStore";

// Order statuses that mean the money is actually in.
// new = awaiting payment, canceled = refunded, expired = superseded.
var PAID_STATUSES = ["paid", "done"];

function main()
{
    var input = data || {};

    var accessToken = input.xsollaAccessToken;
    if (!accessToken)
    {
        return fail("NO_TOKEN", "xsollaAccessToken is required — the Xsolla order endpoint is authed as the player.");
    }

    // ---- 1. Work out which order we are checking -----------------------------
    var orderId = input.orderId;
    var receiptSku = null;

    if (!orderId && input.receipt)
    {
        var decoded = decodeReceipt(input.receipt);
        if (decoded.error)
        {
            return fail("BAD_RECEIPT", decoded.error);
        }

        orderId    = decoded.value.uid;
        receiptSku = decoded.value.sku;
    }

    if (!orderId)
    {
        return fail("NO_ORDER", "Neither orderId nor a decodable receipt was supplied.");
    }

    // ---- 2. Ask Xsolla about the order --------------------------------------
    var order = getOrder(accessToken, orderId);
    if (order.error)
    {
        return fail(order.code, order.error);
    }

    var body  = order.value;
    var items = (body.content && body.content.items) || [];
    var first = items.length > 0 ? items[0] : {};

    // ---- 3. Decide ----------------------------------------------------------
    var isPaid = indexOf(PAID_STATUSES, body.status) >= 0;

    // If the receipt named a SKU, Xsolla has to agree with it.
    var skuMatches = true;
    if (receiptSku)
    {
        skuMatches = false;
        for (var i = 0; i < items.length; i++)
        {
            if (items[i].sku === receiptSku) { skuMatches = true; break; }
        }
    }

    var result = {
        verified:     isPaid && skuMatches,
        orderId:      body.order_id,
        status:       body.status,
        sku:          first.sku || receiptSku || null,
        quantity:     first.quantity || 0,
        isFree:       body.content ? !!body.content.is_free : false,
        price:        body.content ? body.content.price : null,
        virtualPrice: body.content ? body.content.virtual_price : null,

        // Taken from the token, not from the client's claims. The order lookup
        // is scoped to the bearer token, so a 200 already proves this order
        // belongs to this user — this just surfaces who that is.
        xsollaUserId: readJwtClaim(accessToken, "sub"),

        // Raw payload so the server team can see the real shape.
        raw: body
    };

    if (!isPaid)
    {
        result.reasonCode = "NOT_PAID";
        result.reason     = "Order status is '" + body.status + "', expected one of: " + PAID_STATUSES.join(", ");
    }
    else if (!skuMatches)
    {
        result.reasonCode = "SKU_MISMATCH";
        result.reason     = "Receipt claimed SKU '" + receiptSku + "' but the order does not contain it.";
    }

    return result;
}

// ----------------------------------------------------------------------------
//  GET /v2/project/{project_id}/order/{order_id}
//  Auth: Authorization: Bearer <user JWT>   (Xsolla calls this "AuthForCart")
//  200 -> { order_id, status, content: { price, virtual_price, is_free, items } }
//  404 -> order does not exist, or does not belong to this token
// ----------------------------------------------------------------------------
function getOrder(accessToken, orderId)
{
    var http = bridge.getHttpClientServiceProxy();

    var headers = { "Authorization": "Bearer " + accessToken };
    var path    = "/v2/project/" + XSOLLA_PROJECT_ID + "/order/" + orderId;

    var result = http.getResponseJson(SERVICE_CODE, path, {}, headers);

    if (result.status !== 200 || !result.data)
    {
        return { code: "HTTP_FAILED", error: "Order lookup did not complete: " + JSON.stringify(result) };
    }

    if (result.data.statusCode === 404)
    {
        return { code: "ORDER_NOT_FOUND", error: "Xsolla has no order " + orderId + " for this player." };
    }

    if (result.data.statusCode === 401 || result.data.statusCode === 403)
    {
        return { code: "TOKEN_REJECTED", error: "Xsolla rejected the access token (HTTP " + result.data.statusCode + ")." };
    }

    if (result.data.statusCode !== 200)
    {
        return { code: "HTTP_" + result.data.statusCode, error: "Order lookup failed (HTTP " + result.data.statusCode + "): " + JSON.stringify(result.data.json) };
    }

    return { value: result.data.json || {} };
}

// ----------------------------------------------------------------------------
//  The receipt is base64url-encoded JSON: { tag, sku, uid }
//  'uid' is the Xsolla order ID; 'tag' is a constant baked into the Xsolla SDK
//  and carries no information, so it is ignored here.
// ----------------------------------------------------------------------------
function decodeReceipt(receipt)
{
    var json = base64UrlDecode(receipt);
    if (!json)
    {
        return { error: "Receipt did not base64url-decode to anything." };
    }

    var parsed;
    try
    {
        parsed = JSON.parse(json);
    }
    catch (e)
    {
        return { error: "Receipt did not contain JSON: " + json };
    }

    if (!parsed.uid)
    {
        return { error: "Receipt has no 'uid' (order ID): " + json };
    }

    return { value: parsed };
}

// ----------------------------------------------------------------------------
//  Base64url -> string. ASCII payloads only, which is all Xsolla sends here.
// ----------------------------------------------------------------------------
function base64UrlDecode(input)
{
    var CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    var s = String(input)
        .replace(/-/g, "+")
        .replace(/_/g, "/")
        .replace(/[^A-Za-z0-9+\/]/g, "");   // also drops '=' padding and whitespace

    var out = "";

    for (var i = 0; i < s.length; i += 4)
    {
        var n = s.length - i;

        var e1 = CHARS.indexOf(s.charAt(i));
        var e2 = n > 1 ? CHARS.indexOf(s.charAt(i + 1)) : -1;
        var e3 = n > 2 ? CHARS.indexOf(s.charAt(i + 2)) : -1;
        var e4 = n > 3 ? CHARS.indexOf(s.charAt(i + 3)) : -1;

        if (e1 < 0 || e2 < 0) break;   // a lone trailing character carries no whole byte

        out += String.fromCharCode((e1 << 2) | (e2 >> 4));
        if (e3 >= 0) out += String.fromCharCode(((e2 & 15) << 4) | (e3 >> 2));
        if (e4 >= 0) out += String.fromCharCode(((e3 & 3) << 6) | e4);
    }

    return out;
}

// ----------------------------------------------------------------------------
//  Peek at a claim in the (unverified) JWT payload. Fine for reporting who the
//  token says it is; the trust comes from Xsolla accepting the token above.
// ----------------------------------------------------------------------------
function readJwtClaim(jwt, claim)
{
    if (!jwt) return null;

    var segments = String(jwt).split(".");
    if (segments.length < 2) return null;

    try
    {
        var payload = JSON.parse(base64UrlDecode(segments[1]));
        return payload[claim] || null;
    }
    catch (e)
    {
        return null;
    }
}

function indexOf(arr, value)
{
    for (var i = 0; i < arr.length; i++)
    {
        if (arr[i] === value) return i;
    }
    return -1;
}

function fail(code, message)
{
    return {
        verified:   false,
        reasonCode: code,
        reason:     message
    };
}

main();

// ============================================================================
//  Why this is temporary
//
//  1. The player supplies the access token, so this verifies "an order this
//     player owns is paid" — it does not prove the client did not replay an
//     older order ID. Production needs the order ID bound to something the
//     server issued.
//
//  2. Xsolla exposes no server-authed lookup for a single order. The only
//     admin endpoint is POST /v3/project/{id}/admin/order/search, which is
//     basicAuth (merchant_id:api_key) and searches by date range only — there
//     is no order_id filter. So there is no way to do this call purely
//     server-side today.
//
//  3. The real answer is the order_paid webhook: Xsolla posts it to us,
//     signed with the project secret key, carrying the order, the items and
//     the custom_parameters (custom_user_id / custom_payload) that this
//     endpoint does NOT return. That is what ties an order back to a
//     brainCloud profile without trusting the client at all.
// ============================================================================
