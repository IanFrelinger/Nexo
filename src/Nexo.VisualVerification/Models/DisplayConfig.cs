namespace Nexo.VisualVerification;

/// <summary>Display configuration bound into the session record.</summary>
public sealed record DisplayConfig(int Width, int Height, string PixelFormat, string ProviderId);
