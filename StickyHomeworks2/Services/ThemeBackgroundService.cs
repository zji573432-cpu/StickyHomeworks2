using System.ComponentModel;
using System.Diagnostics;
using ElysiaFramework;
using System.Windows.Media;
using ClassIsland.Services;
using ElysiaFramework.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;

namespace StickyHomeworks.Services;

public class ThemeBackgroundService : IHostedService
{
    private SettingsService SettingsService { get; }

    private IThemeService ThemeService { get; }

    private WallpaperPickingService WallpaperPickingService { get; }

    public ThemeBackgroundService(SettingsService settingsService, IThemeService themeService, WallpaperPickingService wallpaperPickingService)
    {
        SettingsService = settingsService;
        ThemeService = themeService;
        WallpaperPickingService = wallpaperPickingService;
        SettingsService.OnSettingsChanged += SettingsServiceOnOnSettingsChanged;
        SystemEvents.UserPreferenceChanged += SystemEventsOnUserPreferenceChanged;
        WallpaperPickingService.WallpaperColorPlatteChanged += WallpaperPickingServiceOnWallpaperColorPlatteChanged;
    }

    private void WallpaperPickingServiceOnWallpaperColorPlatteChanged(object? sender, EventArgs e)
    {
        UpdateTheme();
    }

    private Stopwatch UpdateStopWatch { get; } = new();

    private async void SystemEventsOnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        await WallpaperPickingService.GetWallpaperAsync();
        //UpdateTheme();
    }

    private void SettingsServiceOnOnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!WallpaperPickingService.IsWorking)
        {
            UpdateTheme();
        }
    }

    private void UpdateTheme()
    {
        if (UpdateStopWatch is { IsRunning: true, ElapsedMilliseconds: < 300 })
        {
            return;
        }
        UpdateStopWatch.Restart();
        var primary = Colors.DodgerBlue;
        var secondary = Colors.DodgerBlue;
        switch (SettingsService.Settings.ColorSource)
        {
            case 0: //custom
                primary = SettingsService.Settings.PrimaryColor;
                secondary = SettingsService.Settings.SecondaryColor;
                break;
            case 1:
                primary = secondary = SettingsService.Settings.SelectedPlatte;
                break;
            case 2:
                try
                {
                    NativeWindowHelper.DwmGetColorizationColor(out var color, out _);
                    var c = NativeWindowHelper.GetColor(color);
                    primary = secondary = c;
                }
                catch
                {
                    // ignored
                }

                break;
        }

        ThemeService.SetTheme(SettingsService.Settings.Theme, primary, secondary);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        UpdateTheme();
        await WallpaperPickingService.GetWallpaperAsync();
        //UpdateTheme();
        UpdateStopWatch.Start();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}