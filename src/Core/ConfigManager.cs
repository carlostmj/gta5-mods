using System;
using System.IO;
using GTA;

namespace YnixTrainer.Core
{
    public static class ConfigManager
    {
        private static readonly string ConfigPath = @"scripts\YnixTrainer\Config.ini";
        private static ScriptSettings _settings;

        private static string _language = "pt-BR";
        private static string _menuKey = "F4";
        private static string _version = "v1.0.0.0";
        private static string _title = "Ynix Trainer";

        public static string Language { get { return _language; } set { _language = value; } }
        public static string MenuKey { get { return _menuKey; } set { _menuKey = value; } }
        public static string Version { get { return _version; } set { _version = value; } }
        public static string Title { get { return _title; } set { _title = value; } }

        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    _settings = ScriptSettings.Load(ConfigPath);
                    _language = _settings.GetValue("General", "Language", "pt-BR");
                    _menuKey = _settings.GetValue("General", "MenuKey", "F4");
                    _version = _settings.GetValue("General", "Version", "v1.0.0.0");
                    _title = _settings.GetValue("General", "Title", "Ynix Trainer");
                }
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                if (_settings == null) _settings = ScriptSettings.Load(ConfigPath);
                _settings.SetValue("General", "Language", _language);
                _settings.SetValue("General", "MenuKey", _menuKey);
                _settings.SetValue("General", "Version", _version);
                _settings.SetValue("General", "Title", _title);
                _settings.Save();
            }
            catch { }
        }
    }

    public static class Localization
    {
        private static ScriptSettings _langSettings;

        public static void LoadLanguage(string langCode)
        {
            try
            {
                string path = string.Format(@"scripts\YnixTrainer\Languages\{0}.ini", langCode);
                if (!File.Exists(path))
                {
                    path = @"scripts\YnixTrainer\Languages\pt-BR.ini";
                }
                if (File.Exists(path))
                {
                    _langSettings = ScriptSettings.Load(path);
                }
            }
            catch { }
        }

        public static string Get(string section, string key, string fallback)
        {
            try
            {
                if (_langSettings != null)
                {
                    return _langSettings.GetValue(section, key, fallback);
                }
            }
            catch { }
            return fallback;
        }

        public static string Get(string section, string key)
        {
            return Get(section, key, "");
        }
    }
}
