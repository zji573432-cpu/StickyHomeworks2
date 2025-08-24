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

namespace StickyHomeworks;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; set; } = new MainViewModel();

    public ProfileService ProfileService { get; }

    public SettingsService SettingsService { get; }

    public event EventHandler? OnHomeworkEditorUpdated;

    public MainWindow(ProfileService profileService,
                      SettingsService settingsService,
                      WindowFocusObserverService focusObserverService)
    {
        ProfileService = profileService;
        SettingsService = settingsService;
        //Automation.AddAutomationFocusChangedEventHandler(OnFocusChangedHandler);
        InitializeComponent();
        focusObserverService.FocusChanged += FocusObserverServiceOnFocusChanged;
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        ViewModel.PropertyChanging += ViewModelOnPropertyChanging;
        DataContext = this;
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
        if (e.PropertyName == nameof(ViewModel.SelectedHomework))
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
        if (e.PropertyName == nameof(ViewModel.SelectedHomework))
        {
            ExitEditingMode(false);
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
    }

    private void SetPos()
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
        ViewModel.ExpiredHomeworks = ProfileService.CleanupOutdated();
        if (ViewModel.ExpiredHomeworks.Count > 0)
        {
            ViewModel.CanRecoverExpireHomework = true;
            ViewModel.SnackbarMessageQueue.Enqueue($"清除了{ViewModel.ExpiredHomeworks.Count}条过期的作业。",
                "恢复", (o) => { RecoverExpiredHomework(); }, null, false, false, TimeSpan.FromSeconds(30));
        }
        base.OnInitialized(e);
    }

    private void RecoverExpiredHomework()
    {
        if (!ViewModel.CanRecoverExpireHomework)
            return;
        ViewModel.CanRecoverExpireHomework = false;
        var rm = ViewModel.ExpiredHomeworks;
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
        ViewModel.SelectedHomework = s;
        ViewModel.IsUpdatingHomeworkSubject = false;
    }

    private void OnEditingFinished(object? sender, EventArgs e)
    {
        ExitEditingMode();
    }

    private void ButtonCreateHomework_OnClick(object sender, RoutedEventArgs e)
    {
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
        OnHomeworkEditorUpdated?.Invoke(this, EventArgs.Empty);
        ViewModel.IsCreatingMode = false;
        if (ViewModel.SelectedHomework== null)
            return;
        ViewModel.EditingHomework = ViewModel.SelectedHomework;
        ViewModel.IsDrawerOpened = true;
        RepositionEditingWindow();
        AppEx.GetService<HomeworkEditWindow>().TryOpen();
    }

    private void RepositionEditingWindow()
    {
        if (ViewModel.SelectedListBoxItem == null) 
            return;
        Debug.WriteLine("selected changed");
        try
        {
            GetCurrentDpi(out var dpiX, out var dpiY);
            var p = ViewModel.SelectedListBoxItem.PointToScreen(new Point(ViewModel.SelectedListBoxItem.ActualWidth, 0));
            var screen = Screen.PrimaryScreen!.WorkingArea;
            var homeworkEditWindow = AppEx.GetService<HomeworkEditWindow>();
            homeworkEditWindow.Left = p.X / dpiX;
            homeworkEditWindow.Top = Math.Min(p.Y, screen.Bottom - homeworkEditWindow.ActualHeight * dpiY) / dpiY;
        }
        catch (Exception e)
        {
            // ignored
        }
    }

    private void ButtonRemoveHomework_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsUpdatingHomeworkSubject = true;
        if (ViewModel.SelectedHomework == null)
            return;
        ProfileService.Profile.Homeworks.Remove(ViewModel.SelectedHomework);
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
        if (!SettingsService.Settings.IsBottom)
        {
            return;
        }
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
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            goto done;
        }

        ExitEditingMode();
        //MainListView.Background = (Brush)FindResource("MaterialDesignPaper");
        await System.Windows.Threading.Dispatcher.Yield(DispatcherPriority.Render);
        var file = dialog.FileName!;
        var visual = new DrawingVisual();
        var s = SettingsService.Settings.Scale;
        using (var context = visual.RenderOpen())
        {
            var brush = new VisualBrush(MainListView)
            {
                Stretch = Stretch.None
            };
            var bg = (Brush)FindResource("MaterialDesignPaper");
            context.DrawRectangle(bg, null, new Rect(0, 0, MainListView.ActualWidth * s, MainListView.ActualHeight * s)); 
            context.DrawRectangle(brush, null, new Rect(0, 0, MainListView.ActualWidth * s, MainListView.ActualHeight * s));
            context.Close();
        }

        var bitmap = new RenderTargetBitmap((int)(MainListView.ActualWidth * s), (int)(ActualHeight * s), 96d, 96d,
            PixelFormats.Default);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        try
        {
            var stream = File.Open(file, FileMode.OpenOrCreate);
            encoder.Save(stream);
            stream.Close();
            ViewModel.SnackbarMessageQueue.Enqueue($"成功地导出到：{file}", "查看", () =>
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = file,
                    UseShellExecute = true
                });
            });

        }
        catch(Exception ex)
        {
            ViewModel.SnackbarMessageQueue.Enqueue($"导出失败：{ex}");
        }

        done:
        //MainListView.Background = null;
        dialog.Dispose();
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
        //ExitEditingMode(false);
    }

    public void OnTextBoxEnter()
    {
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
}