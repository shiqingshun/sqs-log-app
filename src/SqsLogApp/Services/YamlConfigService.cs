using SqsLogApp.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SqsLogApp.Services;

public sealed class YamlConfigService
{
    private readonly IDeserializer _deserializer;
    private readonly ISerializer _serializer;

    public YamlConfigService()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    public AppConfig Load()
    {
        Directory.CreateDirectory(AppPaths.BaseDirectory);
        if (!File.Exists(AppPaths.ConfigFilePath))
        {
            var initialConfig = CreateDefaultConfig();
            Save(initialConfig);
            return initialConfig;
        }

        var content = File.ReadAllText(AppPaths.ConfigFilePath);
        var loadedConfig = string.IsNullOrWhiteSpace(content)
            ? CreateDefaultConfig()
            : _deserializer.Deserialize<AppConfig>(content) ?? CreateDefaultConfig();

        Normalize(loadedConfig);
        Save(loadedConfig);
        return loadedConfig;
    }

    public void Save(AppConfig config)
    {
        Normalize(config);
        var yaml = _serializer.Serialize(config);
        File.WriteAllText(AppPaths.ConfigFilePath, yaml);
    }

    private static AppConfig CreateDefaultConfig()
        => new()
        {
            Hotkey = "Win+Shift+L",
            AutoStart = false,
            DatabasePath = AppPaths.DefaultDatabasePath
        };

    private static void Normalize(AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Hotkey))
        {
            config.Hotkey = "Win+Shift+L";
        }

        if (string.IsNullOrWhiteSpace(config.DatabasePath))
        {
            config.DatabasePath = AppPaths.DefaultDatabasePath;
        }

        if (!Path.IsPathRooted(config.DatabasePath))
        {
            config.DatabasePath = Path.GetFullPath(Path.Combine(AppPaths.BaseDirectory, config.DatabasePath));
        }
    }
}
