using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace MiniMica
{
    /// <summary>
    /// Interaction logic for MiniMicaDialog.xaml
    /// </summary>
    public partial class MiniMicaDialog : Window
    {
        // Cached light/dark theme state for better performance
        private bool _isLightTheme;

        // --- Win32 API Declarations for menu manipulation ---
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        public MiniMicaDialog()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            hwndSource.AddHook(WndProc);
            UpdateTheme();  // Initial theme apply
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Define Win32 constants
            const int WM_ACTIVATEAPP = 0x001C;
            //const int WM_SYSCOMMAND = 0x0112;
            const int WM_INITMENUPOPUP = 0x0117;
            const int WM_SETTINGCHANGE = 0x001A;
            const int WM_THEMECHANGED = 0x031A;

            const uint SC_MAXIMIZE = 0xF030;
            const uint SC_RESTORE = 0xF120;
            const uint SC_MOVE = 0xF010;
            const uint SC_SIZE = 0xF000;

            const uint MF_BYCOMMAND = 0x00000000;
            const uint MF_GRAYED = 0x00000001;
            const uint MF_ENABLED = 0x00000000;

            if (msg == WM_ACTIVATEAPP)
            {
                if (wParam.ToInt32() != 0)
                {
                    if (this.WindowState == WindowState.Minimized)
                        this.WindowState = WindowState.Normal;
                    this.Activate();
                    this.Topmost = true;
                    this.Topmost = false;
                    this.Focus();
                }
            }

            if (msg == WM_INITMENUPOPUP)
            {
                IntPtr systemMenu = GetSystemMenu(hwnd, false);
                EnableMenuItem(systemMenu, SC_MAXIMIZE, MF_BYCOMMAND | MF_GRAYED);
                EnableMenuItem(systemMenu, SC_RESTORE, MF_BYCOMMAND | MF_GRAYED);
                EnableMenuItem(systemMenu, SC_MOVE, MF_BYCOMMAND | MF_ENABLED);
                EnableMenuItem(systemMenu, SC_SIZE, MF_BYCOMMAND | MF_GRAYED);
            }

            if (msg == WM_SETTINGCHANGE || msg == WM_THEMECHANGED)
            {
                UpdateTheme();
            }

            return IntPtr.Zero;
        }

        private void UpdateTheme()
        {
            _isLightTheme = IsLightThemeEnabled();
            this.Background = _isLightTheme ? (Brush)FindResource("MicaBackground_L") : (Brush)FindResource("MicaBackground_D");
            ContentCtrl.UpdateTheme(_isLightTheme);
            UpdateTitleBarTheme();  // Also update title bar controls to match the new theme
        }

        /// <summary>
        /// Updates the title bar controls based on the current theme and activation state.
        /// </summary>
        private void UpdateTitleBarTheme()
        {
            if (this.IsActive)
            {
                if (_isLightTheme)
                {
                    btnClose.Style = (Style)this.FindResource("CloseButton_L_Activated");
                    textMain.Foreground = Brushes.Black;
                }
                else
                {
                    btnClose.Style = (Style)this.FindResource("CloseButton_D_Activated");
                    textMain.Foreground = Brushes.White;
                }
            }
            else // Window is deactivated
            {
                if (_isLightTheme)
                {
                    btnClose.Style = (Style)this.FindResource("CloseButton_L_Deactivated");
                    textMain.Foreground = Brushes.Silver;
                }
                else
                {
                    btnClose.Style = (Style)this.FindResource("CloseButton_D_Deactivated");
                    textMain.Foreground = Brushes.Gray;
                }
            }
        }

        private bool IsLightThemeEnabled()
        {
            // Light theme
            if (Global.appSettings.appearance.Equals("1"))
                return true;

            // Dark theme
            if (Global.appSettings.appearance.Equals("0"))
                return false;

            // Automatic theme
            const string registryKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string valueName = "AppsUseLightTheme";

            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(registryKey))
            {
                var value = key?.GetValue(valueName);
                return value is int intValue && intValue != 0;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MouseLeftButton_Down(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void OnActivated(object sender, EventArgs e)
        {
            UpdateTitleBarTheme();
        }

        private void OnDeactivated(object sender, EventArgs e)
        {
            UpdateTitleBarTheme();
        }

        // Disable Maximize command
        private void OnStateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Maximized)
                this.WindowState = System.Windows.WindowState.Normal;
        }
    }
}
