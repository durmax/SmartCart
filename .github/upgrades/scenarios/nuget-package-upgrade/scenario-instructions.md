# NuGet Package Upgrade

## Preferences
- **Flow Mode**: Automatic
- **Scope**: `src/SmartCart.Infrastructure/SmartCart.Infrastructure.csproj` only; validate downstream consumers including `tests/SmartCart.Application.Tests/SmartCart.Application.Tests.csproj`.
- **Package**: `System.Security.Cryptography.Pkcs`; use the newest supported stable version reported by assessment.
- **User constraint**: Address only the `System.Security.Cryptography.Pkcs` 9.0.11 NU1902/NU1903 vulnerability. Never apply, alter, generate, or run a database migration or `Update-Database`; do not change schema or application behavior; do not suppress the vulnerability warning. Preserve this constraint through assessment, implementation, and validation.
- **User-confirmed source-control decision**: Stash existing uncommitted work, including the existing migration-related changes, before starting. Do not modify or discard those stashed changes.

## Source Control
- **Source Branch**: dev
- **Working Branch**: nuget-package-upgrade-pkcs
- **Pending Changes**: Stashed as `pre-nuget-package-upgrade-pkcs`
- **Commit Strategy**: After Each Task
- **Branch Sync**: Auto (Merge)

## Key Decisions Log
- Dependency graph traced `Microsoft.EntityFrameworkCore.SqlServer 10.0.13` → `Microsoft.Data.SqlClient 6.1.7` → `System.Security.Cryptography.Pkcs 9.0.11` in SmartCart.Infrastructure; SmartCart.Application.Tests receives it through its Infrastructure project reference. Keep package assessment scope to Infrastructure and validate the dependent test project.
