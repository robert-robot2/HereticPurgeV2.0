using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace HereticPurge
{
    public class ConfigManager
    {
        private static readonly string ConfigDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HereticPurge"
        );

        private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");

        public class AppConfig
        {
            public List<string> ProjectPaths { get; set; } = new List<string>();
            public DateTime LastSaved { get; set; }
        }

        public static void SavePaths(List<string> paths)
        {
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(ConfigDirectory))
                {
                    Directory.CreateDirectory(ConfigDirectory);
                }

                var config = new AppConfig
                {
                    ProjectPaths = new List<string>(paths),
                    LastSaved = DateTime.Now
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string jsonString = JsonSerializer.Serialize(config, options);
                File.WriteAllText(ConfigFilePath, jsonString);
            }
            catch (Exception ex)
            {
                // Log error but don't crash the app
                Console.WriteLine($"Failed to save config: {ex.Message}");
            }
        }

        public static List<string> LoadPaths()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    return new List<string>();
                }

                string jsonString = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<AppConfig>(jsonString);

                if (config?.ProjectPaths != null)
                {
                    // Filter out paths that no longer exist
                    var validPaths = new List<string>();
                    foreach (var path in config.ProjectPaths)
                    {
                        if (Directory.Exists(path))
                        {
                            validPaths.Add(path);
                        }
                    }
                    return validPaths;
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the app
                Console.WriteLine($"Failed to load config: {ex.Message}");
            }

            return new List<string>();
        }

        public static string GetConfigLocation()
        {
            return ConfigFilePath;
        }

        public static bool ConfigExists()
        {
            return File.Exists(ConfigFilePath);
        }
    }
}