# Retailer offer-source assessment

## Decision

SmartCart does not call Lidl or Aldi offer pages or internal endpoints. No source was verified that combines documented authorization for machine use with a supported browser-accessible interface, suitable CORS behavior, and dependable offer identity/validity/restriction data. The planner therefore uses explicitly labeled synthetic development examples only. Sample prices are not real retailer prices, are not current, have no source URL or validity period, and are excluded from comparison results.

## What was checked

These checks were limited to publicly reachable official pages and HTTP responses during implementation. They are observations of the current access path, not a legal determination or a guarantee of availability to every browser, region, or user.

| Retailer/source | Observed response | Access and data findings |
| --- | --- | --- |
| [Lidl Germany homepage](https://www.lidl.de/) | HTTP 200 without an authenticated session in the check | Contains consumer-facing promotional product cards, including Lidl Plus/login-related content. It is not a documented offer API. The tested HTML response did not include `Access-Control-Allow-Origin`. |
| [Lidl presumed offers path](https://www.lidl.de/c/angebote/a10005723) | HTTP 404 in the check | The path was not a usable source. The homepage links to changing campaign/offer pages, which are not a stable machine contract. |
| [ALDI Nord current offers](https://www.aldi-nord.de/angebote.html) | HTTP 200 without an authenticated session in the check | The page represents current-week offers and embeds an internal `OFFER_GET` page-state payload. No public API specification or explicit permission for this app to consume that internal payload was identified. The tested HTML response did not include `Access-Control-Allow-Origin`. |
| [ALDI Nord robots.txt](https://www.aldi-nord.de/robots.txt) | HTTP 200 | Lists a sitemap and disallows some paths; it is not a license or an API authorization grant. The offer page's presence/absence in crawler rules does not establish permission for application data use. |
| [ALDI Süd offers](https://www.aldi-sued.de/de/angebote.html) | HTTP 403 from the check environment | This does not prove the page is universally blocked or requires authentication; region, network, or bot protections may affect the response. No browser integration was verified. |

## Authorization, browser access, and authentication

- The official pages are meant for consumers to view. No public developer documentation, supported offer API, or explicit machine-consumption authorization for this app was found in the reviewed page/source material. Public visibility alone is not treated as permission to scrape or reuse offer data.
- Tested retailer HTML responses lacked an `Access-Control-Allow-Origin` header. A standalone WebAssembly app cannot rely on cross-origin `fetch`/XHR access to those pages. A page being reachable by direct navigation is different from allowing another origin to read it.
- The ALDI Nord and Lidl page checks succeeded without a login cookie, but Lidl's page also exposes Lidl Plus/login-related offers. The app does not access login-protected or personalized offers. ALDI Süd returned 403 in this environment, and its authentication/access requirements were not established.

## Coverage and validity limitations

The checked pages expose selected promotional items rather than a verified complete product catalog. Offers can be campaign-, time-, region-, store-, or loyalty-restricted. Although consumer pages can display dates and prices, there is no verified stable data contract here that guarantees normalized product/package identity, validity windows, restrictions, store availability, or completeness. Those limitations prevent reliable “cheapest” claims and unit-price comparison from the pages alone.

## Current demo and future option

`DevelopmentDemoRetailerOfferProvider` supplies fabricated examples for UI development. The planner labels every entry as demo, reports that its source and validity are not verified, and does not present it as a real available offer. No direct retailer request, proxy, or server integration was added.

A future server-side proxy could address browser CORS only after the retailer has granted/ documented the required access and a stable interface is identified. A proxy does not itself grant permission, resolve authentication or coverage restrictions, or validate offer data. Any future integration must first confirm those points and preserve source, retrieval, validity, package, and restriction metadata.
