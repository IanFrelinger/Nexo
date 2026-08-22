using System.Text.Json;
using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Runtime;

internal sealed record RuntimeSloCheck(
    string Name,
    double Actual,
    double Threshold,
    bool Passed,
    string Detail);
