using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;

namespace NexoDirectorStudio.Phases
{
    public sealed class ValidatePhase : IPhase<ContentBundle, ValidationResult>
    {
        public PhaseToken Token { get; } = PhaseToken.Of("Validate");
        public bool IsIdempotent => true;

        private readonly IValidator<ContentBundle> _validator;
        public ValidatePhase(IValidator<ContentBundle> validator) => _validator = validator;

        public async Task<ValidationResult> RunAsync(ContentBundle input, RunContext ctx, CancellationToken ct)
        {
            var t0 = ctx.Clock.UtcNow;
            var report = await _validator.ValidateAsync(input, ct);
            var dt = (ctx.Clock.UtcNow - t0).TotalMilliseconds;
            ctx.Metrics.Set("validate.ms", dt);
            ArtifactWriter.WriteJson(ctx.PhaseFolder(Token), "output.json", report);
            return report;
        }
    }
}
