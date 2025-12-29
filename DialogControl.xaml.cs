using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MiniMica
{
    /// <summary>
    /// Interaction logic for DialogControl.xaml
    /// </summary>
    public partial class DialogControl : UserControl
    {
        public DialogControl()
        {
            InitializeComponent();

            // Read settings
            //var config = ((App)Application.Current).g_appConfig;
            checkNotification.IsChecked = Global.appSettings.notification.Equals("1");
            checkDiagnostics.IsChecked = Global.appSettings.diagnostics.Equals("1");

            if (Global.appSettings.appearance.Equals("2"))
                radioAuto.IsChecked = true;
            else if (Global.appSettings.appearance.Equals("0"))
                radioDark.IsChecked = true;
            else if (Global.appSettings.appearance.Equals("1"))
                radioLight.IsChecked = true;

            comboLanguage.SelectedValue = Global.appSettings.language;

            // Enable TestOnly mode if user opens the Settings dialog while holding Ctrl+Shift
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift))
                TestOnly.Visibility = Visibility.Visible;        // hidden by default
            else
                TestOnly.Visibility = Visibility.Hidden;

            // Version string as Major.Minor.Build
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string strVersion = $"{version.Major}.{version.Minor}.{version.Build}";
            textVersion.Text = textVersion.Text.Replace("{M.m.build}", strVersion);
        }

        // MiniMicaDislog calls this method to update theme colors
        public void UpdateTheme(bool isLightTheme)
        {
            SolidColorBrush colorTextPrimary = isLightTheme
                ? new SolidColorBrush(Colors.Black)
                : new SolidColorBrush(Colors.White);
            SolidColorBrush colorTextSecondary = isLightTheme
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));

            textSettings.Foreground = colorTextPrimary;
            textGlyph1.Foreground = colorTextPrimary;
            textOption1.Foreground = colorTextPrimary;
            radioAuto.Foreground = colorTextPrimary;
            radioLight.Foreground = colorTextPrimary;
            radioDark.Foreground = colorTextPrimary;
            textGlyph2.Foreground = colorTextPrimary;
            textOption2.Foreground = colorTextPrimary;
            textGlyph3.Foreground = colorTextPrimary;
            textOption3.Foreground = colorTextPrimary;
            textGlyph4.Foreground = colorTextPrimary;
            textOption4.Foreground = colorTextPrimary;

            textVersion.Foreground = colorTextSecondary;
        }

        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Canvas Sizes = " + CtrlRoot.ActualWidth + ", " + CtrlRoot.ActualHeight);
        }

        private void CheckNotification_Click(object sender, RoutedEventArgs e)
        {
            bool? isChecked = checkNotification.IsChecked;
            if (isChecked == null)
                isChecked = true;   // default value

            Global.appSettings.notification = (bool)isChecked ? "1" : "0";
            Global.appConfig.WriteAppConfig("notification", Global.appSettings.notification);
        }

        private void CheckDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            bool? isChecked = checkDiagnostics.IsChecked;
            if (isChecked == null)
                isChecked = false;  // default value

            Global.appSettings.diagnostics = (bool)isChecked ? "1" : "0";
            Global.appConfig.WriteAppConfig("diagnostics", Global.appSettings.diagnostics);
        }

        /// <summary>
        /// Sends a WM_THEMECHANGED message to all application windows to trigger a theme update.
        /// </summary>
        private void BroadcastThemeChange()
        {
            // Send message to the main window
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                var helper = new WindowInteropHelper(mainWindow);
                SendMessage(helper.Handle, WM_THEMECHANGED, IntPtr.Zero, IntPtr.Zero);
            }

            // Send message to the settings dialog window
            var parentDialog = Window.GetWindow(this);
            if (parentDialog != null)
            {
                var helper = new WindowInteropHelper(parentDialog);
                SendMessage(helper.Handle, WM_THEMECHANGED, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private void RadioAuto_Click(object sender, RoutedEventArgs e)
        {
            Global.appSettings.appearance = "2";
            Global.appConfig.WriteAppConfig("appearance", Global.appSettings.appearance);
            BroadcastThemeChange();
        }

        private void RadioLight_Click(object sender, RoutedEventArgs e)
        {
            Global.appSettings.appearance = "1";
            Global.appConfig.WriteAppConfig("appearance", Global.appSettings.appearance);
            BroadcastThemeChange();
        }

        private void RadioDark_Click(object sender, RoutedEventArgs e)
        {
            Global.appSettings.appearance = "0";
            Global.appConfig.WriteAppConfig("appearance", Global.appSettings.appearance);
            BroadcastThemeChange();
        }

        private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return; // skip during initialization

            string culture = "00";
            if (comboLanguage.SelectedItem is ComboBoxItem cbi)
                culture = cbi.Tag as string;

            if (culture.Equals(Global.appSettings.language))
                return;

            Global.appSettings.language = culture;
            Global.appConfig.WriteAppConfig("language", Global.appSettings.language);

            if (Global.mutex != null)
            {
                Global.mutex.ReleaseMutex();
                Global.mutex = null;
            }

            MessageBox.Show("This app is about to restart.");
            Process.Start(Process.GetCurrentProcess().MainModule.FileName);
            Application.Current.Shutdown();
        }

        private const int WM_THEMECHANGED = 0x031A;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    }
}
