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

namespace MiniMica
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        public MainControl()
        {
            InitializeComponent();
        }

        // MiniMicaWindow calls this method to update theme colors
        public void UpdateTheme(bool isLightTheme)
        {
            textSample.Foreground = isLightTheme ? new SolidColorBrush(Colors.Purple) : new SolidColorBrush(Colors.Orange);
        }

        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MiniMicaDialog();
            dialog.Owner = Window.GetWindow(this);
            //dialog.textSample.Text = "LayoutRoot Sizes = " + CtrlRoot.ActualWidth + ", " + CtrlRoot.ActualHeight;
            bool? result = dialog.ShowDialog();
        }
    }
}
