# mu88.Shared — Repo Context

## This Is a NuGet Package, Not an Application
- The primary output is a NuGet package. Use `dotnet pack` to build it; there is nothing to `dotnet run`.
- `mu88.Shared.props` and `mu88.Shared.targets` are consumed by every downstream project. Be conservative: breaking changes here affect all consumers at their next package update.

## Test Architecture — Two Host Projects
Two test host projects exist intentionally and both must pass:
- `DummyAspNetCoreProject`: references `mu88.Shared` via **project reference** — validates behavior at source level.
- `DummyAspNetCoreProjectViaNuGet`: references `mu88.Shared` via **NuGet package** — validates the published artifact. Must be run after `dotnet pack` has produced a fresh package.
- `SharedTargetsTests.cs` verifies MSBuild targets behavior — always run this when modifying `.targets` or `.props` files.

## Internal Types
- `Mu88SharedOptions` and `OpenTelemetryOptions` are `internal`. `InternalsVisibleTo("Tests")` is declared in `mu88.Shared.csproj`.

## Bundled Assembly
- `mu88.HealthCheck` is a separate project that gets bundled into the NuGet package via custom MSBuild targets. Its `.dll` and `.pdb` are included alongside `mu88.Shared.dll`. Changes to `mu88.HealthCheck` affect the NuGet output.

## Container Publishing Target (Sensitive)
- The MSBuild target that publishes regular + chiseled container images (the E2 scenario) required significant effort to get right. When modifying it, verify by running `dotnet publish` against a real consumer project and inspecting the resulting container image before committing.
