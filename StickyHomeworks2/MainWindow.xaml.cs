using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Shared.Enums;
using ElysiaFramework;
using MaterialDesignThemes.Wpf;
using Microsoft.Xaml.Behaviors;
using StickyHomeworks.Behaviors;
using StickyHomeworks.Models;
using StickyHomeworks.Services;
using StickyHomeworks.ViewModels;
using StickyHomeworks.Views;
using System.Windows.Automation;
using System.Windows.Forms;
using System.Windows.Threading;
using Stfu.Linq;
using DataFormats = System.Windows.DataFormats;
using DragEventArgs = System.Windows.DragEventArgs;
using H.NotifyIcon;
using Microsoft.Extensions.Logging;

namespace StickyHomeworks;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; set; } = new MainViewModel();

    public ProfileService ProfileService { get; }

    public SettingsService SettingsService { get; }

    public TimeMachineService TimeMachineService { get; }

    public event EventHandler? OnHomeworkEditorUpdated;

    private DispatcherTimer _setBottomTimer;
    private readonly WindowFocusObserverService _focusObserverService;
    private readonly ClassIslandIpcService? _classIslandIpcService;
    private readonly PropertyChangedEventHandler _settingsClassIslandIpcPropertyChanged;
    private readonly ILogger<MainWindow> _logger;

    public MainWindow(ProfileService profileService,
                      SettingsService settingsService,
                      WindowFocusObserverService focusObserverService,
                      ILogger<MainWindow> logger)
    {
        _logger = logger;
        ProfileService = profileService;
        SettingsService = settingsService;
        TimeMachineService = AppEx.GetService<TimeMachineService>();
        _focusObserverService = focusObserverService;
        InitializeComponent();
        _focusObserverService.FocusChanged += FocusObserverServiceOnFocusChanged;
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        ViewModel.PropertyChanging += ViewModelOnPropertyChanging;
        this.StateChanged += OnWindowStateChanged;
        DataContext = this;
        this.TrayIconView.TrayRightMouseUp += TrayIconView_TrayMouseRightClick;

        _classIslandIpcService = AppEx.GetService<ClassIslandIpcService>();
        _settingsClassIslandIpcPropertyChanged = SettingsOnPropertyChangedForClassIslandIpc;
        if (_classIslandIpcService != null)
        {
            _classIslandIpcService.ClassStateChanged += OnClassStateChanged;
            SettingsService.Settings.PropertyChanged += _settingsClassIslandIpcPropertyChanged;
        }
    }

    private void SettingsOnPropertyChangedForClassIslandIpc(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Settings.IsClassIslandIpcEnabled) || _classIslandIpcService == null)
            return;
        _ = SettingsService.Settings.IsClassIslandIpcEnabled
            ? _classIslandIpcService.ConnectAsync()
            : Task.Run(_classIslandIpcService.Disconnect);
    }

    private void OnClassStateChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var ipcService = AppEx.GetService<ClassIslandIpcService>();
            var settings = SettingsService.Settings;
            if (ipcService == null || !settings.IsClassIslandIpcEnabled) return;
            
            // 使用 TimeState 判断是否在上课
            var isInClass = ipcService.CurrentState == TimeState.OnClass;
            var currentSubjectAction = settings.ClassIslandSubjects.FirstOrDefault(s => s.Name == ipcService.CurrentSubjectName);
            var previousSubjectAction = settings.ClassIslandSubjects.FirstOrDefault(s => s.Name == ipcService.PreviousSubjectName);
            
            bool shouldHide;
            if (isInClass)
            {
                // 上课时：检查当前科目是否被监控
                if (currentSubjectAction == null || !currentSubjectAction.IsMonitored)
                {
                    return;
                }
                
                // 0=隐藏, 1=显示, 2=隐藏&下课显示, 3=显示&下课隐藏
                var mode = currentSubjectAction.ActionMode;
                if (mode == 0 || mode == 2) shouldHide = true;
                else if (mode == 1 || mode == 3) shouldHide = false;
                else return;
            }
            else
            {
                // 下课/放学/课间：检查之前科目是否被监控
                if (previousSubjectAction == null || !previousSubjectAction.IsMonitored)
                {
                    return;
                }
                
                // 0=隐藏→下课显示, 1=显示→下课隐藏, 2=隐藏→下课显示, 3=显示→下课隐藏
                var mode = previousSubjectAction.ActionMode;
                if (mode == 0 || mode == 2) shouldHide = false;
                else if (mode == 1 || mode == 3) shouldHide = true;
                else return;
            }
            
            if (shouldHide && settings.IsMainWindowVisible)
            {
                _logger.LogDebug("课程状态变更: 隐藏窗口 (科目={Subject})", ipcService.CurrentSubjectName);
                settings.IsMainWindowVisible = false;
                Hide();
            }
            else if (!shouldHide && !settings.IsMainWindowVisible)
            {
                _logger.LogDebug("课程状态变更: 显示窗口 (科目={Subject})", ipcService.PreviousSubjectName);
                settings.IsMainWindowVisible = true;
                Show();
                Activate();
            }
        });
    }

    private void FocusObserverServiceOnFocusChanged(object? sender, EventArgs e)
    {
        if (!ViewModel.IsDrawerOpened)
            return;
        try
        {
            var hWnd = NativeWindowHelper.GetForegroundWindow();
            NativeWindowHelper.GetWindowThreadProcessId(hWnd, out var id);
            using var proc = Process.GetProcessById(id);
            if (proc.Id != Environment.ProcessId &&
                !new List<string>(["ctfmon", "textinputhost", "chsime"]).Contains(proc.ProcessName.ToLower()))
            {
                Dispatcher.Invoke(() => ExitEditingMode());
            }
        }
        catch
        {
            // ignored
        }
    }

    private void ViewModelOnPropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.SelectedHomework) && !ViewModel.IsUpdatingHomeworkSubject)
        {
            ExitEditingMode(true);
        }
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.SelectedListBoxItem))
        {
            RepositionEditingWindow();
        }
        if (e.PropertyName == nameof(ViewModel.SelectedHomework) && !ViewModel.IsUpdatingHomeworkSubject)
        {
            ExitEditingMode(false);
        }
        if (e.PropertyName == nameof(ViewModel.IsFrozen) && ViewModel.IsFrozen)
        {
            ExitEditingMode(false);
            MainListView.SelectedIndex = -1;
            ViewModel.SelectedHomework = null;
            ViewModel.SelectedListBoxItem = null;
        }
    }
    

    private void ExitEditingMode(bool hard=true)
    {
        if (ViewModel.IsCreatingMode)
        {
            ViewModel.IsCreatingMode = false;
            return;
        }
        if (hard)
            MainListView.SelectedIndex = -1;
        ViewModel.IsDrawerOpened = false;
        AppEx.GetService<HomeworkEditWindow>().TryClose();
        AppEx.GetService<ProfileService>().SaveProfile();
        // 仅在 hard=true 且不在还原状态时才触发备份
        if (hard && !TimeMachineService.IsRestoring)
        {
            TimeMachineService.CreateBackup(MainListView);
        }
        _logger.LogTrace("退出编辑模式: Hard={Hard}", hard);
    }

    public void SetPos()
    {
        GetCurrentDpi(out var dpi, out _);
        Left = SettingsService.Settings.WindowX / dpi;
        Top = SettingsService.Settings.WindowY / dpi;
        Width = SettingsService.Settings.WindowWidth / dpi;
        Height = SettingsService.Settings.WindowHeight / dpi;
    }

    private void SavePos()
    {
        GetCurrentDpi(out var dpi, out _);
        SettingsService.Settings.WindowX = Left * dpi;
        SettingsService.Settings.WindowY = Top * dpi;
        if (ViewModel.IsExpanded)
        {
            SettingsService.Settings.WindowWidth = Width * dpi;
            SettingsService.Settings.WindowHeight = Height * dpi;
        }
    }

    protected override void OnInitialized(EventArgs e)
    {
        if (SettingsService.Settings.Autooutwork)
        {
            var expired = ProfileService.CleanupOutdated();
            if (expired.Count > 0)
            {
                ViewModel.ExpiredHomeworks = expired;
                ViewModel.CanRecoverExpireHomework = true;
                ViewModel.SnackbarMessageQueue.Enqueue(
                    $"清除了{expired.Count}条过期的作业。",
                    "恢复",
                    (o) => RecoverExpiredHomework(),
                    null,
                    false,
                    false,
                    TimeSpan.FromSeconds(30)
                );
            }
        }
        else
        {
            // 不自动清理，ExpiredHomeworks 为空
            ViewModel.ExpiredHomeworks = new List<Homework>();
        }

        base.OnInitialized(e);
    }

    //全屏或最大化处理
    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
      
        if (WindowState != WindowState.Minimized &&
            ViewModel.IsUnlocked) 
        {
            if (WindowState != WindowState.Normal)
            {
                SetPos(); 
                WindowState = WindowState.Normal;
            }
        }
    }

    private void RecoverExpiredHomework()
    {
        if (!ViewModel.CanRecoverExpireHomework)
            return;
        ViewModel.CanRecoverExpireHomework = false;
        var rm = ViewModel.ExpiredHomeworks;
        if (rm == null || rm.Count == 0) return;
        var details = string.Join(", ", rm.Select(h => h.Subject ?? "(无科目)"));
        _logger.LogInformation("恢复 {Count} 条过期作业: {Details}", rm.Count, details);
        foreach (var i in rm)
        {
            ProfileService.Profile.Homeworks.Add(i);
        }
    }

    protected override void OnContentRendered(EventArgs e)
    {
        SetBottom();
        SetPos();
        AppEx.GetService<HomeworkEditWindow>().EditingFinished += OnEditingFinished;
        AppEx.GetService<HomeworkEditWindow>().SubjectChanged += OnSubjectChanged;
        base.OnContentRendered(e);
    }

    private void OnSubjectChanged(object? sender, EventArgs e)
    {
        if (ViewModel.IsUpdatingHomeworkSubject)
            return;
        if (ViewModel.SelectedHomework == null)
            return;
        if (!ViewModel.IsDrawerOpened)
            return;
        ViewModel.IsUpdatingHomeworkSubject = true;
        var s = ViewModel.SelectedHomework;
        ProfileService.Profile.Homeworks.Remove(s);
        ProfileService.Profile.Homeworks.Add(s);

        // 保存当前的IsDrawerOpened状态
        bool wasDrawerOpened = ViewModel.IsDrawerOpened;
        // 更新SelectedHomework
        ViewModel.SelectedHomework = s;
        ViewModel.IsUpdatingHomeworkSubject = false;
        // 确保编辑窗口保持打开状态
        if (wasDrawerOpened)
        {
            ViewModel.IsDrawerOpened = true;
            AppEx.GetService<HomeworkEditWindow>().TryOpen();
        }
    }

    private void OnEditingFinished(object? sender, EventArgs e)
    {
        ViewModel.IsCreatingMode = false;
        ExitEditingMode();
    }

    private void ButtonCreateHomework_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsFrozen) return;
        CreateHomework();
    }

    private void CreateHomework()
    {
        ViewModel.IsUpdatingHomeworkSubject = true;
        OnHomeworkEditorUpdated?.Invoke(this ,EventArgs.Empty);
        var lastSubject = ViewModel.EditingHomework.Subject;
        ViewModel.IsCreatingMode = true;
        ViewModel.IsDrawerOpened = true;
        var o = new Homework()
        {
            Subject = lastSubject
        };
        ViewModel.EditingHomework = o;
        ViewModel.SelectedHomework = o;
        ProfileService.Profile.Homeworks.Add(o);
        //ComboBoxSubject.Text = lastSubject;
        SettingsService.SaveSettings();
        ProfileService.SaveProfile();
        _logger.LogInformation("创建作业: Subject={Subject} DueTime={DueTime} Tags=[{Tags}] IsCompleted={IsCompleted}",
            o.Subject ?? "", o.DueTime.ToString("yyyy-MM-dd"),
            o.Tags != null ? string.Join(",", o.Tags) : "",
            o.IsExpired);
        ViewModel.IsUpdatingHomeworkSubject = false;
        RepositionEditingWindow();
        AppEx.GetService<HomeworkEditWindow>().TryOpen();
    }

    private void ButtonAddHomeworkCompleted_OnClick(object sender, RoutedEventArgs e)
    {
        ProfileService.Profile.Homeworks.Add(ViewModel.EditingHomework);
        ViewModel.IsDrawerOpened = false;
    }

    public void GetCurrentDpi(out double dpiX, out double dpiY)
    {
        var source = PresentationSource.FromVisual(this);

        dpiX = 1.0;
        dpiY = 1.0;

        if (source?.CompositionTarget != null)
        {
            dpiX = 1.0 * source.CompositionTarget.TransformToDevice.M11;
            dpiY = 1.0 * source.CompositionTarget.TransformToDevice.M22;
        }
    }

    private void ButtonSettings_OnClick(object sender, RoutedEventArgs e)
    {
        OpenSettingsWindow();
    }

    private void ButtonFreeze_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsFrozen = !ViewModel.IsFrozen;
    }

    private void OpenSettingsWindow()
    {
        var win = AppEx.GetService<SettingsWindow>();
        if (!win.IsOpened)
        {
            //Analytics.TrackEvent("打开设置窗口");
            win.IsOpened = true;
            win.Show();
        }
        else
        {
            if (win.WindowState == WindowState.Minimized)
            {
                win.WindowState = WindowState.Normal;
            }

            win.Activate();
        }
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (!ViewModel.IsClosing)
        {
            e.Cancel = true;
            return;
        }

        SavePos();
        SettingsService.SaveSettings();
        ProfileService.SaveProfile();
    }

    private void ButtonEditTags_OnClick(object sender, RoutedEventArgs e)
    {
        OnHomeworkEditorUpdated?.Invoke(this, EventArgs.Empty);
        ViewModel.IsTagEditingPopupOpened = true;
    }

    private void ButtonEditHomework_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsFrozen) return;
        OnHomeworkEditorUpdated?.Invoke(this, EventArgs.Empty);
        ViewModel.IsCreatingMode = false;
        _logger.LogDebug("开始编辑作业: Subject={Subject}", ViewModel.SelectedHomework.Subject);
        if (ViewModel.SelectedHomework== null)
            return;
        ViewModel.EditingHomework = ViewModel.SelectedHomework;
        ViewModel.IsDrawerOpened = true;
        RepositionEditingWindow();
        AppEx.GetService<HomeworkEditWindow>().TryOpen();
    }
    
    public void RepositionHomeworkEditWindow() => RepositionEditingWindow();

    private void RepositionEditingWindow()
    {
        if (ViewModel.SelectedListBoxItem == null)
            return;

        try
        {
            GetCurrentDpi(out var dpiX, out var dpiY);


            var itemRect = new Rect(
                ViewModel.SelectedListBoxItem.PointToScreen(new Point(0, 0)), // 左上角
                new Point(
                    ViewModel.SelectedListBoxItem.PointToScreen(new Point(ViewModel.SelectedListBoxItem.ActualWidth, 0)).X,
                    ViewModel.SelectedListBoxItem.PointToScreen(new Point(0, ViewModel.SelectedListBoxItem.ActualHeight)).Y
                )
            );


            var screen = Screen.PrimaryScreen!.WorkingArea;
            var screenRect = new Rect(screen.Left, screen.Top, screen.Width, screen.Height);


            var homeworkEditWindow = AppEx.GetService<HomeworkEditWindow>();


            var windowWidthPx = (int)(homeworkEditWindow.ActualWidth * dpiX);
            var windowHeightPx = (int)(homeworkEditWindow.ActualHeight * dpiY);

            double targetLeftPx = 0;
            double targetTopPx = 0;


      
            double preferredRight = itemRect.Right; 
            if (preferredRight + windowWidthPx <= screenRect.Right)
            {

                targetLeftPx = preferredRight;
            }
            else if (itemRect.Left - windowWidthPx >= screenRect.Left)
            {

                targetLeftPx = itemRect.Left - windowWidthPx;
            }
            else
            {

                targetLeftPx = Math.Max(screenRect.Left, itemRect.Left + (itemRect.Width - windowWidthPx) / 2);

                targetLeftPx = Math.Max(screenRect.Left, targetLeftPx);
            }

            double preferredTop = itemRect.Top;
            if (preferredTop + windowHeightPx <= screenRect.Bottom && preferredTop >= screenRect.Top)
            {
           
                targetTopPx = preferredTop;
            }
            else if (preferredTop + windowHeightPx > screenRect.Bottom && preferredTop - windowHeightPx >= screenRect.Top)
            {
                targetTopPx = preferredTop - windowHeightPx;
            }
            else
            {

                targetTopPx = itemRect.Top + (itemRect.Height - windowHeightPx) / 2;
               
                targetTopPx = Math.Max(screenRect.Top, targetTopPx); 
                targetTopPx = Math.Min(targetTopPx, screenRect.Bottom - windowHeightPx);
            }

            homeworkEditWindow.Left = targetLeftPx / dpiX;
            homeworkEditWindow.Top = targetTopPx / dpiY;


        }
        catch (Exception e)
        {
        }
    }


    private void ButtonRemoveHomework_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsFrozen) return;
        ViewModel.IsUpdatingHomeworkSubject = true;
        if (ViewModel.SelectedHomework == null)
            return;
        var hw = ViewModel.SelectedHomework;
        _logger.LogInformation("删除作业: Subject={Subject} DueTime={DueTime} Tags=[{Tags}] ContentLen={ContentLen}",
            hw.Subject ?? "", hw.DueTime.ToString("yyyy-MM-dd"),
            hw.Tags != null ? string.Join(",", hw.Tags) : "",
            hw.Content?.Length ?? 0);
        ProfileService.Profile.Homeworks.Remove(hw);
        ProfileService.SaveProfile();
        // 仅在不在还原状态时才触发备份
        if (!TimeMachineService.IsRestoring)
        {
            TimeMachineService.CreateBackup(MainListView);
        }
        ViewModel.IsUpdatingHomeworkSubject = false;
    }

    private void ButtonEditDone_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsDrawerOpened = false;
    }

    private void DragBorder_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.IsUnlocked && e.LeftButton == MouseButtonState.Pressed)
        {
            SetBottom();
            DragMove();
            SetBottom();
        }
    }

    private void SetBottom()
    {
        if (!SettingsService.Settings.IsBottom) return;

        if (_setBottomTimer == null)
        {
            _setBottomTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(300), DispatcherPriority.Background, (s, e) =>
            {
                _setBottomTimer.Stop();
            }, Dispatcher);
        }

        _setBottomTimer.Stop();
        _setBottomTimer.Start();
        var hWnd = new WindowInteropHelper(this).Handle;
        NativeWindowHelper.SetWindowPos(hWnd, NativeWindowHelper.HWND_BOTTOM, 0, 0, 0, 0, NativeWindowHelper.SWP_NOSIZE | NativeWindowHelper.SWP_NOMOVE | NativeWindowHelper.SWP_NOACTIVATE);
    }

    private void MainWindow_OnStateChanged(object? sender, EventArgs e)
    {
        SetBottom();
    }

    private void MainWindow_OnActivated(object? sender, EventArgs e)
    {
        SetBottom();
    }

    private void ButtonExit_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsClosing = true;
        Close();
    }

    private void ButtonDateSetToday_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.EditingHomework.DueTime = DateTime.Today;
    }

    private void ButtonDateSetWeekends_OnClick(object sender, RoutedEventArgs e)
    {
        var today = DateTime.Today;
        var delta = DayOfWeek.Saturday - today.DayOfWeek + 1;
        ViewModel.EditingHomework.DueTime = today + TimeSpan.FromDays(delta);
    }

    private void ButtonExpandingSwitcher_OnClick(object sender, RoutedEventArgs e)
    {
        SavePos();
        ViewModel.IsExpanded = !ViewModel.IsExpanded;
        if (ViewModel.IsExpanded)
        {
            SizeToContent = SizeToContent.Manual;
            SetPos();
        }
        else
        {
            ViewModel.IsUnlocked = false;
            SizeToContent = SizeToContent.Height;
            Width = Math.Min(ActualWidth, 350);
        }
    }

    private void MainWindow_OnDeactivated(object? sender, EventArgs e)
    {
        //MainListView.SelectedIndex = -1;
    }

    private async void ButtonExport_OnClick(object sender, RoutedEventArgs e)
    {
        
        ViewModel.IsWorking = true;

      
        var dialog = new System.Windows.Forms.SaveFileDialog()
        {
           
            Filter = "图片 (*.png)|*.png"
        };

        // 生成一个默认的文件名，包含时间戳
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        dialog.FileName = $"Export_{timestamp}.png";

        
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            goto done;
        }

        
        ExitEditingMode();

      
        await Task.Yield();

       
        var file = dialog.FileName;

       
        var scale = SettingsService.Settings.Scale;

        
        var listViewWidth = MainListView.ActualWidth;
        var listViewHeight = MainListView.ActualHeight;

        
        var backgroundWidth = listViewWidth * scale + 100;
        var backgroundHeight = listViewHeight * scale + 100;

        
        var backgroundVisual = new DrawingVisual();
        using (var context = backgroundVisual.RenderOpen())
        {
            
            var bg = (System.Windows.Media.Brush)FindResource("MaterialDesignPaper");

            // 绘制背景
            context.DrawRectangle(bg, null, new Rect(0, 0, backgroundWidth, backgroundHeight));
        }

   
        var contentVisual = new DrawingVisual();
        using (var context = contentVisual.RenderOpen())
        {
            // 创建一个新的视觉画刷
            var brush = new VisualBrush(MainListView)
            {
                Stretch = Stretch.None  // 设置画刷的拉伸模式为 None
            };

          
            context.DrawRectangle(brush, null, new Rect(50, 50, listViewWidth * scale, listViewHeight * scale));
        }

       
        var finalVisual = new DrawingVisual();
        using (var context = finalVisual.RenderOpen())
        {
            // 绘制背景
            context.DrawDrawing(backgroundVisual.Drawing);
            // 绘制内容
            context.DrawDrawing(contentVisual.Drawing);
        }

     
        var bitmap = new RenderTargetBitmap((int)backgroundWidth, (int)backgroundHeight, 96d, 96d, PixelFormats.Default);
        bitmap.Render(finalVisual);

     
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

     
        try
        {
            
            using (var stream = new FileStream(file, FileMode.Create, FileAccess.Write))
            {

                encoder.Save(stream);
                _logger.LogInformation("导出作业列表到 {FilePath} 作业数={Count} 尺寸={Width}x{Height}",
                    file, MainListView.Items.Count, (int)bitmap.Width, (int)bitmap.Height);


                ViewModel.SnackbarMessageQueue.Enqueue($"成功地导出到：{file}", "查看", () =>
                {
                    // 启动系统默认程序打开导出的文件
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = file,
                        UseShellExecute = true
                    });
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出作业列表失败");
            ViewModel.SnackbarMessageQueue.Enqueue($"导出失败：{ex.Message}");
        }

    done:
        // 释放保存对话框占用的资源
        dialog.Dispose();

        // false，导出已完成
        ViewModel.IsWorking = false;
    }


    private void DrawerHost_OnDrawerClosing(object? sender, DrawerClosingEventArgs e)
    {
        SettingsService.SaveSettings();
        ProfileService.SaveProfile();
    }

    private void ButtonMore_Click(object sender, RoutedEventArgs e)
    {
        PopupExAdvanced.IsOpen = true;
    }

    private void MainListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.IsFrozen)
        {
            var listView = sender as System.Windows.Controls.ListView;
            if (listView != null)
            {
                listView.SelectedIndex = -1;
            }
            return;
        }
        //ExitEditingMode(false);
    }

    public void OnTextBoxEnter()
    {
        // 编辑窗已打开时再按回车：先把当前条目正文写入模型，再继续新建（避免仅用 IsDrawerOpened 拦截而无法切换新建）
        var editWin = AppEx.GetService<HomeworkEditWindow>();
        if (ViewModel.IsDrawerOpened && editWin.IsOpened)
            editWin.FlushEditingToModel();

        CreateHomework();
    }

    private void MenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        PopupExAdvanced.IsOpen = false;
    }

    private void MenuItemRecoverExpiredHomework_OnClick(object sender, RoutedEventArgs e)
    {
        RecoverExpiredHomework();
    }

    private void MenuItemTimeMachine_OnClick(object sender, RoutedEventArgs e)
    {
        PopupExAdvanced.IsOpen = false;
        var win = AppEx.GetService<TimeMachineWindow>();
        if (!win.IsOpened)
        {
            win.IsOpened = true;
            win.Show();
        }
        else
        {
            if (win.WindowState == WindowState.Minimized)
            {
                win.WindowState = WindowState.Normal;
            }
            win.Activate();
        }
    }

    private void MainWindow_OnDragOver(object sender, DragEventArgs e)
    {

    }

    private void MainWindow_OnDragEnter(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;
        ViewModel.IsExpanded = false;
        ViewModel.IsUnlocked = false;
        SizeToContent = SizeToContent.Height;
        Width = Math.Min(ActualWidth, 350);
    }

    private void Docs(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://sh2.xn--fjqu59cvx0aoqi.icu/",
            UseShellExecute = true
        });
    }
    private void OpenTaskbarWindow()
    {
        var taskbarWindow = new SingleInstanceWarning();
        taskbarWindow.ShowDialog();
    }
    private void TrayIconView_TrayMouseRightClick(object sender, RoutedEventArgs e)
    {
        // 弹出 Taskbar.xaml 窗口
        var menu = new Views.Taskbar();
        // 在鼠标位置显示
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        _focusObserverService.FocusChanged -= FocusObserverServiceOnFocusChanged;
        ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        ViewModel.PropertyChanging -= ViewModelOnPropertyChanging;
        StateChanged -= OnWindowStateChanged;
        StateChanged -= MainWindow_OnStateChanged;
        Activated -= MainWindow_OnActivated;
        Deactivated -= MainWindow_OnDeactivated;

        if (_classIslandIpcService != null)
        {
            _classIslandIpcService.ClassStateChanged -= OnClassStateChanged;
            SettingsService.Settings.PropertyChanged -= _settingsClassIslandIpcPropertyChanged;
        }

        var homeworkEdit = AppEx.GetService<HomeworkEditWindow>();
        homeworkEdit.EditingFinished -= OnEditingFinished;
        homeworkEdit.SubjectChanged -= OnSubjectChanged;

        _setBottomTimer?.Stop();

        TrayIconView.TrayRightMouseUp -= TrayIconView_TrayMouseRightClick;
        TrayIconView.Dispose();
        base.OnClosed(e);
    }
}