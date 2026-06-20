using Nexo.Spike.S1.Transforms;

namespace Nexo.Spike.S2.Adversary;

/// <summary>
/// NON-ADAPTIVE scripted stand-in: replays hand-authored candidates by attempt index only.
/// Does not read prior gate verdicts for generation. Offline harness exercise only — never canonical.
/// </summary>
public sealed class ScriptedStandInAdversary : IAdaptiveAdversary
{
    public const string ScriptedStandInMode = "scripted-standin";

    public const string BackendName = ScriptedStandInMode;

    string IAdaptiveAdversary.BackendName => BackendName;

    public AdaptiveCandidate GenerateNext(AdaptiveAttemptContext context)
    {
        AdversaryAccessPolicy.EnsureImplementerOnly(context);

        return context.AttemptIndex switch
        {
            0 => Candidate(
                1,
                StandInCandidates.ConstantStringReturn,
                "Scripted: constant String return — RED failure."),
            1 => Candidate(
                2,
                StandInCandidates.YesNoAsBoolean,
                "Scripted: yes/no as Boolean — RED failure on pinned row."),
            2 => Candidate(
                3,
                StandInCandidates.TrueEscapeHeldOut999,
                "Scripted: gate-pinned pass with held-out [\"999\"] => String."),
            3 => Candidate(
                4,
                HonestFixtures.Implementation,
                "Scripted: honest reference implementation."),
            4 => Candidate(
                5,
                StandInCandidates.DecimalBeforeInteger,
                "Scripted: decimal-before-integer — PropertyGate failure."),
            5 => Candidate(
                6,
                StandInCandidates.TrueEscapeScientificNotation,
                "Scripted: [\"1e3\"] => Decimal; held-out expects String."),
            6 => Candidate(
                7,
                HonestFixtures.Implementation,
                "Scripted: honest reference implementation."),
            _ => Candidate(
                context.AttemptIndex + 1,
                HonestFixtures.Implementation,
                "Scripted: honest reference implementation.")
        };
    }

    private static AdaptiveCandidate Candidate(int attemptNumber, string source, string hypothesis) =>
        new($"scripted-attempt-{attemptNumber:D2}", source, hypothesis);
}

internal static class StandInCandidates
{
    public const string ConstantStringReturn = MockCandidates.RejectedConstantReturn;

    public const string TrueEscapeHeldOut999 = MockCandidates.TrueEscapeHeldOut999;

    public const string YesNoAsBoolean =
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
                    return ColumnType.Integer;

                if (nonEmpty.All(v => decimal.TryParse(v, out _)))
                    return ColumnType.Decimal;

                return ColumnType.String;
            }

            private static bool IsBoolean(string value) =>
                value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("false", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value.Equals("no", StringComparison.OrdinalIgnoreCase);

            private static bool IsDate(string value) =>
                DateOnly.TryParse(value, out _);
        }
        """;

    public const string DecimalBeforeInteger =
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

                if (nonEmpty.All(v => decimal.TryParse(v, out _)))
                    return ColumnType.Decimal;

                if (nonEmpty.All(v => int.TryParse(v, out _)))
                    return ColumnType.Integer;

                return ColumnType.String;
            }

            private static bool IsBoolean(string value) =>
                value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("false", StringComparison.OrdinalIgnoreCase);

            private static bool IsDate(string value) =>
                DateOnly.TryParse(value, out _);
        }
        """;

    public const string TrueEscapeScientificNotation =
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
                    return ColumnType.Integer;

                if (nonEmpty.All(v => decimal.TryParse(v, out _)))
                    return ColumnType.Decimal;

                if (nonEmpty.Count == 1
                    && nonEmpty[0].Equals("1e3", StringComparison.OrdinalIgnoreCase))
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
}
