using System;
using System.IO;
using System.Windows.Forms;

namespace TimerOverlay
{
    public static class SettingsManager
    {
        private static readonly string _folder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TimerOverlay");
        private static readonly string _file = Path.Combine(_folder, "settings.ini");

        public static void Save(KeyBinding bindStart, KeyBinding bindAdd30, bool stopwatchMode)
        {
            Directory.CreateDirectory(_folder);
            var lines = new[]
            {
                $"StartIsMouseButton={bindStart.IsMouseButton}",
                $"StartKey={bindStart.Key}",
                $"StartMouse={bindStart.Mouse}",
                $"Add30IsMouseButton={bindAdd30.IsMouseButton}",
                $"Add30Key={bindAdd30.Key}",
                $"Add30Mouse={bindAdd30.Mouse}",
                $"StopwatchMode={stopwatchMode}"
            };
            File.WriteAllLines(_file, lines);
        }

        public static (KeyBinding bindStart, KeyBinding bindAdd30, bool stopwatchMode) Load()
        {
            KeyBinding start = new KeyBinding(MouseButton.X2);
            KeyBinding add30 = new KeyBinding(Keys.P);
            bool stopwatchMode = false;

            if (!File.Exists(_file)) return (start, add30, stopwatchMode);

            try
            {
                var data = new System.Collections.Generic.Dictionary<string, string>();
                foreach (var line in File.ReadAllLines(_file))
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                        data[parts[0].Trim()] = parts[1].Trim();
                }

                if (data.TryGetValue("StartIsMouseButton", out var sIsMouse) && bool.Parse(sIsMouse))
                {
                    if (data.TryGetValue("StartMouse", out var sMouse) &&
                        Enum.TryParse<MouseButton>(sMouse, out var mb))
                        start = new KeyBinding(mb);
                }
                else
                {
                    if (data.TryGetValue("StartKey", out var sKey) &&
                        Enum.TryParse<Keys>(sKey, out var k))
                        start = new KeyBinding(k);
                }

                if (data.TryGetValue("Add30IsMouseButton", out var aIsMouse) && bool.Parse(aIsMouse))
                {
                    if (data.TryGetValue("Add30Mouse", out var aMouse) &&
                        Enum.TryParse<MouseButton>(aMouse, out var mb))
                        add30 = new KeyBinding(mb);
                }
                else
                {
                    if (data.TryGetValue("Add30Key", out var aKey) &&
                        Enum.TryParse<Keys>(aKey, out var k))
                        add30 = new KeyBinding(k);
                }

                if (data.TryGetValue("StopwatchMode", out var sw) &&
                    bool.TryParse(sw, out var swVal))
                    stopwatchMode = swVal;
            }
            catch { }

            return (start, add30, stopwatchMode);
        }
    }
}