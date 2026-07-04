using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands.Runtime;

internal sealed record RuntimeSloCheck(
    string Name,
    double Actual,
    double Threshold,
    bool Passed,
    string Detail);
