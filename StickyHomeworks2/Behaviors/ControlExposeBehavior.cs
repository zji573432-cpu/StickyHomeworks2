using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace StickyHomeworks.Behaviors;

public class ControlExposeBehavior : Behavior<Control>
{
    public static readonly DependencyProperty ExposedControlProperty = DependencyProperty.Register(
        nameof(ExposedControl), typeof(Control), typeof(ControlExposeBehavior), new PropertyMetadata(default(Control)));

    public Control ExposedControl
    {
        get { return (Control)GetValue(ExposedControlProperty); }
        set { SetValue(ExposedControlProperty, value); }
    }

    public static readonly DependencyProperty ConditionProperty = DependencyProperty.Register(
        nameof(Condition), typeof(bool), typeof(ControlExposeBehavior), new PropertyMetadata(default(bool), (o, args) =>
        {
            if (o is not ControlExposeBehavior b)
                return;
            b.TryExpose();
        }));

    public bool Condition
    {
        get { return (bool)GetValue(ConditionProperty); }
        set { SetValue(ConditionProperty, value); }
    }

    public void TryExpose()
    {
        if (Condition)
            ExposedControl = AssociatedObject;
    }

    protected override void OnAttached()
    {
        TryExpose();
        base.OnAttached();
    }
}