using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ElysiaFramework;
using Stfu.Linq;
using StickyHomeworks.Models;
using StickyHomeworks.Views;

namespace StickyHomeworks.Controls;

/// <summary>
/// HomeworkControl.xaml 的交互逻辑
/// </summary>
public partial class HomeworkControl : UserControl
{
    public static readonly DependencyProperty HomeworkProperty = DependencyProperty.Register(
        nameof(Homework), typeof(Homework), typeof(HomeworkControl), new PropertyMetadata(default(Homework)));

    public Homework Homework
    {
        get { return (Homework)GetValue(HomeworkProperty); }
        set { SetValue(HomeworkProperty, value); }
    }

    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected), typeof(bool), typeof(HomeworkControl), new PropertyMetadata(default(bool), (o, args) =>
        {
            var c = o as HomeworkControl;
            c?.IsSelectedChanged((bool)args.NewValue);
        }));

    public bool IsSelected
    {
        get { return (bool)GetValue(IsSelectedProperty); }
        set { SetValue(IsSelectedProperty, value); }
    }

    public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register(
        nameof(IsEditing), typeof(bool), typeof(HomeworkControl), new PropertyMetadata(default(bool),
            (o, args) =>
            {
                var c = o as HomeworkControl;
                c?.IsEditingChanged((bool)args.NewValue);
            }));


    public bool IsEditing
    {
        get { return (bool)GetValue(IsEditingProperty); }
        set { SetValue(IsEditingProperty, value); }
    }

    public HomeworkControl()
    {
        InitializeComponent();
    }

    private void IsEditingChanged(bool value)
    {
        Debug.WriteLine($"IsEditing changed! {value} {IsSelected}");
        if (IsSelected && value)
        {
            Debug.WriteLine("RelatedRichTextBox updated because IsEditing changed");
            EnterEdit();
        }
    }

    private async void EnterEdit()
    {
        AppEx.GetService<HomeworkEditWindow>().RelatedRichTextBox = RichTextBox;
        await System.Windows.Threading.Dispatcher.Yield();
        RichTextBox.Focus();
        //RichTextBox.Selection.Select(new TextPointer());
        RichTextBox.CaretPosition = RichTextBox.CaretPosition.DocumentEnd;
    }

    private void IsSelectedChanged(bool value)
    {
        Debug.WriteLine($"IsSelected changed! {value} {IsEditing}");
        if (value && IsEditing)
        {
            Debug.WriteLine("RelatedRichTextBox updated because IsSelected changed");
            EnterEdit();
        }
    }

    private void RichTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            App.GetService<MainWindow>().OnTextBoxEnter();
        }
    }
}