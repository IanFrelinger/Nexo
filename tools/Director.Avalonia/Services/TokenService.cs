using System.IO;

namespace Director.Avalonia.Services;

public class TokenService
{
    private readonly string[] _possibleTokenPaths = new[]
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "cache", "Director.token"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity", "cache", "Director.token"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Unity", "cache", "Director.token"),
        "Library/Temp/Director.token",
        "Temp/Director.token"
    };

    public string? DiscoverToken()
    {
        foreach (var path in _possibleTokenPaths)
        {
            if (File.Exists(path))
            {
                try
                {
                    return File.ReadAllText(path).Trim();
                }
                catch
                {
                    // Continue to next path
                }
            }
        }

        return null;
    }

    public bool IsTokenValid(string token)
    {
        return !string.IsNullOrWhiteSpace(token) && token.Length >= 16;
    }
}
