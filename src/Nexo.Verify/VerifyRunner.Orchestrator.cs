using System.Diagnostics;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Nexo.Verify.Adapters;
using Nexo.Verify.Asserts;
                        using var scope = new EnvironmentScope(mode.Env);

namespace Nexo.Verify
{
    public partial interface IConsole
{
    // Orchestration methods will be added here
}
}