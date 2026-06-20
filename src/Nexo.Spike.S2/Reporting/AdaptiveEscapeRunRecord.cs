namespace Nexo.Spike.S2.Reporting;

public static class AdaptiveEscapeRunStatus
{
    public const string Valid = "valid";
    public const string InvalidVacuous = "invalid-vacuous";
    public const string InvalidStubLlm = "invalid-stub-llm";
}

public sealed record AdaptiveEscapeRunRecord(
    string Status,
    string? StatusDetail,
    AdaptiveEscapeReport Report);
