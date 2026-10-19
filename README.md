# SmartCart

SmartCart is a .NET 10 Blazor WebAssembly standalone shopping assistant. The browser app supports private receipt review, a personal product catalog, and an upcoming shopping list without requiring the API or database projects to run.

## Current functionality

- Import a Lidl receipt image and run bundled German OCR in the browser. Images and OCR text are transient and are not saved.
- Review parsed products and save only user-confirmed product details.
- Manage products, verified aliases, EANs, and package attributes in browser-local IndexedDB.
- Maintain a separate upcoming shopping list; editing its quantities does not change the product catalog.
- Export/import the product catalog as JSON and clear browser-local products and list items in Settings.
- View clearly labeled synthetic development offers. No actual Lidl Germany or ALDI SÜD offer feed is connected; real-price comparison is not yet supported because no authorized, browser-compatible source has been verified.

## Run the browser app without the API

From the repository root:

```powershell
dotnet restore src/SmartCart.Web/SmartCart.Web.csproj
dotnet run --project src/SmartCart.Web/SmartCart.Web.csproj
```

Open the local URL printed by the .NET CLI. The standalone Web project references only Application and Domain; the API and database are not needed for these browser flows.

## Validate

```powershell
dotnet build SmartCart.sln
dotnet test SmartCart.sln
```

## Architecture, privacy, and offer limitations

See [Project overview](docs/PROJECT_OVERVIEW.md) for project boundaries and browser data handling, and [Retailer data sources](docs/RETAILER_DATA_SOURCES.md) for the ALDI SÜD and Lidl source assessment, the unofficial ALDiscount research reference, and the requirements for a future authorized integration.
