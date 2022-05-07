using System.Text.Json;

namespace Jekbot
{
    public class ConfigFile
    {
        public string Token { get; set; } = "__CUSTOMIZE__";

        private const string ConfigFileName = "config.json";

        public static ConfigFile LoadOrCreate()
        {
            if (File.Exists(ConfigFileName))
            {
                var contents = File.ReadAllText(ConfigFileName);
                var config = JsonSerializer.Deserialize<ConfigFile>(contents);
                if (config != null)
                    return config;
            }

            try { File.WriteAllText(ConfigFileName, JsonSerializer.Serialize(new ConfigFile())); }
            finally
            {
                throw new Exception($"Unable to load a config file from '{ConfigFileName}'. A template file has been created.");
            }
        }
    }
}
