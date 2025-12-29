using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace MiniMica
{
    public partial class MiniMicaWindow : Window
    {
        // Will the window start maximized?
        public bool IsStartMaximized { get; set; } = false;

        // Cached light/dark theme state for better performance
        private bool _isLightTheme;

        // Pseudo maximization state
        private bool _isPseudoMaximized = false;        // if the window is currently "maximized" 
        private Rect _restoreBounds;                    // the window's size and position before maximizing
        private bool _isDraggingFromMaximized = false;  // if the window is being dragged from a "maximized" state
        private Point _dragStartPoint;                  // for tracking system's official minimum drag distance

        // --- Win32 API Declarations for menu manipulation ---
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        public MiniMicaWindow()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (this.IsStartMaximized)
                TogglePseudoMaximize();

            var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            hwndSource.AddHook(WndProc);
            UpdateTheme();  // Initial theme apply

            // No need to display the scroll buttons initially, because the window size is big enough
            //UpdateScrollButtonsVisibility();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Define Win32 constants
            const int WM_ACTIVATEAPP = 0x001C;
            const int WM_SYSCOMMAND = 0x0112;
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
                if (_isPseudoMaximized)
                {
                    EnableMenuItem(systemMenu, SC_MAXIMIZE, MF_BYCOMMAND | MF_GRAYED);
                    EnableMenuItem(systemMenu, SC_RESTORE, MF_BYCOMMAND | MF_ENABLED);
                    EnableMenuItem(systemMenu, SC_MOVE, MF_BYCOMMAND | MF_GRAYED);
                    EnableMenuItem(systemMenu, SC_SIZE, MF_BYCOMMAND | MF_GRAYED);
                }
                else
                {
                    EnableMenuItem(systemMenu, SC_MAXIMIZE, MF_BYCOMMAND | MF_ENABLED);
                    EnableMenuItem(systemMenu, SC_RESTORE, MF_BYCOMMAND | MF_GRAYED);
                    EnableMenuItem(systemMenu, SC_MOVE, MF_BYCOMMAND | MF_ENABLED);
                    EnableMenuItem(systemMenu, SC_SIZE, MF_BYCOMMAND | MF_ENABLED);
                }
            }

            // --- Updated Logic to Intercept System Menu Commands ---
            if (msg == WM_SYSCOMMAND)
            {
                int command = wParam.ToInt32() & 0xFFF0;

                // If the user chooses to Maximize
                if (command == SC_MAXIMIZE)
                {
                    TogglePseudoMaximize();
                    handled = true;
                }
                // If the user chooses to Restore, ONLY handle it if we are
                // in our pseudo-maximized state AND the window is not currently minimized.
                else if (command == SC_RESTORE && _isPseudoMaximized && this.WindowState != WindowState.Minimized)
                {
                    TogglePseudoMaximize();
                    handled = true;
                }
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
            this.DropShadow.Color = _isLightTheme ? (Color)FindResource("MicaDropShadow_L") : (Color)FindResource("MicaDropShadow_D");
            this.DropShadow.Opacity = _isLightTheme ? 0.25 : 0.75;
            this.CanvasBorder.Style = _isLightTheme ? (Style)FindResource("CanvasBorderStyle_L") : (Style)FindResource("CanvasBorderStyle_D");

            ContentCtrl.UpdateTheme(_isLightTheme);
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

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            TogglePseudoMaximize();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MiniMicaDialog();
            //dialog.Owner = Window.GetWindow(this);
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void MouseLeftButton_Down(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                TogglePseudoMaximize();
            }
            else if (e.ClickCount == 1)
            {
                if (_isPseudoMaximized)
                {
                    // Arm the drag-to-restore, record the start point, and capture the mouse.
                    _isDraggingFromMaximized = true;
                    _dragStartPoint = this.PointToScreen(e.GetPosition(this));
                    Mouse.Capture(this);
                }
                else
                {
                    // If the window is normal, just perform a standard drag.
                    this.DragMove();
                }
            }
        }

        private void OnActivated(object sender, EventArgs e)
        {
            if (_isLightTheme)
            {
                btnClose.Style = (Style)this.FindResource("CloseButton_L_Activated");
                btnMaximize.Style = (Style)this.FindResource("MaximizeButton_L_Activated");
                btnMinimize.Style = (Style)this.FindResource("MinimizeButton_L_Activated");
                btnSettings.Style = (Style)this.FindResource("SettingsButton_L_Activated");
                textMain.Foreground = Brushes.Black;
            }
            else
            {
                btnClose.Style = (Style)this.FindResource("CloseButton_D_Activated");
                btnMaximize.Style = (Style)this.FindResource("MaximizeButton_D_Activated");
                btnMinimize.Style = (Style)this.FindResource("MinimizeButton_D_Activated");
                btnSettings.Style = (Style)this.FindResource("SettingsButton_D_Activated");
                textMain.Foreground = Brushes.White;
            }
        }

        private void OnDeactivated(object sender, EventArgs e)
        {
            if (_isLightTheme)
            {
                btnClose.Style = (Style)this.FindResource("CloseButton_L_Deactivated");
                btnMaximize.Style = (Style)this.FindResource("MaximizeButton_L_Deactivated");
                btnMinimize.Style = (Style)this.FindResource("MinimizeButton_L_Deactivated");
                btnSettings.Style = (Style)this.FindResource("SettingsButton_L_Deactivated");
                textMain.Foreground = Brushes.Silver;
            }
            else
            {
                btnClose.Style = (Style)this.FindResource("CloseButton_D_Deactivated");
                btnMaximize.Style = (Style)this.FindResource("MaximizeButton_D_Deactivated");
                btnMinimize.Style = (Style)this.FindResource("MinimizeButton_D_Deactivated");
                btnSettings.Style = (Style)this.FindResource("SettingsButton_D_Deactivated");
                textMain.Foreground = Brushes.Gray;
            }
        }

        private void TogglePseudoMaximize()
        {
            if (_isPseudoMaximized)
            {
                // RESTORE
                this.Top = _restoreBounds.Top;
                this.Left = _restoreBounds.Left;
                this.Width = _restoreBounds.Width;
                this.Height = _restoreBounds.Height;

                LayoutRoot.Margin = new Thickness(0);

                btnMaximize.Content = "\uE922";
                _isPseudoMaximized = false;

                // Restore WindowChrome properties
                MicaWindowChrome.ResizeBorderThickness = new Thickness(8);
                MicaWindowChrome.GlassFrameThickness = new Thickness(1);
            }
            else
            {
                // MAXIMIZE
                _restoreBounds = new Rect(this.Left, this.Top, this.Width, this.Height);

                var windowHandle = new WindowInteropHelper(this).Handle;
                // Explicit System.Windows.Forms since many types conflict with System.Windows
                System.Windows.Forms.Screen currentScreen = System.Windows.Forms.Screen.FromHandle(windowHandle);
                var workingAreaPixels = currentScreen.WorkingArea;

                PresentationSource source = PresentationSource.FromVisual(this);
                if (source == null) return;
                Matrix matrix = source.CompositionTarget.TransformToDevice;
                double scaleX = matrix.M11;

                double totalBorderThickness = (GetSystemMetrics(32) + GetSystemMetrics(92)) / scaleX;

                this.Top = (workingAreaPixels.Top / matrix.M22) - totalBorderThickness;
                this.Left = (workingAreaPixels.Left / matrix.M11) - totalBorderThickness;
                this.Width = (workingAreaPixels.Width / matrix.M11) + (totalBorderThickness * 2);
                this.Height = (workingAreaPixels.Height / matrix.M22) + (totalBorderThickness * 2);

                // Set the margin to pull the window control in-screen
                LayoutRoot.Margin = new Thickness(totalBorderThickness, totalBorderThickness, totalBorderThickness, totalBorderThickness);

                btnMaximize.Content = "\uE923";
                _isPseudoMaximized = true;

                // Set WindowChrome properties to zero to remove the frame/gap
                MicaWindowChrome.ResizeBorderThickness = new Thickness(0);
                MicaWindowChrome.GlassFrameThickness = new Thickness(0);
            }
        }

        /*
        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDraggingFromMaximized && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = this.PointToScreen(e.GetPosition(this));
                Vector dragDelta = currentPosition - _dragStartPoint;

                if (Math.Abs(dragDelta.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(dragDelta.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _isDraggingFromMaximized = false;
                    _isPseudoMaximized = false;

                    this.Width = _restoreBounds.Width;
                    this.Height = _restoreBounds.Height;
                    btnMaximize.Content = "\uE922";

                    // Restore WindowChrome properties
                    MicaWindowChrome.ResizeBorderThickness = new Thickness(8);
                    MicaWindowChrome.GlassFrameThickness = new Thickness(1);

                    // Restore the margin to 0 since maximized mode had padding
                    LayoutRoot.Margin = new Thickness(0);

                    this.Top = currentPosition.Y - 15;
                    this.Left = currentPosition.X - (this.Width / 2);

                    Mouse.Capture(null);
                    var windowHelper = new WindowInteropHelper(this);
                    SendMessage(windowHelper.Handle, 0x00A1, new IntPtr(2), IntPtr.Zero);
                }
            }
        }
        */

        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDraggingFromMaximized && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = this.PointToScreen(e.GetPosition(this));
                Vector dragDelta = currentPosition - _dragStartPoint;

                // Check if the mouse has moved more than the minimum system drag distance.
                if (Math.Abs(dragDelta.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(dragDelta.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    // The drag has started. Disarm the flag and restore the window.
                    _isDraggingFromMaximized = false;
                    _isPseudoMaximized = false;

                    // Restore window size and button icon
                    this.Width = _restoreBounds.Width;
                    this.Height = _restoreBounds.Height;
                    btnMaximize.Content = "\uE922";

                    // Restore WindowChrome properties
                    MicaWindowChrome.ResizeBorderThickness = new Thickness(8);
                    MicaWindowChrome.GlassFrameThickness = new Thickness(1);
                    LayoutRoot.Margin = new Thickness(0);

                    // --- DPI-Aware Repositioning Logic ---

                    // 1. Get the DPI scaling factor of the current monitor.
                    PresentationSource source = PresentationSource.FromVisual(this);
                    if (source == null) return; // Should not happen
                    Matrix matrix = source.CompositionTarget.TransformToDevice;
                    double scaleX = matrix.M11;
                    double scaleY = matrix.M22;

                    // 2. Convert the mouse's physical pixel coordinates to Device Independent Units (DIUs).
                    Point currentPositionDIU = new Point(currentPosition.X / scaleX, currentPosition.Y / scaleY);

                    // 3. Reposition the window using the corrected DIU values.
                    this.Top = currentPositionDIU.Y - 15;
                    this.Left = currentPositionDIU.X - (this.Width / 2);

                    // --- End of new logic ---

                    // Release capture and initiate native drag.
                    Mouse.Capture(null);
                    var windowHelper = new WindowInteropHelper(this);
                    SendMessage(windowHelper.Handle, 0x00A1, new IntPtr(2), IntPtr.Zero);
                }
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Disarm the flag if the user just clicks without dragging.
            _isDraggingFromMaximized = false;
            Mouse.Capture(null);
        }

        private void ScrollLeft_Click(object sender, RoutedEventArgs e)
        {
            ContentScroller.LineLeft();
        }

        private void ScrollRight_Click(object sender, RoutedEventArgs e)
        {
            ContentScroller.LineRight();
        }

        private void UpdateScrollButtonsVisibility()
        {
            if (ContentScroller.ScrollableWidth > 0)
            {
                btnScrollLeft.Visibility = Visibility.Visible;
                btnScrollRight.Visibility = Visibility.Visible;
            }
            else
            {
                btnScrollLeft.Visibility = Visibility.Collapsed;
                btnScrollRight.Visibility = Visibility.Collapsed;
            }
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Defer the visibility update until after the layout pass is complete.
            // This ensures that ScrollViewer.ScrollableWidth has an updated value.
            this.Dispatcher.BeginInvoke(
                new Action(() => UpdateScrollButtonsVisibility()),
                System.Windows.Threading.DispatcherPriority.Background
            );
        }
    }
}
