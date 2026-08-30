using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using KnotLink;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StickyHomeworks.Models;

namespace StickyHomeworks.Services;

public class KnotLinkService : ObservableRecipient, IHostedService
{
    private const string AppIdConst = "com.stickyhomeworks2";
    private const string HomeworkSocketId = "homework";
    private const string ControlSocketId = "control";

    private readonly ProfileService _profileService;
    private readonly SettingsService _settingsService;
    private readonly MainWindow _mainWindow;
    private readonly ILogger<KnotLinkService> _logger;
    private readonly SemaphoreSlim _profileLock = new(1, 1);
    private readonly SemaphoreSlim _settingsLock = new(1, 1);

    private OpenSocketResponser? _homeworkResponser;
    private OpenSocketResponser? _controlResponser;
    private CancellationTokenSource? _cts;
    private bool _isHomeworkConnected;
    private bool _isControlConnected;
    private string _homeworkStatusText = "未连接";
    private string _controlStatusText = "未连接";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public string AppId => AppIdConst;

    public bool IsHomeworkConnected
    {
        get => _isHomeworkConnected;
        private set
        {
            if (value == _isHomeworkConnected) return;
            _isHomeworkConnected = value;
            OnPropertyChanged();
        }
    }

    public bool IsControlConnected
    {
        get => _isControlConnected;
        private set
        {
            if (value == _isControlConnected) return;
            _isControlConnected = value;
            OnPropertyChanged();
        }
    }

    public string HomeworkStatusText
    {
        get => _homeworkStatusText;
        private set
        {
            if (value == _homeworkStatusText) return;
            _homeworkStatusText = value;
            OnPropertyChanged();
        }
    }

    public string ControlStatusText
    {
        get => _controlStatusText;
        private set
        {
            if (value == _controlStatusText) return;
            _controlStatusText = value;
            OnPropertyChanged();
        }
    }

    public KnotLinkService(
        ProfileService profileService,
        SettingsService settingsService,
        MainWindow mainWindow,
        ILogger<KnotLinkService> logger)
    {
        _profileService = profileService;
        _settingsService = settingsService;
        _mainWindow = mainWindow;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // 监听总开关变化，动态启停
        _settingsService.Settings.PropertyChanged += OnKnotLinkSettingChanged;

        if (_settingsService.Settings.IsKnotLinkEnabled)
        {
            StartAllResponsers();
        }

        return Task.CompletedTask;
    }

    private void OnKnotLinkSettingChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.IsKnotLinkEnabled))
        {
            if (_settingsService.Settings.IsKnotLinkEnabled)
                StartAllResponsers();
            else
                StopAllResponsers();
        }
    }

    private void StartAllResponsers()
    {
        StopAllResponsers();

        // 为每个接口创建独立的 CTS
        if (_cts == null || _cts.IsCancellationRequested)
            return;

        _ = Task.Run(() => RunResponserAsync(HomeworkSocketId,
            r => _homeworkResponser = r,
            connected => IsHomeworkConnected = connected,
            status => HomeworkStatusText = status,
            HandleHomeworkRequestAsync,
            () => _settingsService.Settings.IsKnotLinkHomeworkEnabled,
            _cts.Token), _cts.Token);

        _ = Task.Run(() => RunResponserAsync(ControlSocketId,
            r => _controlResponser = r,
            connected => IsControlConnected = connected,
            status => ControlStatusText = status,
            HandleControlRequestAsync,
            () => _settingsService.Settings.IsKnotLinkControlEnabled,
            _cts.Token), _cts.Token);
    }

    private void StopAllResponsers()
    {
        try { _homeworkResponser?.Dispose(); } catch { }
        try { _controlResponser?.Dispose(); } catch { }
        _homeworkResponser = null;
        _controlResponser = null;
        IsHomeworkConnected = false;
        IsControlConnected = false;
        HomeworkStatusText = "未连接";
        ControlStatusText = "未连接";
    }

    private async Task RunResponserAsync(
        string socketId,
        Action<OpenSocketResponser?> setResponser,
        Action<bool> setConnected,
        Action<string> setStatus,
        Func<string, Task<string>> handler,
        Func<bool> isEnabled,
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // 检查分开关
            if (!isEnabled())
            {
                setStatus("已停用");
                setConnected(false);
                try { await Task.Delay(3000, ct); } catch (OperationCanceledException) { break; }
                continue;
            }

            OpenSocketResponser? r = null;
            try
            {
                setStatus("正在连接...");
                _logger.LogInformation("正在连接 KnotLink ({SocketId}) ...", socketId);
                r = new OpenSocketResponser(AppIdConst, socketId);
                r.OnQuestionAsync = handler;
                setResponser(r);
                setConnected(true);
                setStatus("已连接");
                _logger.LogInformation("KnotLink OpenSocketResponser 已注册 (appid={AppId}, opensocketid={SocketId})",
                    AppIdConst, socketId);

                // 阻塞等待取消信号
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                await using var registration = ct.Register(() => tcs.TrySetResult(true));
                await tcs.Task;

                _logger.LogInformation("KnotLink ({SocketId}) 收到取消信号，正在退出...", socketId);
                setConnected(false);
                break;
            }
            catch (OperationCanceledException)
            {
                setConnected(false);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "KnotLink ({SocketId}) 连接失败，5 秒后重试...", socketId);
                setConnected(false);
                setStatus($"连接失败: {ex.Message}");
                try { r?.Dispose(); } catch { }
                setResponser(null);
                try { await Task.Delay(5000, ct); } catch (OperationCanceledException) { break; }
            }
        }

        setConnected(false);
        setStatus("未连接");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在停止 KnotLink 服务...");
        _settingsService.Settings.PropertyChanged -= OnKnotLinkSettingChanged;
        _cts?.Cancel();
        StopAllResponsers();
        _cts?.Dispose();
        return Task.CompletedTask;
    }

    // ==================== homework 接口 ====================

    private async Task<string> HandleHomeworkRequestAsync(string data)
    {
        if (!_settingsService.Settings.IsKnotLinkHomeworkEnabled)
            return "status=err;message=homework opensocket disabled";

        _logger.LogInformation("KnotLink [homework] 收到请求: {Data}", data);

        try
        {
            var kv = new KLKVMap();
            kv.Deserialize(data);
            var action = kv.Get("action");

            return action switch
            {
                "list-homeworks" => await HandleListHomeworksAsync(),
                "add-homework" => await HandleAddHomeworkAsync(kv),
                "edit-homework" => await HandleEditHomeworkAsync(kv),
                "delete-homework" => await HandleDeleteHomeworkAsync(kv),
                "list-subjects" => await HandleListSubjectsAsync(),
                "manage-subjects" => await HandleManageSubjectsAsync(kv),
                "ping" => "status=pong",
                _ => "status=err;message=unknown action"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KnotLink [homework] 处理请求失败");
            return $"status=err;message={ex.Message}";
        }
    }

    // ==================== control 接口 ====================

    private async Task<string> HandleControlRequestAsync(string data)
    {
        if (!_settingsService.Settings.IsKnotLinkControlEnabled)
            return "status=err;message=control opensocket disabled";

        _logger.LogInformation("KnotLink [control] 收到请求: {Data}", data);

        try
        {
            var kv = new KLKVMap();
            kv.Deserialize(data);
            var action = kv.Get("action");

            return action switch
            {
                "ping" => "status=pong",
                "hide-window" => await HandleHideWindowAsync(),
                "show-window" => await HandleShowWindowAsync(),
                "set-topmost" => await HandleSetTopmostAsync(kv),
                "set-position" => await HandleSetPositionAsync(kv),
                "set-size" => await HandleSetSizeAsync(kv),
                "set-title" => await HandleSetTitleAsync(kv),
                "get-window-state" => await HandleGetWindowStateAsync(),
                _ => "status=err;message=unknown action"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KnotLink [control] 处理请求失败");
            return $"status=err;message={ex.Message}";
        }
    }

    private async Task<string> HandleHideWindowAsync()
    {
        await Application.Current.Dispatcher.InvokeAsync(() => _mainWindow.Hide());
        _logger.LogInformation("KnotLink [control] hide-window");
        return "status=ok";
    }

    private async Task<string> HandleShowWindowAsync()
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _mainWindow.Show();
            _mainWindow.Activate();
        });
        _logger.LogInformation("KnotLink [control] show-window");
        return "status=ok";
    }

    private async Task<string> HandleSetTopmostAsync(KLKVMap kv)
    {
        var value = kv.Get("value");
        var topmost = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        await Application.Current.Dispatcher.InvokeAsync(() => _mainWindow.Topmost = topmost);
        _logger.LogInformation("KnotLink [control] set-topmost: {Value}", topmost);
        return "status=ok";
    }

    private async Task<string> HandleSetPositionAsync(KLKVMap kv)
    {
        var xStr = kv.Get("x");
        var yStr = kv.Get("y");
        if (!double.TryParse(xStr, out var x) || !double.TryParse(yStr, out var y))
            return "status=err;message=invalid x or y";

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _mainWindow.Left = x;
            _mainWindow.Top = y;
        });
        _logger.LogInformation("KnotLink [control] set-position: x={X}, y={Y}", x, y);
        return "status=ok";
    }

    private async Task<string> HandleSetSizeAsync(KLKVMap kv)
    {
        var wStr = kv.Get("width");
        var hStr = kv.Get("height");
        if (!double.TryParse(wStr, out var w) || !double.TryParse(hStr, out var h))
            return "status=err;message=invalid width or height";

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _mainWindow.Width = w;
            _mainWindow.Height = h;
        });
        _logger.LogInformation("KnotLink [control] set-size: width={W}, height={H}", w, h);
        return "status=ok";
    }

    private async Task<string> HandleSetTitleAsync(KLKVMap kv)
    {
        var title = kv.Get("title");
        if (string.IsNullOrWhiteSpace(title))
            return "status=err;message=title is required";

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _settingsService.Settings.Title = title;
        });
        _settingsService.SaveSettings();
        _logger.LogInformation("KnotLink [control] set-title: {Title}", title);
        return "status=ok";
    }

    private async Task<string> HandleGetWindowStateAsync()
    {
        var result = await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var kv = new KLKVMap
            {
                ["status"] = "ok",
                ["visible"] = _mainWindow.IsVisible ? "true" : "false",
                ["x"] = ((int)_mainWindow.Left).ToString(),
                ["y"] = ((int)_mainWindow.Top).ToString(),
                ["width"] = ((int)_mainWindow.Width).ToString(),
                ["height"] = ((int)_mainWindow.Height).ToString(),
                ["topmost"] = _mainWindow.Topmost ? "true" : "false",
                ["title"] = _mainWindow.Title
            };
            return kv.Serialize();
        });

        _logger.LogInformation("KnotLink [control] get-window-state");
        return result;
    }

    // ==================== homework handlers ====================

    private async Task<string> HandleListHomeworksAsync()
    {
        await _profileLock.WaitAsync();
        List<Homework> snapshot;
        try
        {
            snapshot = _profileService.Profile.Homeworks.ToList();
        }
        finally
        {
            _profileLock.Release();
        }

        var homeworkList = snapshot.Select(h => new
        {
            id = h.Id.ToString(),
            subject = h.Subject,
            content = h.Content,
            dueDate = h.DueTime.ToString("yyyy-MM-dd"),
            tags = h.Tags.ToList()
        }).ToList();

        var json = JsonSerializer.Serialize(homeworkList, JsonOptions);
        _logger.LogInformation("KnotLink list-homeworks: {Count} 条", homeworkList.Count);

        var resp = new KLKVMap
        {
            ["status"] = "ok",
            ["count"] = homeworkList.Count.ToString(),
            ["homeworks"] = json
        };
        return resp.Serialize();
    }

    private async Task<string> HandleAddHomeworkAsync(KLKVMap kv)
    {
        var subject = kv.Get("subject");
        var content = kv.Get("content");
        var dueDateStr = kv.Get("dueDate");
        var tagsStr = kv.Get("tags");

        if (string.IsNullOrWhiteSpace(subject))
            return "status=err;message=subject is required";

        var dueDate = DateTime.TryParse(dueDateStr, out var dt) ? dt : DateTime.Today;

        var homework = new Homework
        {
            Id = Guid.NewGuid(),
            Subject = subject,
            Content = content,
            DueTime = dueDate,
            Tags = new ObservableCollection<string>(
                (tagsStr ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        };

        await _profileLock.WaitAsync();
        try
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _profileService.Profile.Homeworks.Add(homework);
            });
            _profileService.SaveProfile();
        }
        finally
        {
            _profileLock.Release();
        }

        _logger.LogInformation("KnotLink add-homework: id={Id}, subject={Subject}", homework.Id, subject);

        var resp = new KLKVMap { ["status"] = "ok", ["id"] = homework.Id.ToString() };
        return resp.Serialize();
    }

    private async Task<string> HandleEditHomeworkAsync(KLKVMap kv)
    {
        var idStr = kv.Get("id");
        if (!Guid.TryParse(idStr, out var id))
            return "status=err;message=invalid id";

        await _profileLock.WaitAsync();
        try
        {
            var homework = _profileService.Profile.Homeworks.FirstOrDefault(h => h.Id == id);
            if (homework == null)
                return "status=err;message=homework not found";

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var subject = kv.Get("subject");
                var content = kv.Get("content");
                var dueDateStr = kv.Get("dueDate");
                var tagsStr = kv.Get("tags");

                if (!string.IsNullOrWhiteSpace(subject)) homework.Subject = subject;
                if (!string.IsNullOrWhiteSpace(content)) homework.Content = content;
                if (!string.IsNullOrWhiteSpace(dueDateStr) && DateTime.TryParse(dueDateStr, out var dt))
                    homework.DueTime = dt;
                if (!string.IsNullOrWhiteSpace(tagsStr))
                    homework.Tags = new ObservableCollection<string>(
                        tagsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            });

            _profileService.SaveProfile();
            _logger.LogInformation("KnotLink edit-homework: id={Id}", id);
        }
        finally
        {
            _profileLock.Release();
        }

        return "status=ok";
    }

    private async Task<string> HandleDeleteHomeworkAsync(KLKVMap kv)
    {
        var idStr = kv.Get("id");
        if (!Guid.TryParse(idStr, out var id))
            return "status=err;message=invalid id";

        await _profileLock.WaitAsync();
        try
        {
            var homework = _profileService.Profile.Homeworks.FirstOrDefault(h => h.Id == id);
            if (homework == null)
                return "status=err;message=homework not found";

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _profileService.Profile.Homeworks.Remove(homework);
            });

            _profileService.SaveProfile();
            _logger.LogInformation("KnotLink delete-homework: id={Id}", id);
        }
        finally
        {
            _profileLock.Release();
        }

        return "status=ok";
    }

    private async Task<string> HandleListSubjectsAsync()
    {
        await _settingsLock.WaitAsync();
        List<string> snapshot;
        try { snapshot = _settingsService.Settings.Subjects.ToList(); }
        finally { _settingsLock.Release(); }

        var subjects = string.Join(",", snapshot);
        _logger.LogInformation("KnotLink list-subjects: {Count} 个", snapshot.Count);

        var resp = new KLKVMap { ["status"] = "ok", ["subjects"] = subjects };
        return resp.Serialize();
    }

    private async Task<string> HandleManageSubjectsAsync(KLKVMap kv)
    {
        var op = kv.Get("op");
        var name = kv.Get("name");

        if (string.IsNullOrWhiteSpace(op)) return "status=err;message=op is required";
        if (string.IsNullOrWhiteSpace(name)) return "status=err;message=name is required";

        await _settingsLock.WaitAsync();
        try
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var subjects = _settingsService.Settings.Subjects;

                switch (op)
                {
                    case "add":
                        if (!subjects.Contains(name, StringComparer.OrdinalIgnoreCase))
                        {
                            subjects.Add(name);
                            _logger.LogInformation("KnotLink manage-subjects: add {Name}", name);
                        }
                        break;
                    case "edit":
                    {
                        var newName = kv.Get("newname");
                        if (string.IsNullOrWhiteSpace(newName))
                            throw new InvalidOperationException("newname is required for edit");
                        var idx = subjects.IndexOf(name);
                        if (idx >= 0) subjects[idx] = newName;
                        break;
                    }
                    case "delete":
                        subjects.Remove(name);
                        break;
                    default:
                        throw new InvalidOperationException($"unknown op: {op}");
                }
            });
            _settingsService.SaveSettings();
        }
        finally { _settingsLock.Release(); }

        return "status=ok";
    }
}
