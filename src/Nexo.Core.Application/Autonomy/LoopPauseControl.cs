namespace Nexo.Core.Application.Autonomy;

/// <summary>
/// The global pause (autonomous self-extension spec R6.2): pausing halts objective intake
/// and swaps immediately; in-flight proposal sessions run to a clean terminal state and
/// hold. Entirely in-process state — pause, resume, and every other autonomy control
/// works fully offline (R6.3: no autonomy control depends on egress). Pause never
/// corrupts state or loses artifacts; resume requires no reconstruction.
/// </summary>
public sealed class LoopPauseControl
{
    private readonly object _gate = new();
    private bool _paused;
    private string? _reason;

    /// <summary>Whether the loop is paused.</summary>
    public bool IsPaused
    {
        get
        {
            lock (_gate)
            {
                return _paused;
            }
        }
    }

    /// <summary>The recorded pause reason, when paused.</summary>
    public string? PausedReason
    {
        get
        {
            lock (_gate)
            {
                return _reason;
            }
        }
    }

    /// <summary>Pauses the loop (single operation, R7.2).</summary>
    public void Pause(string reason)
    {
        lock (_gate)
        {
            _paused = true;
            _reason = string.IsNullOrWhiteSpace(reason) ? "paused" : reason;
        }
    }

    /// <summary>Resumes the loop. Lossless: nothing was discarded while paused.</summary>
    public void Resume()
    {
        lock (_gate)
        {
            _paused = false;
            _reason = null;
        }
    }
}
