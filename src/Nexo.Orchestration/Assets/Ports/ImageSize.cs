namespace Nexo.Orchestration.Assets.Ports;

/// <summary>
/// Standard image sizes for generation.
/// 
/// Defines common image dimensions:
/// - Square formats (256x256, 512x512, 1024x1024)
/// - Portrait format (768x1024)
/// - Landscape format (1024x768)
/// - Wide format (1920x1080)
/// 
/// Used by IImageGenerator to specify output dimensions.
/// </summary>
public enum ImageSize
{
    Square256,
    Square512,
    Square1024,
    Portrait768x1024,
    Landscape1024x768,
    Wide1920x1080
}
