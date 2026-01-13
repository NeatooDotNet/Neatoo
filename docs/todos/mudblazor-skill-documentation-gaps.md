# MudBlazor Skill Documentation Gaps

MudBlazor skill documentation gaps identified during NeatooATM implementation (2026-01-11).

Source: `NeatooATM/docs/todos/neatoo-blazor-implementation-plan.md`

---

## Task List

### Installation & Setup

- [ ] **Material Design Icons Font Missing**: Documentation doesn't mention that MudBlazor icons (`Icons.Material.*`) require the Material Symbols font to be loaded:
  ```html
  <link href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined" rel="stylesheet" />
  ```
  Without this, icon buttons and menu items may show empty squares or fallback text.

- [ ] **Blazor WebAssembly vs Server Distinction**: The installation guide conflates Blazor Server and Blazor WebAssembly setup:
  - For **Blazor Server/.NET 9+ SSR**: Uses `@Assets["..."]` Razor syntax in `.razor` files
  - For **Blazor WebAssembly**: Uses static `index.html` with direct paths or `#[.{fingerprint}]` placeholders

  The skill should have separate setup sections for each hosting model.

- [ ] **.NET 10 Asset Fingerprinting for WebAssembly**: The skill mentions `.NET 9+` asset fingerprinting with `@Assets[...]` but doesn't explain:
  - For Blazor WASM `index.html`, use placeholder syntax: `blazor.webassembly#[.{fingerprint}].js`
  - Requires `<OverrideHtmlAssetPlaceholders>true</OverrideHtmlAssetPlaceholders>` in csproj
  - The dev server replaces placeholders at runtime
  - **Actually simpler**: Just use `blazor.webassembly.js` without fingerprinting for development

- [ ] **Complete index.html Example Missing**: The skill should include a complete working `index.html` example for Blazor WebAssembly:
  ```html
  <!DOCTYPE html>
  <html lang="en">
  <head>
      <meta charset="utf-8" />
      <meta name="viewport" content="width=device-width, initial-scale=1.0" />
      <base href="/" />
      <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
      <link href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined" rel="stylesheet" />
      <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
      <link href="MyApp.styles.css" rel="stylesheet" />
  </head>
  <body>
      <div id="app">Loading...</div>
      <script src="_content/MudBlazor/MudBlazor.min.js"></script>
      <script src="_framework/blazor.webassembly.js"></script>
  </body>
  </html>
  ```

---

## Context

These gaps were discovered while implementing the NeatooATM demonstration application. Each gap required trial and error during implementation to discover the correct configuration.

The goal is to update skill documentation so future implementations don't require these workarounds.
