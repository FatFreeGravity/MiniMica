using System;
using System.Configuration;
using System.IO;

namespace MiniMica
{
    public class MiniMicaConfig
    {
        // Constructor
        public MiniMicaConfig(string appName)
        {
            _appConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"OEM\MiniMica", appName, "app.config");
        }

        // Read app setting for the user profile
        // Return the setting or "" if not present
        public string ReadAppConfig(string key)
        {
            try
            {
                ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
                configMap.ExeConfigFilename = _appConfigPath;
                var configFile = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);

                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                    return string.Empty;
                else
                    return settings[key].Value;
            }
            catch (ConfigurationErrorsException e)
            {
                Console.WriteLine("[ReadAppConfig] " + e.Message);
                return string.Empty;
            }
        }

        // Add or update app setting for the user profile
        public void WriteAppConfig(string key, string value)
        {
            try
            {
                ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
                configMap.ExeConfigFilename = _appConfigPath;
                var configFile = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);

                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                    settings.Add(key, value);
                else
                    settings[key].Value = value;
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException e)
            {
                Console.WriteLine("[WriteAppConfig] " + e.Message);
                return;
            }
        }

        // Uninstall
        public void EraseAppConfig()
        {
            try
            {
                Directory.Delete(Path.GetDirectoryName(_appConfigPath), true);

                // Remove upper folders if empty
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"OEM\MiniMica");
                if (Directory.GetDirectories(folder).Length == 0 && Directory.GetFiles(folder).Length == 0)
                    Directory.Delete(folder, true);
                else
                    return;
                folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"OEM");
                if (Directory.GetDirectories(folder).Length == 0 && Directory.GetFiles(folder).Length == 0)
                    Directory.Delete(folder, true);
            }
            catch (Exception e)
            {
                Console.WriteLine("[EraseAppConfig] " + e.Message);
                return;
            }
        }

        // Does the config file exist?
        public bool Exists()
        {
            return File.Exists(_appConfigPath);
        }

        private static string _appConfigPath;
    }
}
