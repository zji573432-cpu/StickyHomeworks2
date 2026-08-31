using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ElysiaFramework;
using Glycoprotein;
using Glycoprotein.Glycosylation;
using Microsoft.Extensions.Logging;
using StickyHomeworks;
using StickyHomeworks.Services;

namespace StickyHomeworks2.Services;

public static class WindowGlycoActions {

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

    public static void Register(GlycoComplex node, SettingsService settingsService, ILogger logger) {
        node.AddAction(new Field.Method {
            Id = "window.hide",
            FriendlyName = "隐藏窗口",
            Description = "隐藏 SH2 主窗口"
        }, () => HideMainWindow(settingsService, logger));

        node.AddAction(new Field.Method {
            Id = "window.show",
            FriendlyName = "显示窗口",
            Description = "显示并激活 SH2 主窗口"
        }, () => ShowMainWindow(settingsService, logger));

        node.AddAction<SetTopmostArgs>(new Field.Method {
            Id = "window.topmost",
            FriendlyName = "设置窗口置顶",
            Description = "true=置顶, false=取消置顶"
        }, args => SetTopmost(args, settingsService, logger));

        node.AddAction<WindowPositionArgs>(new Field.Method {
            Id = "window.position",
            FriendlyName = "设置窗口位置",
            Description = "按物理像素设置主窗口位置 (与设置中的 WindowX/WindowY 一致)"
        }, args => SetWindowPosition(args, settingsService, logger));

        node.AddAction<WindowSizeArgs>(new Field.Method {
            Id = "window.size",
            FriendlyName = "设置窗口大小",
            Description = "按物理像素设置主窗口大小 (与设置中的 WindowWidth/WindowHeight 一致)"
        }, args => SetWindowSize(args, settingsService, logger));

        node.AddAction<SetTitleArgs>(new Field.Method {
            Id = "window.title",
            FriendlyName = "设置窗口标题",
            Description = "修改主窗口标题"
        }, args => SetWindowTitle(args, settingsService, logger));

        node.AddFunction(new Field.Method {
            Id = "window.getState",
            FriendlyName = "获取窗口状态",
            Description = "返回主窗口可见性/置顶/标题/位置大小 (物理像素)"
        }, () => GetWindowState(settingsService));
    }

    private static void HideMainWindow(SettingsService settingsService, ILogger logger) => RunOnUi(() => {
        settingsService.Settings.IsMainWindowVisible = false;
        AppEx.GetService<MainWindow>().Hide();
        logger.LogInformation("Glycoprotein: 已隐藏主窗口");
    });

    private static void ShowMainWindow(SettingsService settingsService, ILogger logger) => RunOnUi(() => {
        settingsService.Settings.IsMainWindowVisible = true;
        var win = AppEx.GetService<MainWindow>();
        win.Show();
        win.Activate();
        logger.LogInformation("Glycoprotein: 已显示主窗口");
    });

    private static void SetTopmost(SetTopmostArgs args, SettingsService settingsService, ILogger logger) => RunOnUi(() => {
        settingsService.Settings.IsMainWindowTopmost = args.Topmost;
        logger.LogInformation("Glycoprotein: 窗口置顶 = {Topmost}", args.Topmost);
    });

    private static void SetWindowPosition(WindowPositionArgs args, SettingsService settingsService, ILogger logger) => RunOnUi(() => {
        var s = settingsService.Settings;
        s.WindowX = args.X;
        s.WindowY = args.Y;
        AppEx.GetService<MainWindow>().SetPos();
        logger.LogInformation("Glycoprotein: 窗口位置 = ({X}, {Y})", args.X, args.Y);
    });

    private static void SetWindowSize(WindowSizeArgs args, SettingsService settingsService, ILogger logger) => RunOnUi(() => {
        var s = settingsService.Settings;
        s.WindowWidth = args.Width;
        s.WindowHeight = args.Height;
        AppEx.GetService<MainWindow>().SetPos();
        logger.LogInformation("Glycoprotein: 窗口大小 = {Width}x{Height}", args.Width, args.Height);
    });

    private static void SetWindowTitle(SetTitleArgs args, SettingsService settingsService, ILogger logger) => RunOnUi(() => {
        settingsService.Settings.Title = args.Title;
        logger.LogInformation("Glycoprotein: 窗口标题 = [{Title}]", args.Title);
    });

    private static WindowStateResponse GetWindowState(SettingsService settingsService) {
        var s = settingsService.Settings;
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
}
