# AGENTS.md - MarketBoardPlugin

1) Build: `dotnet build MarketBoardPlugin.sln -c Debug` (Release as needed)
2) Restore: `dotnet restore MarketBoardPlugin.sln`
3) Tests: none available; no single-test command (manual in-game `/pmb`)
4) Lint: StyleCop analyzers run during `dotnet build`; fix warnings before PRs
5) SDK: `Dalamud.NET.Sdk/14.0.0`; output under `bin/x64/Debug`
6) Indent: 2 spaces, LF endings, UTF-8 (see .editorconfig)
7) Imports: System first, then externals, then project namespaces; blank line groups
8) Headers: retain copyright header on every C# file
9) Docs: XML documentation required on public members; keep summaries meaningful
10) Naming: PascalCase for public types/members; camelCase for private fields with `this.` prefix
11) Types: Prefer explicit types; use nullable reference types (`?`) where appropriate
12) Error handling: Log via `IPluginLog`; avoid surfacing errors to users; catch broadly only with justification
13) Async: Pass `CancellationToken`; use `ConfigureAwait(false)` in library/async calls
14) Dispose: Implement full dispose pattern with `Dispose(bool)` and `GC.SuppressFinalize`
15) UI: Windows derive from `Dalamud.Interface.Windowing.Window` and override `Draw()`
16) Data: Universalis API via `Helpers/UniversalisClient` with Polly retry pipeline
17) Config: Add options in `MBPluginConfig`, expose in `GUI/MarketBoardConfigWindow`, and save via `PluginInterface.SavePluginConfig`
18) Context menu: Integration in `MBPlugin.OnContextMenuOpened`; keep item ID extraction robust
19) Fonts/Resources: `Resources/NotoSans-Medium-NNBSP.otf` embedded; preserve resource metadata
20) No Cursor or Copilot rules present in repo
