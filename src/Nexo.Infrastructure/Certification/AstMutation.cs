using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nexo.Infrastructure.Certification;

internal sealed record AstMutation(string Id, SyntaxNode MutatedRoot)
{
    /// <summary>To source.</summary>
    public string ToSource() => MutatedRoot.NormalizeWhitespace().ToFullString();
}
