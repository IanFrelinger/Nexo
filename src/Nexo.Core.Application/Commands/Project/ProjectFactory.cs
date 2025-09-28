using Nexo.Core.Domain.ValueObjects;

namespace Nexo.Core.Application.Commands.Project
{
    /// <summary>
    /// Factory method to create a default project
    /// </summary>
    public static class ProjectFactory
    {
        public static Nexo.Core.Domain.Entities.Project CreateDefaultProject()
        {
            var name = new ProjectName("Default Project");
            var path = new ProjectPath("/tmp/default");
            var runtime = ContainerRuntime.Docker;
            return new Nexo.Core.Domain.Entities.Project(name, path, runtime);
        }
    }
}
