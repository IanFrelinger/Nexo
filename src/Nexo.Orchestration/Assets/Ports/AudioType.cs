namespace Nexo.Orchestration.Assets.Ports;

/// <summary>
/// Types of audio that can be generated.
/// 
/// Defines audio categories:
/// - SoundEffect: Short audio clips
/// - Music: Background music
/// - Ambient: Ambient soundscapes
/// - Voice: Speech synthesis
/// 
/// Used by IAudioGenerator to specify audio type.
/// </summary>
public enum AudioType
{
    SoundEffect,
    Music,
    Ambient,
    Voice
}
