using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ElysiaFramework;
using Glycoprotein;
using Glycoprotein.Glycosylation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StickyHomeworks.Models;
using StickyHomeworks2.Services;

namespace StickyHomeworks.Services;

public class GlycoproteinIpcService : IHostedService, INotifyPropertyChanged
{
    public const string HomeworkChangedEventId = "homework.changed";

    private readonly SettingsService _settingsService;
    private readonly ProfileService _profileService;
    private readonly ILogger<GlycoproteinIpcService> _logger;
    private GlycoComplex? _node;
    private bool _isRunning;

    public event PropertyChangedEventHandler? PropertyChanged;

    public record PingRespond(string Message);

    public record HomeworkChangedPayload(int HomeworkCount, string Timestamp);

    public GlycoproteinIpcService(SettingsService settingsService, ProfileService profileService, ILogger<GlycoproteinIpcService> logger)
    {
        _settingsService = settingsService;
        _profileService = profileService;
        _logger = logger;
        _settingsService.OnSettingsChanged += OnSettingsChanged;
        _profileService.ProfileSaved += OnProfileSaved;
    }

    public GlycoComplex? Node => _node;

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (value == _isRunning) return;
            _isRunning = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_settingsService.Settings.IsGlycoproteinEnabled)
        {
            _ = StartNodeAsync();
        }
        else
        {
            _logger.LogInformation("Glycoprotein 服务未启用");
        }
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await StopNodeAsync();
        _settingsService.OnSettingsChanged -= OnSettingsChanged;
        _profileService.ProfileSaved -= OnProfileSaved;
    }

    private async void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        try
        {
            if (e.PropertyName == nameof(Settings.IsGlycoproteinEnabled))
            {
                if (_settingsService.Settings.IsGlycoproteinEnabled)
                {
                    await StartNodeAsync();
                }
                else
                {
                    await StopNodeAsync();
                }
            }
            else if (e.PropertyName == nameof(Settings.GlycoproteinNodeId) && _node != null)
            {
                // Gid 变更: 运行中的节点以新 Gid 重建
                var newId = _settingsService.Settings.GlycoproteinNodeId?.Trim();
                if (string.IsNullOrWhiteSpace(newId))
                {
                    _logger.LogWarning("Glycoprotein 节点 ID 不能为空, 保持当前 Gid [{NodeId}]", _node.Id);
                    return;
                }
                if (newId == _node.Id) return;
                _logger.LogInformation("Glycoprotein 节点 ID 变更: [{OldNodeId}] → [{NewNodeId}], 正在重建节点", _node.Id, newId);
                await StopNodeAsync();
                await StartNodeAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "响应 Glycoprotein 设置变更失败");
        }
    }

    public async Task StartNodeAsync()
    {
        if (_node != null) return;
        if (!_settingsService.Settings.IsGlycoproteinEnabled) return;
        try
        {
            var nodeId = _settingsService.Settings.GlycoproteinNodeId?.Trim();
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                _settingsService.Settings.EnsureGlycoproteinNodeId();
                nodeId = _settingsService.Settings.GlycoproteinNodeId;
            }
            var node = new GlycoComplex(nodeId)
            {
                Vendor = $"StickyHomeworks2 v{App.AppVersion}"
            };
            node.OnDiscovered += beacon =>
                _logger.LogInformation("发现 Glycoprotein 节点: [{NodeId}] Vendor=[{Vendor}] 字段数: {Count}",
                    beacon.Id, beacon.Vendor, beacon.Fields.Count);
            node.OnChanged += beacon =>
                _logger.LogTrace("Glycoprotein 节点变更: [{NodeId}] 字段数: {Count}", beacon.Id, beacon.Fields.Count);
            node.OnExpired += beacon =>
                _logger.LogInformation("Glycoprotein 节点过期: [{NodeId}]", beacon.Id);

            #if DEBUG
            node.AddFunction(new Field.Method {
                Id = "ping",
                FriendlyName = "Ping",
                Description = "测试连通性"
            }, () => {
                _logger.LogInformation("收到 Glycoprotein ping 请求");
                return new PingRespond("Pong!");
            });
            #endif
            
            WindowGlycoActions.Register(node, _settingsService, _logger);
            node.AddEvent(new Field.Event {
                Id = HomeworkChangedEventId,
                FriendlyName = "作业数据变更",
                Description = "作业数据发生变更时广播"
            });

            _node = node;
            await node.StartAsync();
            IsRunning = true;
            _logger.LogInformation("Glycoprotein 节点已启动, Gid=[{NodeId}]", nodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动 Glycoprotein 节点失败");
            _node?.Dispose();
            _node = null;
            IsRunning = false;
        }
    }

    public async Task StopNodeAsync()
    {
        var node = _node;
        if (node == null) return;
        _node = null;
        IsRunning = false;
        node.Dispose();
        await Task.CompletedTask;
        _logger.LogInformation("Glycoprotein 节点已停止");
    }

    private async void OnProfileSaved(object? sender, EventArgs e)
    {
        var node = _node;
        if (node == null) return;
        try
        {
            await node.EmitEventAsync(HomeworkChangedEventId, new HomeworkChangedPayload(
                _profileService.Profile.Homeworks.Count,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            _logger.LogInformation("已广播 {EventId} 事件 (作业数: {Count})", HomeworkChangedEventId, _profileService.Profile.Homeworks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "广播 {EventId} 事件失败", HomeworkChangedEventId);
        }
    }
}
