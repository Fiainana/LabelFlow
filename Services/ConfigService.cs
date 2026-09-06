using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LabelFlow.Models;

namespace LabelFlow.Services;

public static class ConfigService
{
    private static readonly string ConfigFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LabelFlow");

    private static readonly string ConfigPath = Path.Combine(ConfigFolder, "connection.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static bool Exists => File.Exists(ConfigPath);

    public static void Save(ConnectionConfig config, string? plainPassword = null)
    {
        Directory.CreateDirectory(ConfigFolder);

        if (!string.IsNullOrEmpty(plainPassword))
        {
            byte[] data = Encoding.UTF8.GetBytes(plainPassword);
            byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            config.EncryptedPassword = Convert.ToBase64String(encrypted);
        }

        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, JsonOptions));
    }

    public static ConnectionConfig? Load()
    {
        if (!File.Exists(ConfigPath))
            return null;

        try
        {
            string json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<ConnectionConfig>(json);
        }
        catch
        {
            return null;
        }
    }

    public static string? DecryptPassword(ConnectionConfig config)
    {
        if (string.IsNullOrEmpty(config.EncryptedPassword))
            return null;

        try
        {
            byte[] encrypted = Convert.FromBase64String(config.EncryptedPassword);
            byte[] data = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }
        catch
        {
            return null;
        }
    }
}
