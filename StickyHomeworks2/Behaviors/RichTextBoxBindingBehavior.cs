using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using Microsoft.Xaml.Behaviors;

namespace StickyHomeworks.Behaviors;

public class RichTextBoxBindingBehavior : Behavior<RichTextBox>
{
    private static HashSet<Thread> _recursionProtection = new HashSet<Thread>();

    protected override void OnAttached()
    {
        AssociatedObject.TextChanged += (obj2, e2) =>
        {
            var sw = new Stopwatch();
            sw.Start();
            RichTextBox richTextBox2 = obj2 as RichTextBox;
            if (richTextBox2 != null)
            {
                SetDocumentXaml(this, XamlWriter.Save(richTextBox2.Document));
            }

        };
        base.OnAttached();
    }

    public static string GetDocumentXaml(DependencyObject obj)
    {
        return (string)obj.GetValue(DocumentXamlProperty);
    }

    public static void SetDocumentXaml(DependencyObject obj, string value)
    {
        _recursionProtection.Add(Thread.CurrentThread);
        obj.SetValue(DocumentXamlProperty, value);
        _recursionProtection.Remove(Thread.CurrentThread);
    }

    public static readonly DependencyProperty DocumentXamlProperty = DependencyProperty.Register(
        nameof(DocumentXaml), typeof(string), typeof(RichTextBoxBindingBehavior), new FrameworkPropertyMetadata(
            "",
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (obj, e) =>
            {
                var sw = new Stopwatch();
                sw.Start();
                if (_recursionProtection.Contains(Thread.CurrentThread))
                {
                    Debug.WriteLine(sw.Elapsed.ToString());
                    return;
                }

                if (obj is not RichTextBoxBindingBehavior b)
                    return;
                var richTextBox = b.AssociatedObject;

                // Parse the XAML to a document (or use XamlReader.Parse())

                var documentXaml = GetDocumentXaml(b);
                richTextBox.Document = RichTextBoxHelper.ConvertDocument(documentXaml);
                richTextBox.Document.IsOptimalParagraphEnabled = true;

                // When the document changes update the source
                
            }
        ));

    public string DocumentXaml
    {
        get { return (string)GetValue(DocumentXamlProperty); }
        set { SetValue(DocumentXamlProperty, value); }
    }
}