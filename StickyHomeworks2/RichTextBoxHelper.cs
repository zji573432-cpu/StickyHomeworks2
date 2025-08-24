using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;

namespace StickyHomeworks;

public static class RichTextBoxHelper
{
    public static FlowDocument ConvertDocument(string xaml)
    {
        try
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml));
            var doc = (FlowDocument)XamlReader.Load(stream);
            doc.IsOptimalParagraphEnabled = true;
            return doc;
        }
        catch (Exception)
        {
            var doc = new FlowDocument();
            var para = new Paragraph();
            doc.IsOptimalParagraphEnabled = true;
            para.Inlines.Add(xaml);
            doc.Blocks.Add(para);
            return doc;
        }
    }
}