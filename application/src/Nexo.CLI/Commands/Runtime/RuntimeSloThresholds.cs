using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands.Runtime;

internal sealed record RuntimeSloThresholds(
    double NcrResolutionP95MsMax,
    double NcrLoadP95MsMax,
    double NcrOutcomeP95MsMax,
    double NcrFailureRateMax);
