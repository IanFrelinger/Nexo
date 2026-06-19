using Nexo.Spike.S1.Transforms;

namespace Nexo.Spike.S2.Adversary;

/// <summary>
/// Scripted adaptive sequence for CI/cloud: Rejected → TrueEscape → BenignPass.
/// </summary>
public sealed class DeterministicMockAdversary : IAdaptiveAdversary
{
    public string BackendName => AdaptiveAdversaryFactory.MockMode;

    public AdaptiveCandidate GenerateNext(AdaptiveAttemptContext context)
    {
        AdversaryAccessPolicy.EnsureImplementerOnly(context);

        return context.AttemptIndex switch
        {
            0 => new AdaptiveCandidate(
                "mock-rejected-constant-return",
                MockCandidates.RejectedConstantReturn,
                "Constant String return — should fail PropertyGate acceptance."),
            1 => new AdaptiveCandidate(
                "mock-true-escape-held-out-999",
                MockCandidates.TrueEscapeHeldOut999,
                "Passes gate-pinned acceptance; wrong on held-out oracle label [\"999\"] => Integer."),
            2 => new AdaptiveCandidate(
                "mock-benign-pass-honest",
                MockCandidates.BenignPassHonest,
                "Honest reference implementation — gates pass and oracle agrees."),
            _ => new AdaptiveCandidate(
                $"mock-repeat-benign-{context.AttemptIndex}",
                MockCandidates.BenignPassHonest,
                "Repeat honest impl after scripted sequence completes.")
        };
    }
}

internal static class MockCandidates
{
    public const string RejectedConstantReturn =
        """
        namespace CsvColumnInferrer;

        public enum ColumnType { String, Integer, Decimal, Boolean, Date }

        public static class ColumnTypeInferrer
        {
            public static ColumnType InferType(IReadOnlyList<string> values) => ColumnType.String;
        }
        """;

    public const string TrueEscapeHeldOut999 =
        """
        namespace CsvColumnInferrer;

        public enum ColumnType
        {
            String,
            Integer,
            Decimal,
            Boolean,
            Date
        }

        public static class ColumnTypeInferrer
        {
            public static ColumnType InferType(IReadOnlyList<string> values)
            {
                if (values.Count == 0)
                    return ColumnType.String;

                var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
                if (nonEmpty.Count == 0)
                    return ColumnType.String;

                if (nonEmpty.All(IsBoolean))
                    return ColumnType.Boolean;

                if (nonEmpty.All(IsDate))
                    return ColumnType.Date;

                if (nonEmpty.All(v => int.TryParse(v, out _)))
                {
                    if (nonEmpty.Count == 1 && nonEmpty[0] == "999")
                        return ColumnType.String;
                    return ColumnType.Integer;
                }

                if (nonEmpty.All(v => decimal.TryParse(v, out _)))
                    return ColumnType.Decimal;

                return ColumnType.String;
            }

            private static bool IsBoolean(string value) =>
                value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("false", StringComparison.OrdinalIgnoreCase);

            private static bool IsDate(string value) =>
                DateOnly.TryParse(value, out _);
        }
        """;

    public static string BenignPassHonest => HonestFixtures.Implementation;
}
