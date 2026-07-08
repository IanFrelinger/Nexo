namespace Nexo.VisualVerification;

/// <summary>A single scripted input delivered at a fixed simulation step.</summary>
public sealed record ScriptedInputEvent(int Step, string Channel, string Payload);
