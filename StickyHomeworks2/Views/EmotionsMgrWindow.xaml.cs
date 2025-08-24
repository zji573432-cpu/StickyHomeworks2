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
using System.Windows.Shapes;
using StickyHomeworks.Core.Context;

namespace StickyHomeworks.Views;

/// <summary>
/// EmotionsMgrWindow.xaml 的交互逻辑
/// </summary>
public partial class EmotionsMgrWindow : Window
{
    public AppDbContext DbContext { get; set; }

    public EmotionsMgrWindow(AppDbContext dbContext)
    {
        InitializeComponent();
        DataContext = this;
        DbContext = dbContext;
    }
}