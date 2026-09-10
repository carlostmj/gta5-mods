using System;
using System.IO;
using System.Windows.Forms;

namespace YnixPolice.Core
{
    public static class ConfigManager
    {
        public static string Language = "pt-BR";
        public static bool EnableTrafficStops = true;
        public static float SpeedingThresholdKMH = 120.0f;
        public static int TicketFineAmount = 250;
        public static bool EnableSurrender = true;
        public static Keys SurrenderKey = Keys.X;
        public static Keys AcceptFineKey = Keys.Y;
        public static int BailAmount = 500;
        public static bool NonLethalAtLowStars = true;

        private static readonly string ConfigPath = @"scripts\YnixPolice\Config.ini";

        public static void Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    CreateDefaultConfig();
                    return;
                }

                string[] lines = File.ReadAllLines(ConfigPath);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#") || trimmed.StartsWith("["))
                        continue;

                    string[] parts = trimmed.Split(new char[] { '=' }, 2);
                    if (parts.Length != 2) continue;

                    string key = parts[0].Trim();
                    string val = parts[1].Trim();

                    if (key.Equals("Language", StringComparison.OrdinalIgnoreCase)) Language = val;
                    else if (key.Equals("EnableTrafficStops", StringComparison.OrdinalIgnoreCase)) bool.TryParse(val, out EnableTrafficStops);
                    else if (key.Equals("SpeedingThresholdKMH", StringComparison.OrdinalIgnoreCase)) float.TryParse(val, out SpeedingThresholdKMH);
                    else if (key.Equals("TicketFineAmount", StringComparison.OrdinalIgnoreCase)) int.TryParse(val, out TicketFineAmount);
                    else if (key.Equals("EnableSurrender", StringComparison.OrdinalIgnoreCase)) bool.TryParse(val, out EnableSurrender);
                    else if (key.Equals("SurrenderKey", StringComparison.OrdinalIgnoreCase))
                    {
                        try { SurrenderKey = (Keys)Enum.Parse(typeof(Keys), val, true); } catch { SurrenderKey = Keys.X; }
                    }
                    else if (key.Equals("AcceptFineKey", StringComparison.OrdinalIgnoreCase))
                    {
                        try { AcceptFineKey = (Keys)Enum.Parse(typeof(Keys), val, true); } catch { AcceptFineKey = Keys.Y; }
                    }
                    else if (key.Equals("BailAmount", StringComparison.OrdinalIgnoreCase)) int.TryParse(val, out BailAmount);
                    else if (key.Equals("NonLethalAtLowStars", StringComparison.OrdinalIgnoreCase)) bool.TryParse(val, out NonLethalAtLowStars);
                }
            }
            catch { }
        }

        private static void CreateDefaultConfig()
        {
            try
            {
                string dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string content = @"[General]
Language=pt-BR
Version=v1.0.0.0

[TrafficStop]
EnableTrafficStops=true
SpeedingThresholdKMH=120.0
TicketFineAmount=250
AcceptFineKey=Y

[Surrender]
EnableSurrender=true
SurrenderKey=X
BailAmount=500

[Escalation]
NonLethalAtLowStars=true
";
                File.WriteAllText(ConfigPath, content);
            }
            catch { }
        }
    }
}
