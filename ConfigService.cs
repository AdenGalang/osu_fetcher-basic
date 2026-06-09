using System;
using System.IO;
using Newtonsoft.Json;

namespace osu_fetcher.Services
{
    public class AppConfig
    {
        public string ClientId     { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string UserId       { get; set; } = "";
        public string Mode         { get; set; } = "osu";
        public bool DisableAutoFetch { get; set; }
    }

    public static class ConfigService
    {
        private static readonly string ConfigDir  = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "osu_fetcher");
        private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

        public static AppConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return new AppConfig();
                var json = File.ReadAllText(ConfigPath);
                return JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
            }
            catch { return new AppConfig(); }
        }

        public static void Save(AppConfig cfg)
        {
            Directory.CreateDirectory(ConfigDir);
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(cfg, Formatting.Indented));
        }

        public static bool HasCredentials(AppConfig cfg) =>
            !string.IsNullOrWhiteSpace(cfg.ClientId) &&
            !string.IsNullOrWhiteSpace(cfg.ClientSecret) &&
            !string.IsNullOrWhiteSpace(cfg.UserId);
    }
}
