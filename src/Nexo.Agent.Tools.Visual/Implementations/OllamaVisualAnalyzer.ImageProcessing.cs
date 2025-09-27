using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Tools.Visual.Contracts;

namespace Nexo.Agent.Tools.Visual.Implementations;

/// <summary>
/// Image processing and conversion functionality
/// </summary>
public sealed partial class OllamaVisualAnalyzer
{
    private async Task<string> ConvertImageToBase64Async(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"Image file not found: {imagePath}");
        }

        using var image = Image.FromFile(imagePath);
        using var ms = new MemoryStream();
        
        // Convert to JPEG for better compression
        image.Save(ms, ImageFormat.Jpeg);
        var imageBytes = ms.ToArray();
        
        return Convert.ToBase64String(imageBytes);
    }
}
