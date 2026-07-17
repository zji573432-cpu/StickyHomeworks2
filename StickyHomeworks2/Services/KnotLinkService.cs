using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using KnotLink;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StickyHomeworks.Models;

namespace StickyHomeworks.Services;

public class KnotLinkService : IHostedService
{
    private readonly ProfileService _profileService;
    private readonly SettingsService _settingsService;
    private readonly ILogger<KnotLinkService> _logger;
    private readonly SemaphoreSlim _profileLock = new(1, 1);
    private readonly SemaphoreSlim _settingsLock = new(1, 1);

    private OpenSocketResponser? _responser;
    private CancellationTokenSource? _cts;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public KnotLinkService(
        ProfileService profileService,
        SettingsService settingsService,
        ILogger<KnotLinkService> logger)
    {
        _profileService = profileService;
        _settingsService = settingsService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = Task.Run(() => RunAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在停止 KnotLink 服务...");
        _cts?.Cancel();
        _responser?.Dispose();
        _responser = null;
        return Task.CompletedTask;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("正在连接 KnotLink 服务 (127.0.0.1:6378)...");
                _responser = new OpenSocketResponser("com.stickyhomeworks2", "homework");
                _responser.OnQuestionAsync = HandleRequestAsync;
                _logger.LogInformation(
                    "KnotLink OpenSocketResponser 已注册 (appid=com.stickyhomeworks2, opensocketid=homework)");

                // 阻塞等待取消信号，保持后台线程存活
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                await using var registration = ct.Register(() => tcs.TrySetResult(true));
                await tcs.Task;

                _logger.LogInformation("KnotLink 服务收到取消信号，正在退出...");
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "KnotLink 连接失败，5 秒后重试...");
                try { _responser?.Dispose(); } catch { /* ignore */ }
                _responser = null;
                try { await Task.Delay(5000, ct); } catch (OperationCanceledException) { break; }
            }
        }
    }

    /// <summary>
    /// 处理来自 KnotLink 的请求（运行在 TcpClient 后台线程）。
    /// </summary>
    private async Task<string> HandleRequestAsync(string data)
    {
        _logger.LogInformation("KnotLink 收到请求: {Data}", data);

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
            _logger.LogError(ex, "KnotLink 处理请求失败");
            return $"status=err;message={ex.Message}";
        }
    }

    /// <summary>
    /// list-homeworks: 读取 Profile.json，返回全部作业
    /// </summary>
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
        _logger.LogInformation("KnotLink list-homeworks: 返回 {Count} 条作业", homeworkList.Count);

        var resp = new KLKVMap
        {
            ["status"] = "ok",
            ["count"] = homeworkList.Count.ToString(),
            ["homeworks"] = json
        };
        return resp.Serialize();
    }

    /// <summary>
    /// add-homework: 新建作业，写入 Profile.json
    /// 入参: subject;content;dueDate;tags (tags 用逗号分隔)
    /// </summary>
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

        var resp = new KLKVMap
        {
            ["status"] = "ok",
            ["id"] = homework.Id.ToString()
        };
        return resp.Serialize();
    }

    /// <summary>
    /// edit-homework: 修改指定作业
    /// 入参: id;subject;content;dueDate;tags
    /// </summary>
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

                if (!string.IsNullOrWhiteSpace(subject))
                    homework.Subject = subject;
                if (!string.IsNullOrWhiteSpace(content))
                    homework.Content = content;
                if (!string.IsNullOrWhiteSpace(dueDateStr) && DateTime.TryParse(dueDateStr, out var dt))
                    homework.DueTime = dt;
                if (!string.IsNullOrWhiteSpace(tagsStr))
                {
                    homework.Tags = new ObservableCollection<string>(
                        tagsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                }
            });

            _profileService.SaveProfile();
            _logger.LogInformation("KnotLink edit-homework: id={Id}", id);
        }
        finally
        {
            _profileLock.Release();
        }

        var resp = new KLKVMap { ["status"] = "ok" };
        return resp.Serialize();
    }

    /// <summary>
    /// delete-homework: 删除指定作业
    /// 入参: id
    /// </summary>
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

        var resp = new KLKVMap { ["status"] = "ok" };
        return resp.Serialize();
    }

    /// <summary>
    /// list-subjects: 返回 Settings.json 中的科目列表
    /// </summary>
    private async Task<string> HandleListSubjectsAsync()
    {
        await _settingsLock.WaitAsync();
        List<string> snapshot;
        try
        {
            snapshot = _settingsService.Settings.Subjects.ToList();
        }
        finally
        {
            _settingsLock.Release();
        }

        var subjects = string.Join(",", snapshot);
        _logger.LogInformation("KnotLink list-subjects: {Count} 个科目", snapshot.Count);

        var resp = new KLKVMap
        {
            ["status"] = "ok",
            ["subjects"] = subjects
        };
        return resp.Serialize();
    }

    /// <summary>
    /// manage-subjects: 管理科目（增/改/删）
    /// 入参: op=add|edit|delete;name=xxx;newname=xxx
    ///   - add: op=add;name=语文
    ///   - edit: op=edit;name=语文;newname=数学
    ///   - delete: op=delete;name=语文
    /// </summary>
    private async Task<string> HandleManageSubjectsAsync(KLKVMap kv)
    {
        var op = kv.Get("op");
        var name = kv.Get("name");

        if (string.IsNullOrWhiteSpace(op))
            return "status=err;message=op is required";
        if (string.IsNullOrWhiteSpace(name))
            return "status=err;message=name is required";

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
                            _logger.LogInformation("KnotLink manage-subjects: 添加科目 {Name}", name);
                        }
                        else
                        {
                            _logger.LogWarning("KnotLink manage-subjects: 科目 {Name} 已存在", name);
                        }
                        break;

                    case "edit":
                    {
                        var newName = kv.Get("newname");
                        if (string.IsNullOrWhiteSpace(newName))
                            throw new InvalidOperationException("newname is required for edit operation");

                        var idx = subjects.IndexOf(name);
                        if (idx >= 0)
                        {
                            subjects[idx] = newName;
                            _logger.LogInformation("KnotLink manage-subjects: 重命名科目 {Old} -> {New}", name, newName);
                        }
                        else
                        {
                            _logger.LogWarning("KnotLink manage-subjects: 未找到科目 {Name}", name);
                        }
                        break;
                    }

                    case "delete":
                        if (!subjects.Remove(name))
                        {
                            _logger.LogWarning("KnotLink manage-subjects: 未找到科目 {Name}", name);
                        }
                        else
                        {
                            _logger.LogInformation("KnotLink manage-subjects: 删除科目 {Name}", name);
                        }
                        break;

                    default:
                        throw new InvalidOperationException($"unknown op: {op}");
                }
            });

            _settingsService.SaveSettings();
        }
        finally
        {
            _settingsLock.Release();
        }

        var resp = new KLKVMap { ["status"] = "ok" };
        return resp.Serialize();
    }
}
