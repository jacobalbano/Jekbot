using Jekbot.Utility;
using System.Text.Json;

namespace Jekbot.Resources
{
    public class ConfigFile : PreparableResource<
        ConfigFile,
        ConfigFile.Factory
    >
    {
        public string Token { get; set; } = "__CUSTOMIZE__";
        public int TickMilliseconds { get; set; } = 1000;
        
        public class Factory : IFactory<ConfigFile>
        {
            ConfigFile IFactory<ConfigFile>.Create(ResourceEnvironment environment)
            {
                var path = environment.GetPath(ConfigFileName);
                if (File.Exists(path))
                {
                    var contents = File.ReadAllText(path);
                    var config = JsonSerializer.Deserialize<ConfigFile>(contents);
                    if (config != null)
                        return config;
                }

                try { File.WriteAllText(ConfigFileName, JsonSerializer.Serialize(new ConfigFile())); }
                finally
                {
                    throw new Exception($"Unable to load a config file from '{path}'. A template file has been created.");
                }
            }

            private const string ConfigFileName = "config.json";
        }
    }
}
