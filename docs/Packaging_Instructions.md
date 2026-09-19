# Packaging Instructions

## Prerequisites
- Visual Studio 2022 (version 17.8+)
- Windows 10/11
- .NET 9.0 SDK

## MSIX Packaging (Recommended)
1. Open `GameDealWatcher.sln` in Visual Studio 2022.
2. Right-click the solution → Add → New Project.
3. Select **Windows Application Packaging Project**.
4. Set the target version to Windows 10/11.
5. Add a reference to `GameDealWatcher.App`.
6. Configure the `Package.appxmanifest`:
   - Set display name, publisher, and version.
   - Add capabilities: `internetClient`, `localSystem`.
7. Right-click the packaging project → Publish → Create App Packages.
8. Follow the wizard to generate `.msix` or `.msixbundle`.

## Unpackaged Distribution (Self-Contained)
1. Build the application:
   ```bash
   dotnet publish src/GameDealWatcher.App/GameDealWatcher.App.csproj -c Release -r win-x64 --self-contained true
   ```
2. The output will be in `bin\Release\net9.0-windows10.0.19041\win-x64\publish\`.
3. Distribute the folder as a zip archive or installer.

## Start with Windows
To enable startup with Windows:
1. Add a shortcut to the executable in:
   ```
   %APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\
   ```
2. Or register via the Windows Task Scheduler using the `--background` flag.

## Windows Task Scheduler for Background Refresh
```powershell
$schTask = @{
    TaskName = "GameDealWatcherRefresh"
    Trigger = New-JobTrigger -Daily -At "08:00AM"
    Action = New-ScheduledTaskAction -Execute "GameDealWatcher.exe" -Argument "--background"
    Settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
}
Register-ScheduledTask @schTask
```

## Creating Application Icon
1. Create a `.ico` file (256x256, 16x16, 32x32).
2. Place in `src/GameDealWatcher.App/Resources/`.
3. Reference in `App.xaml`:
   ```xml
   <Application.Icon>Resources/GameDealWatcher.ico</Application.Icon>
   ```

## Build Instructions
1. Open Developer Command Prompt for VS 2022.
2. Navigate to the project directory.
3. Run:
   ```bash
   dotnet build GameDealWatcher.sln
   ```
4. For release build:
   ```bash
   dotnet build GameDealWatcher.sln -c Release
   ```
