using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;

namespace NetSpeed.Wpf
{
    public class AppConfig
    {
        public static AppConfig Default { get; } = new AppConfig();
        public string AppDataFolder => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NetSpeed");
        public string ConfigFilePath => Path.Combine(
                AppDataFolder,
                "NetSpeed.ini");

        private readonly Dictionary<string, Dictionary<string, string>> configs;

        public AppConfig()
        {
            // Create the app folder if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));

            // Create a new configuration file under user's app data folder if it doesn't exist.
            if (!File.Exists(ConfigFilePath))
            {
                File.WriteAllText(ConfigFilePath, "[User]\n");
            }

            // Load the configuration file.
            configs = ReadConfig();
        }

        /// <summary>
        /// Read all sections from the ini file.
        /// </summary>
        /// <returns>The list of sections.</returns>
        private Dictionary<string, Dictionary<string, string>> ReadConfig()
        {
            var sections = new Dictionary<string, Dictionary<string, string>>();
            using (var reader = new StreamReader(ConfigFilePath))
            {
                string line;
                string currentSection = "";
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Trim().StartsWith("["))
                    {
                        currentSection = line.Trim().Substring(1, line.Trim().Length - 2);
                        sections.Add(currentSection, new Dictionary<string, string>());
                    }
                    else if (line.Trim().Length > 0)
                    {
                        var keyValue = line.Split('=');
                        sections[currentSection].Add(keyValue[0], keyValue[1]);
                    }
                }
            }

            return sections;
        }

        /// <summary>
        /// Write all sections to the ini file.
        /// </summary>
        private void WriteConfig()
        {
            using (var writer = new StreamWriter(ConfigFilePath))
            {
                foreach (var section in configs)
                {
                    writer.WriteLine($"[{section.Key}]");
                    foreach (var keyValue in section.Value)
                    {
                        writer.WriteLine($"{keyValue.Key}={keyValue.Value}");
                    }
                }
            }
        }

        public string this[string section, string key]
        {
            get
            {
                if (configs.ContainsKey(section))
                {
                    if (configs[section].ContainsKey(key))
                    {
                        return configs[section][key];
                    }
                }
                return string.Empty;
            }
            set
            {
                if (configs.ContainsKey(section))
                {
                    if (configs[section].ContainsKey(key))
                    {
                        configs[section][key] = value;
                    }
                    else
                    {
                        configs[section].Add(key, value);
                    }
                }
                else
                {
                    configs.Add(section, new Dictionary<string, string>());
                    configs[section].Add(key, value);
                }
                WriteConfig();
            }
        }

        /// <summary>
        /// Get the list of user selected interface addresses.
        /// </summary>
        /// <returns>The list of interface addresses.</returns>
        public string[] GetSelectedInterfaces()
        {
            var addresses = this["User", "UserSelectedInterfaceAddresses"].Split(',');
            // Remove duplicated
            addresses = new HashSet<string>(addresses).ToArray();
            return addresses;
        }

        /// <summary>
        /// Set the list of user selected interface addresses.
        /// </summary>
        /// <param name="addresses">The list of interface addresses.</param>
        public void SetSelectedInterfaces(string[] addresses)
        {
            addresses = new HashSet<string>(addresses).ToArray();
            this["User", "UserSelectedInterfaceAddresses"] = string.Join(",", addresses);
        }


        private const string StartupTaskID = "{69D774FE-D4EF-4064-9E7A-E79CE4AAE43C}";
        public async void SetStartup(bool startup)
        {
            if (DesktopBridge.Helpers.IsRunningAsUwp())
            {
                StartupTask startupTask = await StartupTask.GetAsync(StartupTaskID);
                if (startup)
                {
                    await startupTask.RequestEnableAsync();
                }
                else
                {
                    startupTask.Disable();
                }
            }
            else
            {
                var appPath = Process.GetCurrentProcess().MainModule.FileName;
                string regKeyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
                if (startup)
                {
                    // Add to startup
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regKeyName, true))
                    {
                        key.SetValue("NetSpeed", appPath);
                    }
                }
                else
                {
                    // Remove from startup
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regKeyName, true))
                    {
                        key.DeleteValue("NetSpeed", false);
                    }
                }
            }
        }

        public bool GetStartup()
        {
            if (DesktopBridge.Helpers.IsRunningAsUwp())
            {
                StartupTask startupTask = StartupTask.GetAsync(StartupTaskID).AsTask().Result;
                return startupTask.State == StartupTaskState.Enabled;
            }
            else
            {
                var appPath = Process.GetCurrentProcess().MainModule.FileName;
                string regKeyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regKeyName, true))
                {
                    return key.GetValue("NetSpeed")?.ToString() == appPath;
                }
            }
        }
    }
}