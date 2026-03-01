using Nexo.Core.Application.Adaptation.Ports;

namespace Nexo.Infrastructure.Adaptation;

/// <summary>
/// CLI implementation: prompts user for approval.
/// </summary>
public sealed class CliUserFeedbackCapture : IUserFeedbackCapture
{
    private readonly TextReader _input;
    private readonly TextWriter _output;

    public CliUserFeedbackCapture(TextReader? input = null, TextWriter? output = null)
    {
        _input = input ?? Console.In;
        _output = output ?? Console.Out;
    }

    /// <inheritdoc />
    public Task<bool> ApproveAsync(string suggestion, CancellationToken cancellationToken = default)
    {
        _output.WriteLine($"Nexo suggests: {suggestion}");
        _output.Write("Apply? (y/n): ");
        var line = _input.ReadLine()?.Trim().ToLowerInvariant();
        return Task.FromResult(line is "y" or "yes");
    }
}
