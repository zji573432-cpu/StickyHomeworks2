using System.ComponentModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StickyHomeworks.Models;

namespace StickyHomeworks.Services;

public class SettingsService : ObservableRecipient, IHostedService
{
    private Settings _settings = new();
    private PropertyChangedEventHandler? _settingsHandler;
    private string? _lastChangedProperty;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(IHostApplicationLifetime applicationLifetime, ILogger<SettingsService> logger)
    {
        _logger = logger;
        PropertyChanged += OnPropertyChanged;
        SubscribeSettings();
        LoadSettings();
        OnSettingsChanged += OnOnSettingsChanged;
        Settings.EnsureGlycoproteinNodeId();
    }

    private void OnOnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        _lastChangedProperty = e.PropertyName;
        SaveSettings();
    }

    private void SubscribeSettings()
    {
        if (_settingsHandler != null)
            Settings.PropertyChanged -= _settingsHandler;
        _settingsHandler = (o, args) => OnSettingsChanged?.Invoke(o, args);
        Settings.PropertyChanged += _settingsHandler;
    }

    public void LoadSettings()
    {
        if (!File.Exists("./Settings.json"))
        {
            _logger.LogInformation("配置文件不存在，跳过加载: {Path}", Path.GetFullPath("./Settings.json"));
            return;
        }
        var json = File.ReadAllText("./Settings.json");
        var r = JsonSerializer.Deserialize<Settings>(json);
        if (r != null)
        {
            if (r.HomeworkTemplate == null)
                r.HomeworkTemplate = new HomeworkTemplateConfig();
            HomeworkTemplateConfig.Normalize(r.HomeworkTemplate);
            Settings = r;
            _logger.LogInformation("加载配置文件: {Path}", Path.GetFullPath("./Settings.json"));
        }
        else
        {
            _logger.LogWarning("配置文件反序列化失败，使用默认设置");
        }
    }

    public void SaveSettings()
    {
        var path = Path.GetFullPath("./Settings.json");
        File.WriteAllText(path, JsonSerializer.Serialize<Settings>(Settings));
        _logger.LogInformation("写入配置文件: {Path} 变更属性: {Property}", path, _lastChangedProperty ?? nameof(Settings));
    }

    public event PropertyChangedEventHandler? OnSettingsChanged;

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings))
            SubscribeSettings();
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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