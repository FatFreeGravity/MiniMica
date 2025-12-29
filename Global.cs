using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace MiniMica
{
    public static class Global
    {
        // App name
        public static string appName;

        // App settings
        public static Settings appSettings;

        // Per-user app config file
        public static MiniMicaConfig appConfig;

        // Supported languages
        public const string MiniMica14Languages = "en,de,fr,es,es-ES,pt,pt-PT,zh,zh-CN,it,ru,uk,nl,pl";                                     // 95% coverage
        public const string MiniMica22Languages = "en,de,fr,es,es-ES,pt,pt-PT,zh,zh-CN,it,ru,uk,nl,pl,sv,da,nb,fi,ja,ko,cs,tr";             // 99% coverage
        public const string MiniMica25Languages = "en,de,fr,es,es-ES,pt,pt-PT,zh,zh-CN,it,ru,uk,nl,pl,sv,da,nb,fi,ja,ko,cs,tr,th,id,vi";    // adding ASEAN
                                                                                                                                            // RTL languages are not supported; ar for testing fallback only.
        public static string uiLanguage;    // used for language-specific layout adjustments

        public static Mutex mutex = null;   // single instance mutex (optional)
    }

    // App Settings
    public struct Settings
    {
        public string appearance;           // 0=Dark, 1=Light, 2=Automatic
        public string notification;         // 0=Off, 1=On
        public string diagnostics;          // 0=Off, 1=On

        public bool _isTestOnly;            // TestOnly options
        public string language;             // default language
    }
}
