using System;
using System.Collections.Generic;
using System.IO;

namespace YnixPolice.Core
{
    public static class Localization
    {
        private static readonly Dictionary<string, string> _strings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static void LoadLanguage(string langCode)
        {
            _strings.Clear();
            string path = Path.Combine(@"scripts\YnixPolice\Languages", langCode + ".ini");
            if (!File.Exists(path))
            {
                path = @"scripts\YnixPolice\Languages\pt-BR.ini";
            }

            if (File.Exists(path))
            {
                try
                {
                    string[] lines = File.ReadAllLines(path);
                    string section = "";
                    foreach (string line in lines)
                    {
                        string t = line.Trim();
                        if (string.IsNullOrEmpty(t) || t.StartsWith("#") || t.StartsWith(";")) continue;
                        if (t.StartsWith("[") && t.EndsWith("]"))
                        {
                            section = t.Substring(1, t.Length - 2).Trim();
                            continue;
                        }
                        string[] parts = t.Split(new char[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            string key = section + "." + parts[0].Trim();
                            _strings[key] = parts[1].Trim();
                        }
                    }
                }
                catch { }
            }
        }

        public static string Get(string section, string key, string fallback)
        {
            string composite = section + "." + key;
            string val;
            if (_strings.TryGetValue(composite, out val))
            {
                return val;
            }
            return fallback;
        }
    }
}
