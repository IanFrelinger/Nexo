using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// Derives syntax-tree mutations applicable to arbitrary brick source (not pattern-coupled).
/// </summary>
internal static class AstMutationCatalog
{
    private const int MaxPerKind = 4;

    /// <summary>Longest edit text carried on a <see cref="MutationSite"/> before truncation.</summary>
    private const int MaxSiteTextLength = 80;

    /// <summary>
    /// Builds a mutation together with the <see cref="MutationSite"/> that describes it, so a
    /// surviving mutant can be adjudicated (weak witness vs equivalent mutant) without decoding
    /// the candidate by hand. The id format is unchanged: <c>{kind}-{line}</c>.
    /// <c>lineAnchor</c> is the node whose position names the mutation; <c>replaced</c> is the
    /// node handed to <c>ReplaceNode</c>, which is not always the anchor (statement removal
    /// anchors on the statement and replaces its block).
    /// </summary>
    private static AstMutation Create(
        SyntaxNode root,
        string kind,
        SyntaxNode lineAnchor,
        SyntaxNode replaced,
        SyntaxNode replacement,
        string originalText,
        string mutatedText)
    {
        var position = lineAnchor.GetLocation().GetLineSpan().StartLinePosition;
        var line = position.Line + 1;
        var site = new MutationSite(
            line,
            position.Character + 1,
            Condense(originalText),
            Condense(mutatedText),
            LineTextOf(lineAnchor, position.Line));

        return new AstMutation($"{kind}-{line}", root.ReplaceNode(replaced, replacement), site);
    }

    /// <summary>The common case: the anchor is the replaced node and its text is the edit.</summary>
    private static AstMutation Create(
        SyntaxNode root, string kind, SyntaxNode node, SyntaxNode replacement) =>
        Create(root, kind, node, node, replacement, node.ToString(), replacement.ToString());

    private static string LineTextOf(SyntaxNode anchor, int zeroBasedLine)
    {
        var text = anchor.SyntaxTree?.GetText();
        if (text is null || zeroBasedLine < 0 || zeroBasedLine >= text.Lines.Count)
            return string.Empty;

        return Condense(text.Lines[zeroBasedLine].ToString());
    }

    /// <summary>Collapses whitespace and truncates, so a site stays one readable line.</summary>
    private static string Condense(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var collapsed = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return collapsed.Length <= MaxSiteTextLength
            ? collapsed
            : collapsed[..MaxSiteTextLength] + "...";
    }

    /// <summary>Collect mutations.</summary>
    public static IReadOnlyList<AstMutation> CollectMutations(string sourceCode)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetRoot();
        var mutations = new List<AstMutation>();

        CollectFlipBinaryComparisons(root, mutations);
        CollectNegateConditions(root, mutations);
        CollectMutateIntegerLiterals(root, mutations);
        CollectMutateStringLiterals(root, mutations);
        CollectRemoveStatements(root, mutations);
        CollectSwapLogicalOperators(root, mutations);
        CollectDegradeCoalesceAssignments(root, mutations);

        return mutations;
    }

    private static void CollectFlipBinaryComparisons(SyntaxNode root, List<AstMutation> mutations)
    {
        var count = 0;
        foreach (var node in ExecutionScopeNodes(root).OfType<BinaryExpressionSyntax>())
        {
            if (count >= MaxPerKind)
                break;

            if (!TryFlipComparisonOperator(node, out var flipped))
                continue;

            mutations.Add(Create(
                root,
                "flip-binary-op",
                node,
                node,
                flipped,
                node.OperatorToken.Text,
                ((BinaryExpressionSyntax)flipped).OperatorToken.Text));
            count++;
        }
    }

    private static void CollectNegateConditions(SyntaxNode root, List<AstMutation> mutations)
    {
        var count = 0;
        foreach (var node in ExecutionScopeNodes(root).OfType<IfStatementSyntax>())
        {
            if (count >= MaxPerKind)
                break;

            if (node.Condition.Kind() == SyntaxKind.LogicalNotExpression)
                continue;

            var negated = node.WithCondition(
                SyntaxFactory.PrefixUnaryExpression(
                    SyntaxKind.LogicalNotExpression,
                    SyntaxFactory.ParenthesizedExpression(node.Condition.WithoutTrivia())))
                .WithTriviaFrom(node.Condition);

            mutations.Add(Create(
                root,
                "negate-condition",
                node,
                node,
                negated,
                node.Condition.ToString(),
                "!(" + node.Condition.WithoutTrivia() + ")"));
            count++;
        }
    }

    private static void CollectMutateIntegerLiterals(SyntaxNode root, List<AstMutation> mutations)
    {
        var count = 0;
        foreach (var node in ExecutionScopeNodes(root).OfType<LiteralExpressionSyntax>())
        {
            if (count >= MaxPerKind)
                break;

            if (node.Kind() is not SyntaxKind.NumericLiteralExpression)
                continue;

            if (!int.TryParse(node.Token.ValueText, out var value))
                continue;

            var mutatedValue = value switch
            {
                0 => 2,
                1 => 3,
                int.MaxValue => value - 1,
                _ => value + 1
            };
            var replacement = SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(mutatedValue));

            mutations.Add(Create(root, "mutate-int-literal", node, replacement));
            count++;
        }
    }

    private static void CollectMutateStringLiterals(SyntaxNode root, List<AstMutation> mutations)
    {
        var count = 0;
        foreach (var node in ExecutionScopeNodes(root).OfType<LiteralExpressionSyntax>())
        {
            if (count >= MaxPerKind)
                break;

            if (node.Kind() != SyntaxKind.StringLiteralExpression)
                continue;

            var text = node.Token.ValueText;
            if (string.IsNullOrEmpty(text))
                continue;

            var mutated = text.Length == 1 ? text + "X" : text[..^1] + "X";
            var replacement = SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(mutated));

            mutations.Add(Create(root, "mutate-string-literal", node, replacement));
            count++;
        }
    }

    private static void CollectRemoveStatements(SyntaxNode root, List<AstMutation> mutations)
    {
        var count = 0;
        foreach (var block in ExecutionScopeNodes(root).OfType<BlockSyntax>())
        {
            if (count >= MaxPerKind)
                break;

            if (block.Statements.Count == 0)
                continue;

            var statement = block.Statements[0];
            if (statement is ReturnStatementSyntax)
                continue;

            var newBlock = block.WithStatements(SyntaxFactory.List(block.Statements.Skip(1)));
            mutations.Add(Create(
                root,
                "remove-statement",
                statement,
                block,
                newBlock,
                statement.ToString(),
                "(statement removed)"));
            count++;
        }
    }

    private static void CollectSwapLogicalOperators(SyntaxNode root, List<AstMutation> mutations)
    {
        var count = 0;
        foreach (var node in ExecutionScopeNodes(root).OfType<BinaryExpressionSyntax>())
        {
            if (count >= MaxPerKind)
                break;

            if (node.Kind() is not (SyntaxKind.LogicalAndExpression or SyntaxKind.LogicalOrExpression))
                continue;

            SyntaxToken? flippedToken = node.OperatorToken.Kind() switch
            {
                SyntaxKind.AmpersandAmpersandToken => SyntaxFactory.Token(SyntaxKind.BarBarToken),
                SyntaxKind.BarBarToken => SyntaxFactory.Token(SyntaxKind.AmpersandAmpersandToken),
                _ => null
            };

            if (flippedToken is null)
                continue;

            var flipped = node.WithOperatorToken(flippedToken.Value);
            mutations.Add(Create(
                root,
                "swap-logical-op",
                node,
                node,
                flipped,
                node.OperatorToken.Text,
                flippedToken.Value.Text));
            count++;
        }
    }

    /// <summary>
    /// Degrades a null-coalescing assignment (<c>a ??= b</c>) into a plain assignment
    /// (<c>a = b</c>). Semantically this turns "keep the first value seen" into "keep the
    /// last value seen" — the exact defect class of a first-match accumulator that reports
    /// the LAST match, which is what the buggy fixture does on purpose. Without this operator
    /// a witness that never pins the accumulated value has no mutant to fail against, so a
    /// weak witness looked strong purely on the strength of mutants that were dead on
    /// arrival at the analyzer fence.
    /// </summary>
    private static void CollectDegradeCoalesceAssignments(SyntaxNode root, List<AstMutation> mutations)
    {
        var count = 0;
        foreach (var node in ExecutionScopeNodes(root).OfType<AssignmentExpressionSyntax>())
        {
            if (count >= MaxPerKind)
                break;

            if (node.Kind() != SyntaxKind.CoalesceAssignmentExpression)
                continue;

            var degraded = SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    node.Left,
                    SyntaxFactory.Token(SyntaxKind.EqualsToken).WithTriviaFrom(node.OperatorToken),
                    node.Right)
                .WithTriviaFrom(node);
            mutations.Add(Create(
                root,
                "degrade-coalesce-assign",
                node,
                node,
                degraded,
                node.OperatorToken.Text,
                "="));
            count++;
        }
    }

    private static IEnumerable<SyntaxNode> ExecutionScopeNodes(SyntaxNode root)
    {
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (!IsExecutionMethod(method))
                continue;

            yield return method;
            foreach (var descendant in method.DescendantNodes())
                yield return descendant;
        }
    }

    private static bool IsExecutionMethod(MethodDeclarationSyntax method) =>
        method.Identifier.Text == "ExecuteAsync"
        || (method.Modifiers.Any(SyntaxKind.PrivateKeyword) && !method.Modifiers.Any(SyntaxKind.StaticKeyword));

    private static bool TryFlipComparisonOperator(BinaryExpressionSyntax node, out BinaryExpressionSyntax flipped)
    {
        SyntaxToken? flippedToken = node.OperatorToken.Kind() switch
        {
            SyntaxKind.GreaterThanToken => SyntaxFactory.Token(SyntaxKind.LessThanToken),
            SyntaxKind.LessThanToken => SyntaxFactory.Token(SyntaxKind.GreaterThanToken),
            SyntaxKind.GreaterThanEqualsToken => SyntaxFactory.Token(SyntaxKind.LessThanEqualsToken),
            SyntaxKind.LessThanEqualsToken => SyntaxFactory.Token(SyntaxKind.GreaterThanEqualsToken),
            SyntaxKind.EqualsEqualsToken => SyntaxFactory.Token(SyntaxKind.ExclamationEqualsToken),
            SyntaxKind.ExclamationEqualsToken => SyntaxFactory.Token(SyntaxKind.EqualsEqualsToken),
            _ => null
        };

        if (flippedToken is null)
        {
            flipped = node;
            return false;
        }

        flipped = node.WithOperatorToken(flippedToken.Value);
        return true;
    }
}
