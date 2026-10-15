# ShoppingAssistant Solution Plan

## Goal
Create a new .NET solution for the ShoppingAssistant architecture without modifying any existing repositories.

## Architecture
- **Web / API** — application entry point and HTTP API.
- **Application** — ReceiptService, ProductService, OfferService, ProductMatchingService.
- **Infrastructure** — database, OCR, and retailer integrations, including LidlOfferProvider.
- **Domain** — Product, ProductAlias, Receipt, ReceiptItem, and Offer.

## Workspace and tooling
- Solution root: `C:\Users\maksoud\source\repos\ShoppingAssistant`.
- Workspace inspection found no existing ShoppingAssistant directory, so this root is isolated from unrelated files.
- Installed .NET SDKs: 7.0.410, 8.0.425, 9.0.306, 10.0.112, and 10.0.300.
- `dotnet` CLI and Visual Studio MSBuild are available. Target .NET 10.

## Dependency direction
- Web/API references Application and Infrastructure.
- Infrastructure references Application and Domain.
- Application references Domain.
- Domain has no project references.

## Steps
1. Inspect the workspace contents, installed .NET SDKs, and restore/build tooling; establish the solution root and avoid overwriting unrelated files.
2. Create the isolated solution root and save this plan as `PLAN.md`.
3. Scaffold the solution and four layer projects with the requested architecture folders.
4. Add project references following the intended dependency direction.
5. Restore and build the solution to validate the scaffold.
