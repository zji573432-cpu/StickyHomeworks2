using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ElysiaFramework;
using StickyHomeworks.Services;
using StickyHomeworks.ViewModels;

namespace StickyHomeworks.Views;

/// <summary>
/// HomeworkEditWindow.xaml 的交互逻辑
/// </summary>
public partial class HomeworkEditWindow : Window, INotifyPropertyChanged
{
    private RichTextBox _relatedRichTextBox = new();
    public MainWindow MainWindow { get; }
    public SettingsService SettingsService { get; }

    public HomeworkEditViewModel ViewModel { get; } = new();

    public bool IsOpened { get; set; } = false;

    public event EventHandler? EditingFinished;

    public event EventHandler? SubjectChanged;

    public void TryOpen()
    {
        if (IsOpened)
            return;
        Show();
        Activate();
        IsOpened = true;
    }

    public void TryClose()
    {
        if (!IsOpened)
            return;
        IsOpened = false;
        Hide();
    }

    public RichTextBox RelatedRichTextBox
    {
        get => _relatedRichTextBox;
        set
        {
            UnregisterOldTextBox(_relatedRichTextBox);
            RegisterNewTextBox(value);
            _relatedRichTextBox = value;
            OnPropertyChanged();
        }
    }

    public HomeworkEditWindow(MainWindow mainWindow, SettingsService settingsService)
    {
        MainWindow = mainWindow;
        SettingsService = settingsService;
        DataContext = this;
        InitializeComponent();
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    protected override void OnInitialized(EventArgs e)
    {
        ViewModel.FontFamilies =
            new ObservableCollection<FontFamily>(from i in Fonts.SystemFontFamilies orderby i.ToString() select i)
                { (FontFamily)FindResource("HarmonyOsSans") };
        base.OnInitialized(e);
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (ViewModel.IsRestoringSelection)
        {
            return;
        }
        var s = RelatedRichTextBox.Selection;
        switch (e.PropertyName)
        {
            case nameof(ViewModel.TextColor):
                s.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(ViewModel.TextColor));
                break;
            case nameof(ViewModel.Font):
                s.ApplyPropertyValue(TextElement.FontFamilyProperty, ViewModel.Font);
                break;
            case nameof(ViewModel.FontSize):
                s.ApplyPropertyValue(TextElement.FontSizeProperty, Math.Max(ViewModel.FontSize, 8));
                break;
        }
    }

    private void RegisterNewTextBox(RichTextBox richTextBox)
    {
        richTextBox.TextChanged += RichTextBoxOnTextChanged;
        richTextBox.SelectionChanged += RichTextBoxOnSelectionChanged;
    }

    private void RichTextBoxOnSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsRestoringSelection)
            return;
        Debug.WriteLine("selection changed!");
        if (RelatedRichTextBox.Selection.Start.Paragraph != null)
            ViewModel.SelectedParagraph = RelatedRichTextBox.Selection.Start.Paragraph;
        // Update selection
        var s = RelatedRichTextBox.Selection;
        if (!MainWindow.IsActive)
        {
            if (ViewModel is not { BeforeTextPointerStart: not null, BeforeTextPointerEnd: not null })
                return;
            ViewModel.IsRestoringSelection = true;
            RelatedRichTextBox.Selection.Select(ViewModel.BeforeTextPointerStart, ViewModel.BeforeTextPointerEnd);
            ViewModel.IsRestoringSelection = false;
            return;
        }
        ViewModel.Selection = s;
        ViewModel.BeforeTextPointerStart = s.Start;
        ViewModel.BeforeTextPointerEnd = s.End;
        ViewModel.IsRestoringSelection = true;
        Debug.WriteLine("selection updated!");
        var w = s.GetPropertyValue(TextElement.FontWeightProperty);
        if (w is FontWeight weight)
        {
            ViewModel.IsBold = weight >= FontWeights.Bold;
        }

        ViewModel.IsItalic = Equals(s.GetPropertyValue(TextElement.FontStyleProperty), FontStyles.Italic);
        if (s.GetPropertyValue(Paragraph.TextDecorationsProperty) is TextDecorationCollection decorations)
        {
            ViewModel.IsUnderlined = decorations.Contains(TextDecorations.Underline[0]);
            ViewModel.IsStrikeThrough = decorations.Contains(TextDecorations.Strikethrough[0]);
        }

        if (s.GetPropertyValue(TextElement.ForegroundProperty) is SolidColorBrush fg)
        {
            ViewModel.TextColor = fg.Color;
        }
        if (s.GetPropertyValue(TextElement.FontFamilyProperty) is FontFamily font)
        {
            ViewModel.Font = font;
        }

        if (s.GetPropertyValue(TextElement.FontSizeProperty) is double fontSize)
        {
            ViewModel.FontSize = fontSize;
        }
        ViewModel.IsRestoringSelection = false;
    }

    private void RichTextBoxOnTextChanged(object sender, TextChangedEventArgs e)
    {
    }

    private void UnregisterOldTextBox(RichTextBox richTextBox)
    {
        richTextBox.TextChanged -= RichTextBoxOnTextChanged;
        richTextBox.SelectionChanged -= RichTextBoxOnSelectionChanged;
    }

    private void ListBoxTextStyles_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.IsRestoringSelection)
            return;
        var s = RelatedRichTextBox.Selection;
        s.ApplyPropertyValue(TextElement.FontWeightProperty, ViewModel.IsBold? FontWeights.Bold : FontWeights.Regular);
        s.ApplyPropertyValue(TextElement.FontStyleProperty, ViewModel.IsItalic ? FontStyles.Italic : FontStyles.Normal);
        var decorations = new TextDecorationCollection();
        if (ViewModel.IsUnderlined)
            decorations.Add(TextDecorations.Underline);
        if (ViewModel.IsStrikeThrough)
            decorations.Add(TextDecorations.Strikethrough);
        s.ApplyPropertyValue(Paragraph.TextDecorationsProperty, decorations);
        RelatedRichTextBox.Focus();
    }

    private void ButtonClearColor_OnClick(object sender, RoutedEventArgs e)
    {
        var s = RelatedRichTextBox.Selection;
        s.ApplyPropertyValue(TextElement.ForegroundProperty, GetValue(TextElement.ForegroundProperty));
    }

    private void ButtonFontSizeDecrease_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.FontSize -= 2;
    }

    private void ButtonFontSizeIncrease_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.FontSize += 2;
    }

    private void ButtonEditingDone_OnClick(object sender, RoutedEventArgs e)
    {
        EditingFinished?.Invoke(this, EventArgs.Empty);
        AppEx.GetService<ProfileService>().SaveProfile();
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SubjectChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ButtonAddToColor_OnClick(object sender, RoutedEventArgs e)
    {
        if (SettingsService.Settings.SavedColors.Contains(ViewModel.TextColor))
            return;
        SettingsService.Settings.SavedColors.Insert(0, ViewModel.TextColor);
        while (SettingsService.Settings.SavedColors.Count > 6)
        {
            SettingsService.Settings.SavedColors.RemoveAt(6);
        }
        SettingsService.SaveSettings();
    }

    private void ListBoxColors_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.IsUpdatingColor)
            return;
        ViewModel.IsUpdatingColor = true;
        foreach (var i in e.AddedItems)
        {
            if (i is Color c)
                ViewModel.TextColor = c;
        }

        if (sender is ListBox l)
            l.SelectedIndex = -1;
        ViewModel.IsUpdatingColor = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}