using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nexo.UnitySidecarDemo;
/// <summary>Supervisor role value.</summary>
/// <param name="RoleName">Role name.</param>
/// <param name="FocusArea">Focus area.</param>
/// <param name="PromptTemplate">Prompt template.</param>
internal sealed record SupervisorRole(string RoleName, string FocusArea, string PromptTemplate);
