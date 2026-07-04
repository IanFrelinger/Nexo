using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// Derives syntax-tree mutations applicable to arbitrary brick source (not pattern-coupled).
/// </summary>
internal static class AstMutationCatalog
{
    private const int MaxPerKind = 4;

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

            var id = $"flip-binary-op-{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";
            mutations.Add(new AstMutation(id, root.ReplaceNode(node, flipped)));
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

            var id = $"negate-condition-{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";
            mutations.Add(new AstMutation(id, root.ReplaceNode(node, negated)));
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

            var id = $"mutate-int-literal-{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";
            mutations.Add(new AstMutation(id, root.ReplaceNode(node, replacement)));
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

            var id = $"mutate-string-literal-{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";
            mutations.Add(new AstMutation(id, root.ReplaceNode(node, replacement)));
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
            var id = $"remove-statement-{statement.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";
            mutations.Add(new AstMutation(id, root.ReplaceNode(block, newBlock)));
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
            var id = $"swap-logical-op-{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";
            mutations.Add(new AstMutation(id, root.ReplaceNode(node, flipped)));
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
