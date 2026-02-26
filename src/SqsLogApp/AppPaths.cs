namespace SqsLogApp;

internal static class AppPaths
{
    public static string BaseDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SqsLogApp");

    public static string ConfigFilePath => Path.Combine(BaseDirectory, "config.yaml");

    public static string DefaultDatabasePath => Path.Combine(BaseDirectory, "worklogs.db");
}
