using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace StickyHomeworks.Services;

/// <summary>
/// KnotLink 独立式节点自注册（清单内嵌代码 + 动态释放 + 退出清理）。
/// 参照 knotlink-self-registration-pattern：
///   启动时把内嵌的 standalone_manifest.json / FuncList.json 释放到
///   %LOCALAPPDATA%\KnotLink\&lt;AppID&gt;\ 并写入注册表，真正退出时清理。
/// 注意：应用有托盘图标，最小化到托盘不算退出，因此清理挂在
/// Application.Current.Exit（真正退出才触发），不要挂在窗口关闭上。
/// </summary>
public static class KnotLinkRegistration
{
    /// <summary>节点 AppID，发布后不可修改，代码/FuncList/manifest 三处保持一致。</summary>
    public const string AppId = "com.github.stickyhomeworks2.stickyhomeworks2";

    private const string RegistryRootKey = @"Software\KnotLink\StandaloneNodes";

    // 与 StickyHomeworks2.csproj 中 EmbeddedResource 的 LogicalName 对应
    private const string ManifestResourceName = "KnotLink.standalone_manifest.json";
    private const string FuncListResourceName = "KnotLink.FuncList.json";

    private static readonly string ManifestDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KnotLink", AppId);

    /// <summary>清单释放目录（%LOCALAPPDATA%\KnotLink\&lt;AppID&gt;\）。</summary>
    public static string ManifestDirectory => ManifestDir;

    /// <summary>
    /// 启动时调用：释放内嵌清单 + 写入注册表，让 KnotHub 发现本节点。
    /// 每次启动覆盖写入，确保清单与当前版本一致。失败不抛异常，不影响主程序启动。
    /// </summary>
    public static void Register(Action<string>? log = null)
    {
        try
        {
            Directory.CreateDirectory(ManifestDir);
            WriteEmbeddedResource(ManifestResourceName, Path.Combine(ManifestDir, "standalone_manifest.json"));
            WriteEmbeddedResource(FuncListResourceName, Path.Combine(ManifestDir, "FuncList.json"));

            using var key = Registry.CurrentUser.CreateSubKey(RegistryRootKey);
            key?.SetValue(AppId, ManifestDir, RegistryValueKind.String);
            log?.Invoke($"KnotLink 自注册完成: {AppId} -> {ManifestDir}");
        }
        catch (Exception ex)
        {
            log?.Invoke($"KnotLink 自注册失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 真正退出时调用：删除注册表值 + 清理清单目录。失败不抛异常。
    /// </summary>
    public static void Unregister(Action<string>? log = null)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRootKey, writable: true);
            key?.DeleteValue(AppId, throwOnMissingValue: false);
            if (Directory.Exists(ManifestDir))
                Directory.Delete(ManifestDir, recursive: true);
            log?.Invoke($"KnotLink 自注册已清理: {AppId}");
        }
        catch (Exception ex)
        {
            log?.Invoke($"KnotLink 自注册清理失败: {ex.Message}");
        }
    }

    private static void WriteEmbeddedResource(string resourceName, string destPath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"缺少内嵌资源: {resourceName}");
        using var file = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.CopyTo(file);
    }
}
