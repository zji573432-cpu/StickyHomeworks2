using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ElysiaFramework;
using Glycoprotein;
using Glycoprotein.Glycosylation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StickyHomeworks.Models;

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

    public record WindowPositionArgs(
        [property:Display(Name = "横坐标", Description = "窗口左上角横坐标 (物理像素)")]double X, 
        [property:Display(Name = "纵坐标", Description = "窗口左上角纵坐标 (物理像素)")]double Y);

    public record WindowSizeArgs(
        [property:Display(Name = "宽度", Description = "窗口宽度 (物理像素)")]double Width, 
        [property:Display(Name = "高度", Description = "窗口高度 (物理像素)")]double Height);

    public record SetTitleArgs(
        [property:Display(Name = "标题", Description = "目标标题")]string Title);

    public record SetTopmostArgs(
        [property:Description("置顶")]bool Topmost);

    public record WindowStateResponse(
        bool Visible, bool Topmost, string Title,
        double X, double Y, double Width, double Height);

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
            
            RegisterControlFields(node);
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

    private void RegisterControlFields(GlycoComplex node)
    {
        node.AddAction(new Field.Method {
            Id = "window.hide",
            FriendlyName = "隐藏窗口",
            Description = "隐藏 SH2 主窗口"
        }, HideMainWindow);

        node.AddAction(new Field.Method {
            Id = "window.show",
            FriendlyName = "显示窗口",
            Description = "显示并激活 SH2 主窗口"
        }, ShowMainWindow);

        node.AddAction<SetTopmostArgs>(new Field.Method {
            Id = "window.topmost",
            FriendlyName = "设置窗口置顶",
            Description = "true=置顶, false=取消置顶"
        }, SetTopmost);

        node.AddAction<WindowPositionArgs>(new Field.Method {
            Id = "window.position",
            FriendlyName = "设置窗口位置",
            Description = "按物理像素设置主窗口位置 (与设置中的 WindowX/WindowY 一致)"
        }, SetWindowPosition);

        node.AddAction<WindowSizeArgs>(new Field.Method {
            Id = "window.size",
            FriendlyName = "设置窗口大小",
            Description = "按物理像素设置主窗口大小 (与设置中的 WindowWidth/WindowHeight 一致)"
        }, SetWindowSize);

        node.AddAction<SetTitleArgs>(new Field.Method {
            Id = "window.title",
            FriendlyName = "设置窗口标题",
            Description = "修改主窗口标题"
        }, SetWindowTitle);

        node.AddFunction(new Field.Method {
            Id = "window.getState",
            FriendlyName = "获取窗口状态",
            Description = "返回主窗口可见性/置顶/标题/位置大小 (物理像素)"
        }, GetWindowState);
    }

    private void HideMainWindow() => RunOnUi(() => {
        _settingsService.Settings.IsMainWindowVisible = false;
        AppEx.GetService<MainWindow>().Hide();
        _logger.LogInformation("Glycoprotein: 已隐藏主窗口");
    });

    private void ShowMainWindow() => RunOnUi(() => {
        _settingsService.Settings.IsMainWindowVisible = true;
        var win = AppEx.GetService<MainWindow>();
        win.Show();
        win.Activate();
        _logger.LogInformation("Glycoprotein: 已显示主窗口");
    });

    private void SetTopmost(SetTopmostArgs args) => RunOnUi(() => {
        _settingsService.Settings.IsMainWindowTopmost = args.Topmost;
        _logger.LogInformation("Glycoprotein: 窗口置顶 = {Topmost}", args.Topmost);
    });

    private void SetWindowPosition(WindowPositionArgs args) => RunOnUi(() => {
        var s = _settingsService.Settings;
        s.WindowX = args.X;
        s.WindowY = args.Y;
        AppEx.GetService<MainWindow>().SetPos();
        _logger.LogInformation("Glycoprotein: 窗口位置 = ({X}, {Y})", args.X, args.Y);
    });

    private void SetWindowSize(WindowSizeArgs args) => RunOnUi(() => {
        var s = _settingsService.Settings;
        s.WindowWidth = args.Width;
        s.WindowHeight = args.Height;
        AppEx.GetService<MainWindow>().SetPos();
        _logger.LogInformation("Glycoprotein: 窗口大小 = {Width}x{Height}", args.Width, args.Height);
    });

    private void SetWindowTitle(SetTitleArgs args) => RunOnUi(() => {
        _settingsService.Settings.Title = args.Title;
        _logger.LogInformation("Glycoprotein: 窗口标题 = [{Title}]", args.Title);
    });

    private WindowStateResponse GetWindowState() {
        var s = _settingsService.Settings;
        var (visible, topmost) = RunOnUi(() => {
            var win = AppEx.GetService<MainWindow>();
            return (win.IsVisible, win.Topmost);
        });
        return new WindowStateResponse(visible, topmost, s.Title, s.WindowX, s.WindowY, s.WindowWidth, s.WindowHeight);
    }

    private static void RunOnUi(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.Invoke(action);
        }
    }

    private static T RunOnUi<T>(Func<T> func)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            return func();
        }
        return dispatcher.Invoke(func);
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
