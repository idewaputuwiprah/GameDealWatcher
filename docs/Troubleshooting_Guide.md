# Troubleshooting Guide

## Common Issues

### Application Fails to Build
- **Symptom**: `dotnet build` fails with errors.
- **Cause**: Missing .NET 9.0 SDK.
- **Fix**: Install .NET 9.0 SDK from https://dotnet.microsoft.com/download/dotnet/9.0

### SQLite Database Locked
- **Symptom**: `SQLiteException: database is locked`.
- **Cause**: Another instance is using the database.
- **Fix**: Close all running instances of GameDealWatcher. Delete `%LOCALAPPDATA%\GameDealWatcher\Database\game_deals.db`.

### Steam/Epic APIs Return 429
- **Symptom**: Providers fail with HTTP 429.
- **Cause**: Rate limit exceeded.
- **Fix**: Wait 5-10 minutes. Refresh interval is configurable in Settings. Reduce refresh frequency.

### Notifications Not Showing
- **Symptom**: No toast notifications appear.
- **Cause**: Windows notification settings or API limitations.
- **Fix**: Ensure notifications are enabled in Windows Settings and GameDealWatcher Settings.

### Images Not Loading
- **Symptom**: Game cards show no images.
- **Cause**: Image cache issues or network errors.
- **Fix**: Clear image cache from Settings → Storage → Clear Image Cache.

### Application Starts Slowly
- **Symptom**: Long startup time.
- **Cause**: Network timeout on startup refresh.
- **Fix**: Application now loads cached data first, then refreshes in background. If slow, check internet connection.

### Duplicate Deals on Refresh
- **Symptom**: Same deal appears multiple times.
- **Cause**: Provider ID mismatch.
- **Fix**: Providers use `ProviderName + ProviderGameId` as unique key. This should prevent duplicates. If duplicates appear, check provider implementation.

### "Start with Windows" Not Working
- **Symptom**: Application doesn't launch on Windows startup.
- **Cause**: Shortcut not created or Task Scheduler not configured.
- **Fix**: Manually create shortcut in `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\` or configure Task Scheduler.

### Offline Mode Issues
- **Symptom**: Application shows error when offline.
- **Cause**: Application attempting network request instead of showing cached data.
- **Fix**: Application now shows cached data first. Ensure database has existing data. Check `LastSuccessfulRefresh` timestamp.

### Invalid JSON from Provider
- **Symptom**: Provider returns unexpected JSON structure.
- **Cause**: API endpoint changed.
- **Fix**: Check provider code for correct endpoint. Verify API response structure. Log errors are written to `%LOCALAPPDATA%\GameDealWatcher\Logs\`.

### Database Migration Required
- **Symptom**: Application crashes on startup after update.
- **Cause**: Database schema changed.
- **Fix**: `DatabaseInitializer.InitializeAsync` uses `CREATE TABLE IF NOT EXISTS` which is safe. For major schema changes, manual migration may be required.

### Memory Issues with Large Image Cache
- **Symptom**: Application uses excessive memory.
- **Cause**: Image cache growing too large.
- **Fix**: Clear cache from Settings → Storage → Clear Image Cache.

## Log Files
All logs are stored at `%LOCALAPPDATA%\GameDealWatcher\Logs\`. Check logs for:
- Provider request errors
- Database operation failures
- HTTP errors
- Parsing errors
- Notification errors

## Support
If issues persist:
1. Check logs in `%LOCALAPPDATA%\GameDealWatcher\Logs\`.
2. Verify internet connectivity.
3. Try manual refresh from the dashboard.
4. Clear cache and database from Settings.
5. Reinstall the application.
