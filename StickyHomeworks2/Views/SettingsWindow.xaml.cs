using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using ClassIsland.Services;
using ElysiaFramework;
using ElysiaFramework.Controls;
using MaterialDesignThemes.Wpf;
using StickyHomeworks.Models;
using StickyHomeworks.Services;
using StickyHomeworks.ViewModels;

namespace StickyHomeworks.Views;
/// <summary>
/// SettingsWindow.xaml 的交互逻辑
/// </summary>
public partial class SettingsWindow : MyWindow
{
    public SettingsViewModel ViewModel
    {
        get;
        set;
    } = new();

    public Settings Settings
    {
        get;
        set;
    } = new();

    public bool IsOpened
    {
        get;
        set;
    } = false;

    public WallpaperPickingService WallpaperPickingService { get; }

    public SettingsWindow(WallpaperPickingService wallpaperPickingService,
        SettingsService settingsService)
    {
        WallpaperPickingService = wallpaperPickingService;

        InitializeComponent();
        DataContext = this;
        Settings = settingsService.Settings;
        settingsService.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == "Settings")
            {
                settingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
                Settings = settingsService.Settings;
            }
        };
        var style = (Style)FindResource("NotificationsListBoxItemStyle");
        //style.Setters.Add(new EventSetter(ListBoxItem.MouseDoubleClickEvent, new System.Windows.Input.MouseEventHandler(EventSetter_OnHandler)));
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        
    }


    protected override void OnInitialized(EventArgs e)
    {
        //RefreshMonitors();
        //var r = new StreamReader(Application.GetResourceStream(new Uri("/Assets/LICENSE.txt", UriKind.Relative))!.Stream);
        //ViewModel.License = r.ReadToEnd();
        base.OnInitialized(e);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        Settings.PropertyChanged += SettingsOnPropertyChanged;
        base.OnContentRendered(e);
    }

    private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!e.Handled)
        {
            // ListView拦截鼠标滚轮事件
            e.Handled = true;

            // 激发一个鼠标滚轮事件，冒泡给外层ListView接收到
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = sender;
            var parent = ((System.Windows.Controls.Control)sender).Parent as UIElement;
            if (parent != null)
            {
                parent.RaiseEvent(eventArg);
            }
        }
    }

    private void SettingsWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        AppEx.GetService<SettingsService>().SaveSettings();
        IsOpened = false;
    }

    private void ButtonCrash_OnClick(object sender, RoutedEventArgs e)
    {
        throw new Exception("Crash test.");
    }

    private void HyperlinkMsAppCenter_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = "https://learn.microsoft.com/zh-cn/appcenter/sdk/data-collected",
            UseShellExecute = true
        });
    }

    private void MyDrawerHost_OnDrawerClosing(object? sender, DrawerClosingEventArgs e)
    {
    }

    private void ButtonDebugToastText_OnClick(object sender, RoutedEventArgs e)
    {
        
    }


    private void ButtonDebugNetworkError_OnClick(object sender, RoutedEventArgs e)
    {
        //UpdateService.CurrentWorkingStatus = UpdateWorkingStatus.NetworkError;
    }


    private void OpenDrawer(string key)
    {
        MyDrawerHost.IsRightDrawerOpen = true;
        ViewModel.DrawerContent = FindResource(key);
    }

    private async Task<object?> ShowDialog(string key)
    {
        return await DialogHost.Show(FindResource(key), "SettingsWindow");
    }


    private void ButtonContributors_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("ContributorsDrawer");
    }

    private void ButtonThirdPartyLibs_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("ThirdPartyLibs");
    }

    private void AppIcon_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel.AppIconClickCount++;
        if (ViewModel.AppIconClickCount >= 10)
        {
            Settings.IsDebugOptionsEnabled = true;
        }
    }

    private void ButtonCloseDebug_OnClick(object sender, RoutedEventArgs e)
    {
        Settings.IsDebugOptionsEnabled = false;
        ViewModel.AppIconClickCount = 0;
    }

    private void MenuItemDebugScreenShot_OnClick(object sender, RoutedEventArgs e)
    {
        
    }

    private async void ButtonUpdateWallpaper_OnClick(object sender, RoutedEventArgs e)
    {
        await WallpaperPickingService.GetWallpaperAsync();
    }

    private async void ButtonBrowseWindows_OnClick(object sender, RoutedEventArgs e)
    {
        var w = new WindowsPicker(Settings.WallpaperClassName)
        {
            Owner = this,
        };
        var r = w.ShowDialog();
        Settings.WallpaperClassName = w.SelectedResult ?? "";
        if (r == true)
        {
            await WallpaperPickingService.GetWallpaperAsync();
        }
        GC.Collect();
    }

    private void MenuItemExperimentalSettings_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsPopupMenuOpened = false;
        OpenDrawer("ExperimentalSettings");
    }

    private async Task EditSubjectAsync(int index)
    {
        ViewModel.SubjectEditText = Settings.Subjects[index];
        var r = (string?)await ShowDialog("EditSubjectDialog");
        if (r == null) return;
        Settings.Subjects[index] = r;
    }

    private async Task EditTagAsync(int index)
    {
        ViewModel.TagEditText = Settings.Tags[index];
        var r = (string?)await ShowDialog("EditTagDialog");
        if (r == null) return;
        Settings.Tags[index] = r;
    }

    private async void ButtonAddSubject_OnClick(object sender, RoutedEventArgs e)
    {
        Settings.Subjects.Add("");
        await EditSubjectAsync(Settings.Subjects.Count - 1);
        var r = Settings.Subjects.Last();
        if (r == "")
        {
            Settings.Subjects.RemoveAt(Settings.Subjects.Count - 1);
        }
        else
        {
            ViewModel.SubjectSelectedIndex = Settings.Subjects.Count - 1;
        }
    }

    private async void ButtonEditSubject_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SubjectSelectedIndex == -1)
        {
            return;
        }
        await EditSubjectAsync(ViewModel.SubjectSelectedIndex);
    }

    private void ButtonDeleteSubject_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SubjectSelectedIndex == -1)
        {
            return;
        }
        Settings.Subjects.RemoveAt(ViewModel.SubjectSelectedIndex);
    }

    private async void ButtonAddTag_OnClick(object sender, RoutedEventArgs e)
    {
        Settings.Tags.Add("");
        await EditTagAsync(Settings.Tags.Count - 1);
        var r = Settings.Tags.Last();
        if (r == "")
        {
            Settings.Tags.RemoveAt(Settings.Tags.Count - 1);
        }
        else
        {
            ViewModel.TagSelectedIndex = Settings.Tags.Count - 1;
        }
    }

    private async void ButtonEditTag_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.TagSelectedIndex == -1)
        {
            return;
        }
        await EditTagAsync(ViewModel.TagSelectedIndex);
    }

    private void ButtonDeleteTag_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.TagSelectedIndex == -1)
        {
            return;
        }
        Settings.Tags.RemoveAt(ViewModel.TagSelectedIndex);
    }

    private void MenuItemTestHomeworkEditWindow_OnClick(object sender, RoutedEventArgs e)
    {
        AppEx.GetService<HomeworkEditWindow>().Show();
    }
}
