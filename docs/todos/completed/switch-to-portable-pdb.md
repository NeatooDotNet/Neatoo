# Switch to Portable PDB with Symbols Package

## Problem

Embedded PDBs increase DLL size by 30-50%+, which impacts Blazor WASM download times. Currently both `Neatoo.csproj` and `Neatoo.Blazor.MudNeatoo.csproj` use `<DebugType>embedded</DebugType>`.

## Solution

Switch to portable PDBs and publish a separate `.snupkg` symbols package to NuGet.org.

## Tasks

- [x] Change `<DebugType>embedded</DebugType>` to `<DebugType>portable</DebugType>` in `src/Neatoo/Neatoo.csproj`
- [x] Change `<DebugType>embedded</DebugType>` to `<DebugType>portable</DebugType>` in `src/Neatoo.Blazor.MudNeatoo/Neatoo.Blazor.MudNeatoo.csproj`
- [x] Add `<IncludeSymbols>true</IncludeSymbols>` to both projects
- [x] Add `<SymbolPackageFormat>snupkg</SymbolPackageFormat>` to both projects
- [x] Update `.github/workflows/build.yml` to upload and push `.snupkg` to NuGet.org
- [x] Configure Source Link with `Microsoft.SourceLink.GitHub` package for symbol server debugging
- [ ] Test that symbols are available via NuGet symbol server after publish

## Benefits

- Smaller DLL size for Blazor WASM apps
- Faster initial load times
- Symbols still available for debugging via NuGet symbol server
- Source Link enables stepping into Neatoo source code

## References

- [NuGet Symbol Packages](https://learn.microsoft.com/en-us/nuget/create-packages/symbol-packages-snupkg)
- [Source Link](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink)
