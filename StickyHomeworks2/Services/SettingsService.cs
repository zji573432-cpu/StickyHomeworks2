using System.ComponentModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Hosting;
using StickyHomeworks.Models;

namespace StickyHomeworks.Services;

public class SettingsService : ObservableRecipient, IHostedService
{
    private Settings _settings = new();

    public SettingsService(IHostApplicationLifetime applicationLifetime)
    {
        PropertyChanged += OnPropertyChanged;
        Settings.PropertyChanged += (o, args) => OnSettingsChanged?.Invoke(o, args);
        LoadSettings();
        //applicationLifetime.ApplicationStopping.Register(SaveSettings);
        OnSettingsChanged += OnOnSettingsChanged;
    }

    private void OnOnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        SaveSettings();
    }

    public void LoadSettings()
    {
        if (!File.Exists("./Settings.json"))
        {
            return;
        }
        var json = File.ReadAllText("./Settings.json");
        var r = JsonSerializer.Deserialize<Settings>(json);
        if (r != null)
        {
            Settings = r;
            //Settings.PropertyChanged += (sender, args) => SaveSettings();
        }
    }

    public void SaveSettings()
    {
        File.WriteAllText("./Settings.json", JsonSerializer.Serialize<Settings>(Settings));
    }

    public event PropertyChangedEventHandler? OnSettingsChanged;

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings))
        {
            Settings.PropertyChanged += (o, args) => OnSettingsChanged?.Invoke(o, args);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }

    public Settings Settings
    {
        get => _settings;
        set
        {
            if (Equals(value, _settings)) return;
            _settings = value;
            OnPropertyChanged();
        }
    }
}