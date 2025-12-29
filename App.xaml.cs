using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace MiniMica
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // Uncomment this if embedding localization DLLs into the main EXE
            // For more details, see 'Localization Notes.md'.
            //AppDomain.CurrentDomain.AssemblyResolve += Resolver;
        }

        #region Localization Support
        // Include the embedded language DLLs as resources in the project
        private static Assembly Resolver(object sender, ResolveEventArgs args)
        {
            // Get the name of the assembly that failed to load
            var requestedAssembly = new AssemblyName(args.Name);

            // We're only interested in our satellite assemblies. Their names end with ".resources".
            if (!requestedAssembly.Name.EndsWith(".resources"))
                return null;

            // Get the culture code (e.g., "es", "zh-CN") from the assembly name
            string cultureName = requestedAssembly.CultureName;

            // Build the name of the embedded resource.
            // Format: YourProjectName.FolderName.CultureCode.resources.dll
            string resourceName = $"MiniMica.EmbeddedAssemblies.{cultureName}.resources.dll";

            // Get the current assembly (your main .exe).
            var currentAssembly = Assembly.GetExecutingAssembly();

            // Load the embedded resource as a stream.
            using (var stream = currentAssembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    return null;

                // Read the stream into a byte array.
                var assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);

                // Load the byte array as an assembly and return it.
                return Assembly.Load(assemblyData);
            }
        }
        #endregion

        private void OnStartup(object sender, StartupEventArgs e)
        {
#if DEBUG
            // --- FOR TESTING LOCALIZATION ONLY ---
            //Thread.CurrentThread.CurrentUICulture = new CultureInfo("es");
            //Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");
            //Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-TW");
#endif

            // Initialize App Settings
            Global.appName = "Contoso";
            Global.appConfig = new MiniMicaConfig(Global.appName);
            if (!Global.appConfig.Exists())
            {
                // Set option defaults
                Global.appSettings.appearance = "2";    // 0=Dark, 1=Light, 2=Automatic
                Global.appSettings.notification = "1";  // 0=Off, 1=On
                Global.appSettings.diagnostics = "0";   // 0=Off, 1=On
                Global.appSettings.language = "00";     // 00=system default
                Global.appConfig.WriteAppConfig("appearance", Global.appSettings.appearance);
                Global.appConfig.WriteAppConfig("notification", Global.appSettings.notification);
                Global.appConfig.WriteAppConfig("diagnostics", Global.appSettings.diagnostics);
                Global.appConfig.WriteAppConfig("language", Global.appSettings.language);
            }
            else
            {
                // Read settings
                Global.appSettings.appearance = Global.appConfig.ReadAppConfig("appearance");
                Global.appSettings.notification = Global.appConfig.ReadAppConfig("notification");
                Global.appSettings.diagnostics = Global.appConfig.ReadAppConfig("diagnostics");
                Global.appSettings.language = Global.appConfig.ReadAppConfig("language");
            }

            // User selected UI language
            if (!Global.appSettings.language.Equals("00"))
                SetUiCulture(Global.appSettings.language);

            // Resolve the current UI culture (used for language-specific layout adjustments)
            //Global.uiLanguage = MiniMica.i18n.Strings.minimica_culture;
            Global.uiLanguage = Strings.minimica_culture;

            // Start the main window
            var mainWindow = new MiniMicaWindow();
            // Set the application's main window
            App.Current.MainWindow = mainWindow;
            // Optional: set the window to start maximized
            mainWindow.IsStartMaximized = true;
            mainWindow.Show();
        }

        private void SetUiCulture(string culture)
        {
            var ci = new CultureInfo(culture);
            Thread.CurrentThread.CurrentUICulture = ci;
            Thread.CurrentThread.CurrentCulture = ci;   // numbers/dates, etc.

            // Make WPF use the new culture for Language-dependent formatting
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(ci.IetfLanguageTag)));
        }
    }
}

/*

WHAT's NEW
------------------------------------------------------------------------------------------------------------------------
MiniMica is a fully custom, modern WPF window that correctly handles activation states, light/dark themes, and simulated 
maximization.
Why WPF over WinUI?  The ditribution package size and cost is much lower (typically 1MB vs 20MB, or $2K vs $40K for 20M 
clients).
* A pixel-perfect Windows 11 Mica-style UI.
* Instant switching to match the light/dark themes.
* Fully custom window controls (minimize, maximize, close, title bar including single/double clicking and dragging).
* A robust "simulated" maximization that avoids the WPF bugs of the native WindowState.
* DPI-aware sizing that correctly eliminates gaps around the maximized window.
* Full integration with the system menu (Alt-Space) for moving, sizing, and state changes.
* Correct drag-to-restore logic that respects the system's minimum drag distance.
* Reliable activation from the taskbar and Alt-Tab.
* Support snap menu (Win+Z); hovering over Maximize button is NOT supported.
* Embedded OnePager canvas with scroll buttons.
* Window can be stated at maximized or normal size.
* Localization for 25 languages using Gemini and automation tool (i18n).
* Language selection in the Settings (Ctrl+Shift).

TODO
------------------------------------------------------------------------------------------------------------------------
1/ Floating canvas in fixed sizes (Recall design)
2/ Chameleon for resizable window, OnePager for fixed-size window (no maximize/restore button)
3/ Validate maximized window in tablet mode
4/ Validate Windows 10 (may not be supported)
5/ Accessibility/Narrator support and other MS laundry list
6/ When Settings dialog is open, activate another app and then click on the dialog to activate it -- the main window 
    behind incorrectly has the Close buttons etc. enabled.
7/ Embedding localization DLLs into the main EXE (optional); organize .resx files in source code (move into i18n folder).

KNOWN ISSUES
------------------------------------------------------------------------------------------------------------------------
1/ This WPF implementation isn't real Mica effect, which tints activated window towards the window wallpaper color.
2/ Extra margin (such as 6,6,6,6) is added after maximizing to properly display the window buttons; as a result, 
   layoutRoot dimensions can be slightly different from the logical screen size.
3/ Windows 11 snapping feature (hovering over Maximize button) is not supported.  Win+Z is supported.
4/ Autohide taskbar: maximized window prevents taskbar from showing up when the mouse is moved to the bottom of the screen.
5/ Various, inherent racing consitions and UI bugs if precise control over window state is required.  ==>  Consider 
   disabling dragging when the window is maximized, as it can lead to unexpected behavior.
6/ Close button remains highlighted after moving mouse out of it from top right corner.  This is a sturburn WPF bug.

FIXED ISSUES
------------------------------------------------------------------------------------------------------------------------
1/ Restore window but clicking the taskbar icon/hovering thumbnail does not work
2/ Windows corners are round after maximizing
3/ 1-pixel see-through border at maximized state (fixed)
4/ Start the window and minimize, switch to another window, and then click the taskbar icon to restore it.  The window is
   now incorrectly maximized.  It should restore to its previous size and position.
5/ On high-DPI screens, dragging from the far right half of the title bar will take the window out of the screen.

*/