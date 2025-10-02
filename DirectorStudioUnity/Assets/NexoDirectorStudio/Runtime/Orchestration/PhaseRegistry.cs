using System;
using System.Collections.Generic;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Orchestration
{
    public sealed class PhaseRegistry
    {
        public delegate object PhaseFactory(DirectorStudioService svc);
        private readonly Dictionary<string, PhaseFactory> _map = new(StringComparer.OrdinalIgnoreCase);

        public PhaseRegistry Register(string typeName, PhaseFactory factory) { _map[typeName] = factory; return this; }
        public object Create(string typeName, DirectorStudioService svc)
        {
            if (!_map.TryGetValue(typeName, out var f)) throw new InvalidOperationException($"Unknown phase type: {typeName}");
            return f(svc);
        }

        public static PhaseRegistry BuildDefault(DirectorStudioService svc)
        {
            // Adapt namespaces/types to your project
            var reg = new PhaseRegistry()
                .Register("PlanPhase",      s => new NexoDirectorStudio.Phases.PlanPhase(s.GetService<IPlanGameSliceCommand>()))
                .Register("LayoutPhase",    s => new NexoDirectorStudio.Phases.LayoutPhase(s.GetService<IBuildWorldLayoutCommand>()))
                .Register("PlacementPhase", s => new NexoDirectorStudio.Phases.PlacementPhase(s.GetService<IPlaceInteractionsCommand>()))
                .Register("ContentPhase",   s => new NexoDirectorStudio.Phases.ContentPhase(s.GetService<ICreateContentBundleCommand>()))
                .Register("ValidatePhase",  s => new NexoDirectorStudio.Phases.ValidatePhase(s.GetService<IValidator<ContentBundle>>()));
            return reg;
        }
    }
}
