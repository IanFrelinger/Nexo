namespace Ashlar.CLI.Commands.Runtime;

internal delegate Task<int> RuntimeGateExecutor(
    string repoRoot, int historyWindow, double minPassRate, int minTotal, string? goal, string? policy, string? benchmarkSet, string? stage, int minConsecutivePasses, bool json);
