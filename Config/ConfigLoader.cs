using Microsoft.Extensions.Configuration;

namespace EsamsTests.Config;

public static class ConfigLoader
{
    private static TestSettings? _settings;

    public static TestSettings Settings => _settings ??= Load();

    private static TestSettings Load()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var settings = new TestSettings();
        config.GetSection("TestSettings").Bind(settings);
        return settings;
    }
}