# Avalonia Migration Plan

## Phase 1: Project Setup

- [ ] Replace `src/GameDealWatcher.App/` in-place (rename existing WinUI project to `.bak` first) — do NOT create a `*.New` folder
- [ ] Target framework: net9.0
- [ ] NuGet packages:
  - Avalonia 11.0.18
  - Avalonia.Themes.Fluent 11.0.18
  - CommunityToolkit.Mvvm 8.3.2
- [ ] Remove dependencies:
  - Microsoft.WindowsAppSDK
  - WinRT.Interop
  - Microsoft.Toolkit.Uwp.Notifications (replace with custom notification via Avalonia Popup)
  - System.Drawing.Common (replace with SixLabors.ImageSharp for cross-platform image caching)
- [ ] Remove from `Infrastructure.csproj` too:
  - Microsoft.Toolkit.Uwp.Notifications
- [ ] Replace `*.resw` / `.resx` resources with `StringBinding` or plain `.resx` (Phase 2 item)
- [ ] Update ALL `*.csproj` target frameworks: change `net9.0-windows10.0.19041` → `net9.0` (cross-platform)
- [ ] Add `WindowIcon` asset migration (WinUI `ApplicationData` → Avalonia `WindowIcon` or embedded resource)

## Phase 2: Code Migration

### 1. `App.xaml` → `App.axaml`

- Replace `<Application ...>` → `<Application ... xmlns="https://github.com/avaloniaui">`
- Replace `Microsoft.UI.Xaml.Application` → `Application` (Avalonia)
- Add Fluent theme:
  ```xml
  <Styles>
    <FluentTheme Mode="Light"/>
  </Styles>
  ```

### 2. `MainWindow.xaml`

- Convert `{ThemeResource BrushName}` → `{DynamicResource BrushName}`
- Convert `{x:Bind ViewModel.X}` → `{Binding X}`
- Replace `<Frame>` → `<ContentControl Content="{Binding CurrentView}"/>`
  - Create a `NavigationService` or expose `CurrentView` (ViewModel) on `MainWindowViewModel`
- Remove WinUI-specific: `ExtendsContentPresenterSize`, `ApplicationData`, etc.

### 3. `MainWindow.xaml.cs`

- Remove Win32 interop (`WindowNative`, `AppWindow`, `Win32Interop`)
- Use Avalonia base:
  ```csharp
  this.Width = 1000;
  this.Height = 600;
  this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
  ```

### 4. Views (DashboardView, EpicView, SettingsView, SteamView)

- Convert `{x:Bind}` → `{Binding}`  
- Convert `{ThemeResource}` → `{DynamicResource}`
- Replace `ListView.ItemTemplate` with `<DataTemplate>`
- Convert `x:Bind N:N` multi-bindings → `IMultiValueConverter` (if used)

### 5. Notification Service Migration

- **Location:** `src/GameDealWatcher.Infrastructure/Notifications/NotificationService.cs`
- Current: `Windows.Toolkit.Uwp.Notifications.ToastContentBuilder`
- New: Implement `INotificationService` using `Avalonia.Controls.Primitives.Popup`
  ```csharp
  var popup = new Popup {
      PlacementTarget = window,
      Content = new TextBlock { Text = deal.Title },
      IsOpen = true
  };
  DispatcherTimer.Run(() => popup.IsOpen = false, TimeSpan.FromSeconds(3));
  ```
- Interface stays in `Infrastructure/Notifications/NotificationService.cs`
- **Implementation moves to App layer** (`App/Services/AvaloniaNotificationService.cs`) since `Popup` requires window references from UI layer
- DI registration in `App.axaml.cs` updated to resolve `AvaloniaNotificationService`
- **No external dependency** (`MessageBox.Avalonia` adds risk of breaking Mvvm conventions)

### 6. ViewModels

- **No changes needed** — already use CommunityToolkit.Mvvm correctly
- Verify all `[ObservableProperty]` and `[RelayCommand]` survive post-migration build

## Phase 3: CI Workflow (`.github/workflows/build.yml`)

- [ ] Create `.github/workflows/` directory and `build.yml` if not present (no workflow file exists)
### Remove:

- `GenerateAppxPackageOnBuild=false` (WinUI-specific)
- `-p:UseAppHost=true` simplification

### New Build Command:

```yaml
- name: Build App
  run: dotnet publish src/GameDealWatcher.App/GameDealWatcher.App.csproj -c Release -o publish
```

### Runner:

- Keep `windows-2022` if Win32 packaging (AOT) still needed
- Switch to `ubuntu-latest` if pure .NET 9 publish is acceptable (smaller artifact)

### Artifact:

- Update `artifact-path` from `**/*.msixupload` → `publish/**`

## Phase 4: Testing

| Test | Notes |
|------|-------|
| Local build | Verify `dotnet build` passes for all 4 projects |
| Binding diagnostics | Enable `DataEntry` debug output in `App.axaml.cs` (`<DiagnosticsHandlers>`) |
| Notifications | Trigger test deal → assert Popup opens/closes |
| Navigation | Switch tabs → assert `ContentControl.Content` changes |
| Data loading | Verify `SteamProvider`/`EpicGamesProvider` calls still resolve |
| CI artifact | Download `publish/` bundle → run binary |

### Estimated Timeline: 6–8 hours

> WinUI → Avalonia is a full rewrite of the view layer, not a 1:1 swap. Binding failures surface only at runtime. Budget time for iteration.

## Open Questions

1. Does the team want Linux builds (cross-platform)? If yes → `ubuntu-latest` + remove WinForms interop.
2. Keep MSIX packaging? If yes → `windows-2022` + `dotnet publish -p:PublishSingleFile=true`.
3. Are `.resw` files present? If yes, must migrate to `.resx` or remove. → **No `.resw` files found; nothing to migrate.**
4. `Application.csproj` references `Infrastructure.csproj` directly (unusual DI direction for clean architecture). Consider whether notification implementation belongs in App layer or a new adapter assembly.

---

✅ Ready to proceed with Phase 1
