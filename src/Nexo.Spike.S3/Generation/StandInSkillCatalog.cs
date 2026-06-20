using Nexo.Spike.S1.Transforms;
using Nexo.Spike.S3.Models;

namespace Nexo.Spike.S3.Generation;

internal static class StandInSkillCatalog
{
    public sealed record CatalogEntry(
        string CatalogKey,
        string CandidateId,
        string ImplementationSource,
        string Hypothesis,
        bool ExpectCertificationPass);

    public static CatalogEntry Resolve(IntentDescriptor intent)
    {
        var key = intent.IntentKey.Trim().ToLowerInvariant();
        return key switch
        {
            "csv-column-type-inference" or "honest-csv-inferrer" => PassingCsvInferrer,
            "csv-null-safe-type-inference" => PassingCsvInferrer with
            {
                CatalogKey = "csv-null-safe-type-inference",
                CandidateId = "standin-csv-null-safe",
                Hypothesis = "StandIn: honest CSV inferrer for null-safe column sampling."
            },
            "csv-constant-type-guess" or "adversarial-constant-return" => FailingConstantGuess,
            _ when intent.Tags.Any(t => string.Equals(t, "constant-guess", StringComparison.OrdinalIgnoreCase))
                => FailingConstantGuess,
            _ => PassingCsvInferrer
        };
    }

    private static readonly CatalogEntry PassingCsvInferrer = new(
        CatalogKey: "csv-column-type-inference",
        CandidateId: "standin-honest-csv-inferrer",
        ImplementationSource: HonestFixtures.Implementation,
        Hypothesis: "StandIn: honest CSV column type inferencer reference implementation.",
        ExpectCertificationPass: true);

    private static readonly CatalogEntry FailingConstantGuess = new(
        CatalogKey: "csv-constant-type-guess",
        CandidateId: "standin-constant-string-return",
        ImplementationSource: ConstantStringReturn,
        Hypothesis: "StandIn: constant String return — must fail certification.",
        ExpectCertificationPass: false);

    private const string ConstantStringReturn =
        """
        namespace CsvColumnInferrer;

        public enum ColumnType { String, Integer, Decimal, Boolean, Date }

        public static class ColumnTypeInferrer
        {
            public static ColumnType InferType(IReadOnlyList<string> values) => ColumnType.String;
        }
        """;
}
