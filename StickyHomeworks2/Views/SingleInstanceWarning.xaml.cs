using System;
using System.Windows;
using ElysiaFramework.Controls;

namespace StickyHomeworks.Views
{
    /// <summary>
    /// SingleInstanceWarning.xaml 的交互逻辑
    /// </summary>
    public partial class SingleInstanceWarning : MyWindow
    {
        public SingleInstanceWarning()
        {
            InitializeComponent();
        }

        private void ButtonIgnore_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
            Application.Current.Shutdown();
            Environment.Exit(0);
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }
    }
}