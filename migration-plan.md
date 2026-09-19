# Avalonia Migration Plan

## Phase 1: Project Setup
- Create new Avalonia project: `dotnet new avalonia.app -o AvaloniaApp --use-xaml`
- Target framework: net9.0 (compatible with .NET 9 SDK already installed)
- Install NuGet packages:
  - Avalonia 11.0.18
  - Avalonia.Themes.Fluent 11.0.18
  - CommunityToolkit.Mvvm 8.3.2 (existing, already compatible)

## Phase 2: Code Migration

### Files to modify:
1. `App.axaml` - Replace WinUI Application base, add Fluent theme
2. `MainWindow.axaml` - Convert {ThemeResource} to {StaticResource}, {x:Bind} to {Binding}
3. `Views/*.axaml` (DashboardView, EpicView, SettingsView, SteamView) - Same conversions
4. `MainWindow.axaml.cs` - Remove WinUI interop, use Avalonia DI setup
5. ViewModels - No changes needed (already use CommunityToolkit.Mvvm)

### Key conversion patterns:
- `Microsoft.UI.Xaml` namespace → `Avalonia`
- `{ThemeResource BrushName}` → `{DynamicResource BrushName}`
- `{x:Bind ViewModel.Property}` → `{Binding ViewModel.Property}`
- `x:Bind` mode → Standard Binding mode

## Phase 3: CI Workflow (`.github/workflows/build.yml`)
- Update to build Avalonia project instead of WinUI
- Use `dotnet publish` with self-contained output
- Maintain existing artifact upload steps

## Phase 4: Testing
- Local build verification on stable Windows
- CI artifact validation
- Functional testing with existing business logic

## Timeline
- Estimated: 4-5 hours total
- Ready to proceed?
