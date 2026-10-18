# SmartCart project overview

## Application architecture

SmartCart is a .NET 10 solution with a standalone Blazor WebAssembly app.

| Project | Responsibility | Dependency direction |
| --- | --- | --- |
| `SmartCart.Domain` | Product, alias, offer entities | No project dependencies |
| `SmartCart.Application` | Product matching and receipt parsing contracts/logic | Domain only |
| `SmartCart.Infrastructure` | Existing server-side persistence and integrations | Application and Domain |
| `SmartCart.Api` | Existing API host | Application and Infrastructure |
| `SmartCart.Web` | Browser UI, JavaScript interop, IndexedDB, local OCR, browser-owned offer UI/comparison | Application and Domain only |

The browser app registers services through `WebServiceCollectionExtensions`. Running `SmartCart.Web` does not start `SmartCart.Api` or use the database. API and persistence projects are unchanged by the browser-only offer work.

## Implemented browser flows

- **Dashboard:** Reads product and upcoming-list counts from the browser store and shows the real offer-source status.
- **Receipt import:** Accepts JPEG/PNG, validates size/type, and invokes a self-hosted Tesseract.js runtime and German language model. Parsed lines are reviewed before saving. The image and OCR text are not persisted.
- **My products:** Supports local CRUD, aliases, package attributes, search, and conservative product matching. Uncertain matches require review.
- **Shopping planner:** Maintains a separate upcoming-trip list with quantity edits and one-time items. Synthetic offers are marked as demo and are never represented as real or current offers.
- **Settings:** Exports/imports the product catalog JSON, confirms replacement/clear operations, and explains that browser storage can be removed by the user or browser.

IndexedDB contains user-confirmed product data and the upcoming shopping list only. It does not contain receipt images, OCR text, payment information, or transaction history. Product export does not include the shopping list.

## Offer comparison safeguards

No authorized, documented, browser-accessible Lidl/Aldi offer source has been verified. The only visible prices in the UI come from `DevelopmentDemoRetailerOfferProvider`; they are synthetic examples with no real source URL or validity dates. They are excluded from comparison claims.

The browser-owned `OfferComparisonService` is prepared for a future explicitly authorized source. It excludes demo data, unauthorized/non-HTTPS sources, restricted offers, invalid prices, missing or expired validity, and product matches that are uncertain. It compares unit prices only when package dimensions are explicit and compatible. Results are described as the best known available offer because source coverage can be incomplete.

`StockUpCalculator` is a conservative helper, not an enabled shopping-planner feature. It requires an explicit non-perishable classification, discount, planned quantity, and user quantity ceiling; unknown or perishable products receive no suggestion. The current user product model does not record perishable status, so the UI does not offer stock-up recommendations.

## Run without server services

```powershell
dotnet restore src/SmartCart.Web/SmartCart.Web.csproj
dotnet run --project src/SmartCart.Web/SmartCart.Web.csproj
```

See [Retailer data sources](RETAILER_DATA_SOURCES.md) for the source-access assessment and [the repository README](../README.md) for validation commands.
