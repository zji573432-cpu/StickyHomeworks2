using System;
using System.Collections.Generic;
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

namespace StickyHomeworks.Controls;

/// <summary>
/// NumberUpDownControl.xaml 的交互逻辑
/// </summary>
public partial class NumberUpDownControl : UserControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(NumberUpDownControl), new PropertyMetadata(0.0));

    public double Value
    {
        get { return (double)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
        nameof(Step), typeof(double), typeof(NumberUpDownControl), new PropertyMetadata(0.1));

    public double Step
    {
        get { return (double)GetValue(StepProperty); }
        set { SetValue(StepProperty, value); }
    }

    public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
        nameof(MinValue), typeof(double), typeof(NumberUpDownControl), new PropertyMetadata(0.0));

    public double MinValue
    {
        get { return (double)GetValue(MinValueProperty); }
        set { SetValue(MinValueProperty, value); }
    }

    public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
        nameof(MaxValue), typeof(double), typeof(NumberUpDownControl), new PropertyMetadata(1.0));

    public double MaxValue
    {
        get { return (double)GetValue(MaxValueProperty); }
        set { SetValue(MaxValueProperty, value); }
    }

    public static readonly DependencyProperty RoundCountProperty = DependencyProperty.Register(
        nameof(RoundCount), typeof(int), typeof(NumberUpDownControl), new PropertyMetadata(2));

    public int RoundCount
    {
        get { return (int)GetValue(RoundCountProperty); }
        set { SetValue(RoundCountProperty, value); }
    }

    public static readonly DependencyProperty StringFormatProperty = DependencyProperty.Register(
        nameof(StringFormat), typeof(string), typeof(NumberUpDownControl), new PropertyMetadata("{0:P}"));

    public string StringFormat
    {
        get { return (string)GetValue(StringFormatProperty); }
        set { SetValue(StringFormatProperty, value); }
    }


    public NumberUpDownControl()
    {
        InitializeComponent();
    }

    private void ButtonMinus_OnClick(object sender, RoutedEventArgs e)
    {
        var fv = Math.Round(Value - Step, RoundCount);
        if (fv >= MinValue)
        {
            Value = fv;
        }
    }

    private void ButtonAdd_OnClick(object sender, RoutedEventArgs e)
    {
        var fv = Math.Round(Value + Step, RoundCount);
        if (fv <= MaxValue)
        {
            Value = fv;
        }
    }
}