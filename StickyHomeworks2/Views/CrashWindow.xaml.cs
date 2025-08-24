using System.ComponentModel;
using System.Windows;
using ElysiaFramework.Controls;

namespace StickyHomeworks.Views;

/// <summary>
/// CrashWindow.xaml 的交互逻辑
/// </summary>
public partial class CrashWindow : MyWindow
{
    public string? CrashInfo
    {
        get;
        set;
    } = "";

    public Exception Exception
    {
        get;
        set;
    } = new();

    public CrashWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public bool IsShowed { get; set; } = false;

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        IsShowed = true;
        base.OnContentRendered(e);
    }

    private void ButtonIgnore_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ButtonExit_OnClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void ButtonRestart_OnClick(object sender, RoutedEventArgs e)
    {
        App.ReleaseLock();
        System.Windows.Forms.Application.Restart();
        Application.Current.Shutdown();
    }

    private void ButtonCopy_OnClick(object sender, RoutedEventArgs e)
    {
        TextBoxCrashInfo.Focus();
        TextBoxCrashInfo.SelectAll();
        TextBoxCrashInfo.Copy();
    }

    

    public void OpenWindow()
    {
        if (IsShowed)
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            Activate();
        }
        else
        {
            Show();
        }
    }

    private void CrashWindow_OnClosed(object? sender, CancelEventArgs e)
    {
        IsShowed = false;
        Hide();
        e.Cancel = true;
    }
}