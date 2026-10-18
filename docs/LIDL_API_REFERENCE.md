# SmartCart — Lidl API and Data Source Reference

**Updated:** 2026-10-19  
**Country:** Germany (`DE`)  
**Scope:** Lidl branch discovery and advertised offers for the SmartCart project  
**Status:** Technical research and local proof of concept; **not** an official/public developer API contract.

> **Provenance:** The three principal Lidl Plus endpoints below were included in our SmartCart discussion on **30 September 2026**. The complete, parameterized Neuss search URL and the HTTP troubleshooting results were established in the subsequent October 2026 conversations. Additional official Lidl pages and open-source references were checked for this document. Do not treat the URLs below as formally supported or guaranteed to remain available.

## 1. Quick reference: the three principal API endpoints

| Purpose | HTTP | Endpoint / template | Verification |
| --- | --- | --- | --- |
| Retrieve German Lidl branch catalog | GET | `https://stores.lidlplus.com/api/v4/DE` | Used by the reference open-source client; not tested successfully from this document's environment |
| Search branches by name, city, or postcode | GET | `https://stores.lidlplus.com/api/v1/autocomplete/DE?input={query}&language=de&latitude={lat}&longitude={lon}` | Parameters confirmed in open-source implementation; omitting latitude/longitude returned a validation error in the user's browser |
| Retrieve offers for a specific store | GET | `https://offers.lidlplus.com/app/api/v4/DE/{storeKey}/offers` | **User-confirmed** JSON in Chrome and `200 OK` using .NET server-side `HttpClient` after adding headers |

**Base URLs** (historically mentioned as host prefixes, not complete operations):

```text
https://stores.lidlplus.com/api/
https://offers.lidlplus.com/app/api/
```

### Links to open

- [Lidl stores catalog (Germany)](https://stores.lidlplus.com/api/v4/DE)
- [Lidl store autocomplete (Neuss, complete parameters)](https://stores.lidlplus.com/api/v1/autocomplete/DE?input=Neuss&language=de&latitude=51.198&longitude=6.692)
- [Lidl offers (example store DE5868)](https://offers.lidlplus.com/app/api/v4/DE/DE5868/offers)

## 2. Store catalog endpoint

**URL**

```http
GET https://stores.lidlplus.com/api/v4/DE
```

**Expected purpose:** Return the branch catalog for Germany, including information used for store selection. The `lidl-discounts` reference client treats the response as a list of stores and can search locally by `storeKey`, name, address, postcode, and locality.

**SmartCart usage:** An alternative when autocomplete does not find a suitable store. Downloading the full catalog every time a user types a character is unnecessary; search and cache responsibly if this route is used and permitted.

**Verification note:** The endpoint appeared in the conversation on 30 September and in the reference client's code. This report does not claim to have independently retrieved a fresh catalog response today.

## 3. Autocomplete / store search endpoint

**Template**

```http
GET https://stores.lidlplus.com/api/v1/autocomplete/DE?input={query}&language=de&latitude={lat}&longitude={lon}
```

**Ready-to-test example: Neuss**

```text
https://stores.lidlplus.com/api/v1/autocomplete/DE?input=Neuss&language=de&latitude=51.198&longitude=6.692
```

[Open the Neuss autocomplete example](https://stores.lidlplus.com/api/v1/autocomplete/DE?input=Neuss&language=de&latitude=51.198&longitude=6.692)

### Parameters

| Parameter | Example | Notes |
| --- | --- | --- |
| `input` | `Neuss` | Search term (town, store name, postcode or address as supported) |
| `language` | `de` | The reference client passes the language prefix (`de`), not `DE` |
| `latitude` | `51.198` | Approximate Neuss city-center latitude; search/ranking context, **not** the user's exact location |
| `longitude` | `6.692` | Approximate Neuss city-center longitude |

**Observed failure with incomplete URL:**

```text
https://stores.lidlplus.com/api/v1/autocomplete/DE?input=Neuss&language=DE
```

The user received `400 Bad Request` with validation errors stating that both `latitude` and `longitude` were missing. Sending the coordinates is therefore necessary for the observed API contract. `language=de` follows the reference implementation. Whether additional query parameters are supported is unknown.

**Expected SmartCart result:** Display candidate stores with the retailer-provided identity, name, address, and `storeKey` when returned. Save only the selected store key (and optionally a short display label) in browser-local preferences. Do not fabricate store keys from addresses.

## 4. Store-specific offers endpoint

**Template**

```http
GET https://offers.lidlplus.com/app/api/v4/DE/{storeKey}/offers
```

**Known test store:** `DE5868`

```text
https://offers.lidlplus.com/app/api/v4/DE/DE5868/offers
```

[Open example offers JSON](https://offers.lidlplus.com/app/api/v4/DE/DE5868/offers)

**Location note:** The independent reference client labels `DE5868` as **Hamburg-Horn (Manshardtstraße 74)**. It is only a **connectivity example**, not the correct default Lidl store for Neuss.

### Response fields observed in the user's successful JSON

| JSON field | SmartCart meaning / caveat |
| --- | --- |
| `offers[]` | List of advertised offers |
| `id` | Offer identifier |
| `offerType` | Type of promotion, e.g. `StoreSpecialPriceDiscount` |
| `redemptionChannel` | Channel in which the offer can be used |
| `category` | Source-specific classification |
| `title` | Advertised product name |
| `brand` | Brand, sometimes null |
| `productIds[]` | **Lidl-specific IDs; do not assume they are EANs/GTINs** |
| `priceBox.largePartNumeric` | Promotional price in the user's example response; verify semantics for other offer types |
| `priceBox.smallPartNumeric` | Displayed crossed-out/reference price in examples; **do not always assume normal shelf price** |
| `priceBox.discountMessage` | Displayed promotion text such as percentage or multibuy offer |
| `packaging` | Free-form package and limitation details; may mention multiple sizes, standard price, or maximum quantity |
| `pricePerUnit` | Free-form unit price; may contain several values for multiple product variants |
| `startValidityDateUTC` | Validity start timestamp in UTC |
| `endValidityDateUTC` | Validity end timestamp in UTC |
| `imageUrl` | Image URL returned for that specific offer; URLs are dynamic and should come from the response |

**Important parsing rule:** A response such as `Knorr Fix` can cover several variants and weights, e.g. `46/40/43/36/... g`, and list multiple unit prices. Do not assign one invented package size or single unit-price value to all variants. Keep ambiguous offers out of automatic unit-price ranking until resolved.

**Offer date note:** Lidl publishes ISO timestamps and localized date fields. Prefer the UTC fields for machine processing; display the dates in the user's locale.

## 5. Technical access tests already performed (October 2026)

The user directly confirmed the following behavior:

| Client | Result | Interpretation |
| --- | --- | --- |
| Chrome, normal browsing | JSON returned | Direct navigation works in that browser/session |
| Chrome Incognito | JSON returned | A previously saved browsing session is not required for every successful navigation |
| `fetch()` from Blazor's origin | Browser CORS failure | Lidl did not include `Access-Control-Allow-Origin` allowing the SmartCart origin |
| PowerShell `curl.exe`, basic GET | HTTP 400, empty response | The endpoint did not accept that form of request |
| .NET `HttpClient` with HTTP/2 only | HTTP 400 | Protocol version alone did not resolve the rejection |
| .NET `HttpClient`, HTTP/2 plus `Accept: application/json` and `Accept-Language: de-DE` | **HTTP 200 and nonempty offers JSON** | **Successful local server-side proof of concept** |

The successful test changed several request properties relative to previous attempts. It **does not prove** which individual header(s) are strictly necessary for all future requests or stores.

### Minimal .NET proof-of-concept request (server-side only)

```csharp
using System.Net;
using System.Net.Http.Headers;

var storeKey = "DE5868"; // Testing only; obtain selected key from store search.

using var request = new HttpRequestMessage(
    HttpMethod.Get,
    $"https://offers.lidlplus.com/app/api/v4/DE/{storeKey}/offers")
{
    Version = HttpVersion.Version20,
    VersionPolicy = HttpVersionPolicy.RequestVersionExact
};

request.Headers.Accept.Add(
    new MediaTypeWithQualityHeaderValue("application/json"));
request.Headers.AcceptLanguage.ParseAdd("de-DE");

using var response = await httpClient.SendAsync(request, cancellationToken);
var json = await response.Content.ReadAsStringAsync(cancellationToken);

// Check the upstream status, validate JSON, and handle failures before returning results.
```

Use a strongly validated store key (`DE` followed by digits), a fixed HTTPS upstream host, a short timeout, and appropriate error handling. Do not accept arbitrary URLs as a proxy parameter. Avoid logging entire offer responses unnecessarily. Do not store browser/anti-bot cookies or tokens.

### Browser CORS versus server-side calls

- A successful direct Chrome navigation **does not** imply `fetch()` from SmartCart.Web is allowed.
- `SmartCart.Api` can make a server-to-server HTTP request without browser CORS restrictions, but the upstream may independently refuse a request.
- When SmartCart.Web calls SmartCart.Api from another origin, configure CORS only on **your own API** (or serve both from the same origin).
- A working endpoint or an Apache-licensed client library does **not** establish Lidl's authorization for production/commercial reuse of its data.

## 6. Is there an API for searching Lidl products directly by name?

**No separate, confirmed official Lidl Plus product-search endpoint was identified in the September 30 conversation or subsequent local tests.** Do not invent one.

For the present SmartCart integration, the supported workflow is:

1. Find or select a Lidl branch using store search.
2. Retrieve `offers[]` for the branch.
3. Locally match `title`, `brand`, and any available product information against the user's personal products and verified aliases.
4. Show current or published-upcoming matching offers and their validity windows.

This searches **the returned offer collection**, not Lidl's complete product catalog. A missing match is **not evidence** that Lidl doesn't sell the product or that it has no other price available.

## 7. Additional useful official Lidl links (webpages, **not APIs**)

These are useful to cross-check branches, retailer offer descriptions, and program conditions. They should not be treated as machine-data access contracts.

| Link | Use |
| --- | --- |
| [Lidl branches in Neuss](https://www.lidl.de/s/de-DE/filialen/neuss/) | Official Neuss branch addresses; helps check store-search results |
| [All Lidl branches in Germany](https://www.lidl.de/s/de-DE/filialen/) | Public store finder |
| [Lidl online brochures](https://www.lidl.de/c/online-prospekte/s10005610/) | Consumer-facing promotions and flyers, including weekly promotions where published |
| [Lidl Plus](https://www.lidl.de/c/lidl-plus/s10007388) | Loyalty program details and member-specific restrictions |
| [How Lidl Plus works](https://www.lidl.de/c/wie-funktioniert-lidl-plus/s10011363) | Helps distinguish ordinary offers, app promotions, and member coupons |

The official Neuss page lists branches; **it does not directly provide a verified `storeKey` for the Lidl Plus offers API**.

## 8. Open-source references and other potential data suppliers

### Primary reference (already cited on 30 September)

- [EvickaStudio/lidl-discounts](https://github.com/EvickaStudio/lidl-discounts) — unofficial, read-only Python client for branch search and offers.
- [Endpoint configuration](https://github.com/EvickaStudio/lidl-discounts/blob/master/lidl/config.py) — declares `stores.lidlplus.com/api/` and `offers.lidlplus.com/app/api/`.
- [Endpoint implementations](https://github.com/EvickaStudio/lidl-discounts/blob/master/main.py) — see `stores()`, `search_stores()`, `search_stores_from_catalog()`, and `offers()` for actual routes and parameters.

**Reference behavior:** `search_stores()` sends `input`, `language`, `latitude`, and `longitude` to autocomplete. If it yields no results, the client can search the full store catalog by name, postcode, address, or store key. The code uses the same three route families shown above.

### Other references (optional, not the September 30 endpoints)

- [zsobix/lidlplus-api](https://github.com/zsobix/lidlplus-api) — unofficial client that documents store, offer, and brochure *methods*; some other features require authentication. **Do not infer unverified raw URLs from method names.**
- [ReefAPI Lidl integration](https://reefapi.com/lidl-api) — third-party paid/developer service with documented API and Lidl search/deals/product-detail operations. Evaluate geographic coverage, pricing, and license separately if official or directly accessible Lidl Plus feeds become unsuitable.
- [everydata.io Lidl API](https://everydata.io/apis/lidl) — third-party data source; check whether its coverage concerns online-shop products rather than the specific nearby in-store offers SmartCart requires.

These third-party services are **not Lidl-operated official developer APIs** merely because their names include Lidl.

## 9. Suggested SmartCart integration (smallest useful implementation)

```text
SmartCart.Web (Blazor WebAssembly)
    |
    | Store search (city/postcode) + select one storeKey
    v
SmartCart.Api (ASP.NET Core)
    |-- Lidl store search -> stores.lidlplus.com/api/v1/autocomplete/DE
    |-- Fallback catalog -> stores.lidlplus.com/api/v4/DE
    |-- Selected-store offers -> offers.lidlplus.com/app/api/v4/DE/{storeKey}/offers
    |
    v
Normalized retailer offer DTOs
    |
    v
SmartCart.Web: match offers against user's IndexedDB products + aliases
    |
    v
ShoppingPlanner: current/upcoming offer + validity + restrictions
```

**Proposed own-API routes (SmartCart routes, not actual Lidl endpoints):**

```http
GET /api/lidl/stores/search?query=Neuss&latitude=51.198&longitude=6.692
GET /api/lidl/offers?storeKey={selectedStoreKey}
```

Choose/store the branch in IndexedDB. **Never send receipt images, raw OCR, or the user's complete product list to Lidl.** The user's product-matching logic should remain local in Blazor. The API can use a short-lived cache of public branch/offer results if justified, with rate limits and a fixed upstream host. Verify data-use permissions before enabling a public production integration.

### Unresolved items to verify

1. Which exact `storeKey` belongs to each Lidl branch in Neuss? Obtain it from the live store API rather than guessing.
2. Does the server-side request for the **store API** need the same headers as the successfully tested offers request?
3. Are prices available to all shoppers or conditional on Lidl Plus/app use?
4. How should multi-variant offers be mapped without inventing package sizes and regular prices?
5. What commercial/production data-use permissions apply to the unofficial Lidl Plus endpoints?
6. Is there an authorized, documented feed suitable for long-term stable production use?

## 10. Source and verification log

- **Conversation, 2026-09-30:** Three Lidl Plus route families and `EvickaStudio/lidl-discounts` `config.py` were provided. No distinct direct product-search route was recorded.
- **User browser tests, October 2026:** Opening a selected-store offers URL returned JSON with product titles, brands, dates, discounts, packaging, and price boxes.
- **User autocomplete test:** Omitting latitude/longitude returned a structured HTTP 400 validation response listing those missing parameters.
- **User .NET test:** HTTP/2 plus `Accept: application/json` and `Accept-Language: de-DE` returned HTTP 200 and populated offers JSON.
- **Reference client code:** https://github.com/EvickaStudio/lidl-discounts/blob/master/main.py
- **Reference client configuration:** https://github.com/EvickaStudio/lidl-discounts/blob/master/lidl/config.py
- **Official Lidl Neuss branches:** https://www.lidl.de/s/de-DE/filialen/neuss/
- **Official Lidl Plus information:** https://www.lidl.de/c/lidl-plus/s10007388

---

**Bottom line:** For SmartCart's current development, use the **store autocomplete → confirmed `storeKey` → store offers** flow. We have observed a successful offers request from ASP.NET Core, but direct browser `fetch` is blocked by CORS and the unofficial endpoints still require a separate permissions/stability assessment before a public release.
