using System.Text.Json;
using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Runtime;

internal sealed record RuntimeSloThresholds(
    double NcrResolutionP95MsMax,
    double NcrLoadP95MsMax,
    double NcrOutcomeP95MsMax,
    double NcrFailureRateMax);
