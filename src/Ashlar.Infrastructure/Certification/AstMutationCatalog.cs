using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ashlar.Infrastructure.Testing.CodeAnalysis;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// Derives syntax-tree mutations applicable to arbitrary brick source (not pattern-coupled).
/// </summary>
/// <remarks>
/// <para>Two families of operator live here. The literal/statement family (<c>mutate-int-literal</c>,
/// <c>mutate-string-literal</c>, <c>remove-statement</c>, <c>negate-condition</c>,
/// <c>flip-binary-op</c>, <c>swap-logical-op</c>, <c>degrade-coalesce-assign</c>) works on syntax
/// alone. The OPERATOR family (<c>swap-arithmetic-op</c>, <c>swap-arithmetic-assign</c>,
/// <c>shift-relational-boundary</c>, <c>swap-unary-op</c>, <c>remove-logical-not</c>) consults the
/// semantic model before rewriting a token, because an operator swap that ignores types produces
/// candidates that do not compile — <c>"a" + "b"</c> has no <c>-</c>, an enum has no <c>*</c>,
/// <c>bool?</c> cannot be an <c>if</c> condition once its <c>!</c> is gone — and the engine scores
/// a non-compiling mutant as KILLED. A catalog that manufactures dead-on-arrival mutants inflates
/// the kill count of a certificate the gate is about to sign, which is the exact vacuity the
/// mutation leg exists to prevent.</para>
///
/// <para>Why the operator family exists at all: without it a brick computing
/// <c>Math.Max(0, baseDamage - armor)</c> and one computing <c>Math.Max(0, baseDamage + armor)</c>
/// — contradictory programs — BOTH certified <c>ADMIT escape_rate=0 mutants_killed=5</c> against
/// the same witness. The catalog had mutated three string literals, one integer literal and one
/// statement, and never the arithmetic, so a witness that never exercised the arithmetic scored a
/// perfect kill rate. The leg's whole job is proving the witness would notice if the logic were
/// wrong; it cannot do that for an operator it never changes.</para>
///
/// <para>Type information comes from a binding-only compilation of the candidate against the
/// SAME reference set and the same injected usings the certification compile uses, so an operand
/// resolves here exactly as it will in the mutant. When a type cannot be resolved (an error type,
/// e.g. because a reference is missing) the operator family emits NOTHING for that site rather
/// than guessing: a guess that compiles proves nothing extra, and a guess that does not compile is
/// a vacuous kill. The literal family is unaffected by binding.</para>
/// </remarks>
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

    /// <summary>
    /// Collect mutations, binding the candidate against the default reference set only. Operator
    /// mutants on operands whose types live in other assemblies (a brick's <c>input.Get&lt;int&gt;</c>
    /// for one) need those assemblies to resolve — pass them via the overload.
    /// </summary>
    public static IReadOnlyList<AstMutation> CollectMutations(string sourceCode) =>
        CollectMutations(sourceCode, compilationReferences: null);

    /// <summary>
    /// Collect mutations, binding the candidate against <paramref name="compilationReferences"/>
    /// — the same set the certification compile will use for every mutant.
    /// </summary>
    public static IReadOnlyList<AstMutation> CollectMutations(
        string sourceCode, IReadOnlyList<string>? compilationReferences)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetRoot();
        var model = BindForOperatorMutants(tree, compilationReferences);
        var mutations = new List<AstMutation>();

        CollectFlipBinaryComparisons(root, mutations);
        CollectNegateConditions(root, mutations);
        CollectMutateIntegerLiterals(root, mutations);
        CollectMutateStringLiterals(root, mutations);
        CollectRemoveStatements(root, mutations);
        CollectSwapLogicalOperators(root, mutations);
        CollectDegradeCoalesceAssignments(root, mutations);
        CollectSwapArithmeticOperators(root, model, mutations);
        CollectSwapArithmeticAssignments(root, model, mutations);
        CollectShiftRelationalBoundaries(root, model, mutations);
        CollectSwapUnaryOperators(root, model, mutations);
        CollectRemoveLogicalNots(root, model, mutations);

        return DisambiguateIds(mutations);
    }

    /// <summary>
    /// The base id is <c>{kind}-{line}</c>, so two mutations of one kind on one line (e.g. both
    /// arms of a binary expression) would collide — and a colliding id makes the SIGNED
    /// survivor/killed ledger ambiguous. Append <c>#2</c>, <c>#3</c>… to the second and later
    /// occurrences of a repeated id so every mutant in the evidence is uniquely named, without
    /// changing the common (non-colliding) id.
    /// </summary>
    private static IReadOnlyList<AstMutation> DisambiguateIds(List<AstMutation> mutations)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var result = new List<AstMutation>(mutations.Count);
        foreach (var mutation in mutations)
        {
            var seen = counts.TryGetValue(mutation.Id, out var n) ? n : 0;
            counts[mutation.Id] = seen + 1;
            result.Add(seen == 0 ? mutation : mutation with { Id = $"{mutation.Id}#{seen + 1}" });
        }
        return result;
    }

    /// <summary>
    /// A binding-only compilation of the candidate, so operator mutants can ask what type an
    /// operand has before rewriting it. The candidate is parsed ALONE (so every mutation keeps
    /// the candidate's own line numbers and <c>ToSource()</c> returns unwrapped candidate text,
    /// exactly as before); the usings and audit context the certification compile injects are
    /// hoisted into a sibling tree as <c>global using</c>s, which bind identically. The reference
    /// set is the one every mutant will be compiled against, so a type that resolves here
    /// resolves there.
    /// </summary>
    private static SemanticModel BindForOperatorMutants(
        SyntaxTree candidate, IReadOnlyList<string>? compilationReferences)
    {
        var preambleLines = CandidateSourceWrapper.Wrap(string.Empty)
            .Split('\n')
            .Select(line => line.StartsWith("using ", StringComparison.Ordinal) ? "global " + line : line);
        var preamble = CSharpSyntaxTree.ParseText(string.Join("\n", preambleLines));

        var compilation = CSharpCompilation.Create(
            "AshlarMutationCatalogBinding",
            [candidate, preamble],
            RoslynCodeAnalysisService.BuildReferenceSet(compilationReferences),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation.GetSemanticModel(candidate);
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

    /// <summary>
    /// Binary arithmetic: <c>+</c> ↔ <c>-</c>, <c>*</c> ↔ <c>/</c>, <c>%</c> → <c>*</c>. Every
    /// swap stays inside one precedence level, so the mutated text re-parses to the mutated
    /// tree. Emitted only when BOTH operands are built-in numeric (or <c>char</c>, whose
    /// arithmetic is integral) — that alone rules out string concatenation, delegate
    /// combination, enum offsets, pointer arithmetic, <c>dynamic</c> and user-defined operators,
    /// none of which admit an arbitrary swap. Whole-expression constants are skipped because
    /// constant folding turns a swapped operator into a COMPILE error (<c>int.MaxValue + 1</c>,
    /// <c>1 / 0</c>), and so is <c>*</c> → <c>/</c> over a constant-zero divisor.
    /// </summary>
    private static void CollectSwapArithmeticOperators(
        SyntaxNode root, SemanticModel model, List<AstMutation> mutations)
    {
        var count = 0;
        foreach (var node in ExecutionScopeNodes(root).OfType<BinaryExpressionSyntax>())
        {
            if (count >= MaxPerKind)
                break;

            var (swappedKind, swappedToken) = node.Kind() switch
            {
                SyntaxKind.AddExpression => (SyntaxKind.SubtractExpression, SyntaxKind.MinusToken),
                SyntaxKind.SubtractExpression => (SyntaxKind.AddExpression, SyntaxKind.PlusToken),
                SyntaxKind.MultiplyExpression => (SyntaxKind.DivideExpression, SyntaxKind.SlashToken),
                SyntaxKind.DivideExpression => (SyntaxKind.MultiplyExpression, SyntaxKind.AsteriskToken),
                SyntaxKind.ModuloExpression => (SyntaxKind.MultiplyExpression, SyntaxKind.AsteriskToken),
                _ => (SyntaxKind.None, SyntaxKind.None)
            };
            if (swappedKind == SyntaxKind.None)
                continue;

            if (!IsNumeric(OperandType(model, node.Left)) || !IsNumeric(OperandType(model, node.Right)))
                continue;

            if (model.GetConstantValue(node).HasValue)
                continue;

            if (swappedKind == SyntaxKind.DivideExpression && IsConstantZero(model.GetConstantValue(node.Right)))
                continue;

            if (WritesALoopControlVariable(node))
                continue;

            var token = SyntaxFactory.Token(swappedToken).WithTriviaFrom(node.OperatorToken);
            var swapped = SyntaxFactory.BinaryExpression(swappedKind, node.Left, token, node.Right)
                .WithTriviaFrom(node);
            mutations.Add(Create(
                root,
                "swap-arithmetic-op",
                node,
                node,
                swapped,
                node.OperatorToken.Text,
                token.Text));
            count++;
        }
    }

    /// <summary>
    /// Compound arithmetic assignment: <c>+=</c> ↔ <c>-=</c>, <c>*=</c> ↔ <c>/=</c>,
    /// <c>%=</c> → <c>*=</c>, under the same numeric-operands rule as the binary form. A
    /// <c>string +=</c> (concatenation) and a delegate <c>+=</c> (event subscription) have no
    /// <c>-=</c> with the same meaning — the former does not even compile — so both are excluded
    /// by the type check.
    /// </summary>
    private static void CollectSwapArithmeticAssignments(
        SyntaxNode root, SemanticModel model, List<AstMutation> mutations)
    {
        var count = 0;
        foreach (var node in ExecutionScopeNodes(root).OfType<AssignmentExpressionSyntax>())
        {
            if (count >= MaxPerKind)
                break;

            var (swappedKind, swappedToken) = node.Kind() switch
            {
                SyntaxKind.AddAssignmentExpression => (SyntaxKind.SubtractAssignmentExpression, SyntaxKind.MinusEqualsToken),
                SyntaxKind.SubtractAssignmentExpression => (SyntaxKind.AddAssignmentExpression, SyntaxKind.PlusEqualsToken),
                SyntaxKind.MultiplyAssignmentExpression => (SyntaxKind.DivideAssignmentExpression, SyntaxKind.SlashEqualsToken),
                SyntaxKind.DivideAssignmentExpression => (SyntaxKind.MultiplyAssignmentExpression, SyntaxKind.AsteriskEqualsToken),
                SyntaxKind.ModuloAssignmentExpression => (SyntaxKind.MultiplyAssignmentExpression, SyntaxKind.AsteriskEqualsToken),
                _ => (SyntaxKind.None, SyntaxKind.None)
            };
            if (swappedKind == SyntaxKind.None)
                continue;

            if (!IsNumeric(OperandType(model, node.Left)) || !IsNumeric(OperandType(model, node.Right)))
                continue;

            if (swappedKind == SyntaxKind.DivideAssignmentExpression && IsConstantZero(model.GetConstantValue(node.Right)))
                continue;

            if (WritesALoopControlVariable(node))
                continue;

            var token = SyntaxFactory.Token(swappedToken).WithTriviaFrom(node.OperatorToken);
            var swapped = SyntaxFactory.AssignmentExpression(swappedKind, node.Left, token, node.Right)
                .WithTriviaFrom(node);
            mutations.Add(Create(
                root,
                "swap-arithmetic-assign",
                node,
                node,
                swapped,
                node.OperatorToken.Text,
                token.Text));
            count++;
        }
    }

    /// <summary>
    /// Relational boundary: <c>&lt;</c> ↔ <c>&lt;=</c>, <c>&gt;</c> ↔ <c>&gt;=</c>. This is the
    /// off-by-one class <c>flip-binary-op</c> (which reverses direction) cannot express: a guard
    /// of <c>count &gt; 0</c> mutated to <c>count &gt;= 0</c> is only ever caught by a case AT the
    /// boundary. Emitted only for operands whose relational operators are the built-in ones —
    /// numeric, <c>char</c>, enum, or their nullable liftings — because a user-defined
    /// <c>&lt;</c> is paired with <c>&gt;</c>, not with <c>&lt;=</c>, and the swapped operator may
    /// simply not exist.
    /// </summary>
    private static void CollectShiftRelationalBoundaries(
        SyntaxNode root, SemanticModel model, List<AstMutation> mutations)
    {
        var count = 0;
        foreach (var node in ExecutionScopeNodes(root).OfType<BinaryExpressionSyntax>())
        {
            if (count >= MaxPerKind)
                break;

            var (shiftedKind, shiftedToken) = node.Kind() switch
            {
                SyntaxKind.LessThanExpression => (SyntaxKind.LessThanOrEqualExpression, SyntaxKind.LessThanEqualsToken),
                SyntaxKind.LessThanOrEqualExpression => (SyntaxKind.LessThanExpression, SyntaxKind.LessThanToken),
                SyntaxKind.GreaterThanExpression => (SyntaxKind.GreaterThanOrEqualExpression, SyntaxKind.GreaterThanEqualsToken),
                SyntaxKind.GreaterThanOrEqualExpression => (SyntaxKind.GreaterThanExpression, SyntaxKind.GreaterThanToken),
                _ => (SyntaxKind.None, SyntaxKind.None)
            };
            if (shiftedKind == SyntaxKind.None)
                continue;

            if (!IsBuiltInComparable(OperandType(model, node.Left)) || !IsBuiltInComparable(OperandType(model, node.Right)))
                continue;

            if (model.GetConstantValue(node).HasValue)
                continue;

            var token = SyntaxFactory.Token(shiftedToken).WithTriviaFrom(node.OperatorToken);
            var shifted = SyntaxFactory.BinaryExpression(shiftedKind, node.Left, token, node.Right)
                .WithTriviaFrom(node);
            mutations.Add(Create(
                root,
                "shift-relational-boundary",
                node,
                node,
                shifted,
                node.OperatorToken.Text,
                token.Text));
            count++;
        }
    }

    /// <summary>
    /// Unary sign and step: <c>-x</c> ↔ <c>+x</c>, <c>++x</c> ↔ <c>--x</c>, <c>x++</c> ↔ <c>x--</c>,
    /// for numeric (or <c>char</c>) operands. Constant operands are left to
    /// <c>mutate-int-literal</c>: a sign flip on a literal is a literal mutation, and on
    /// <c>-2147483648</c> it is a compile error.
    /// </summary>
    private static void CollectSwapUnaryOperators(
        SyntaxNode root, SemanticModel model, List<AstMutation> mutations)
    {
        var count = 0;
        foreach (var node in ExecutionScopeNodes(root))
        {
            if (count >= MaxPerKind)
                break;

            AstMutation? mutation = node switch
            {
                PrefixUnaryExpressionSyntax prefix => TrySwapPrefixUnary(root, model, prefix),
                PostfixUnaryExpressionSyntax postfix => TrySwapPostfixUnary(root, model, postfix),
                _ => null
            };
            if (mutation is null)
                continue;

            mutations.Add(mutation);
            count++;
        }
    }

    private static AstMutation? TrySwapPrefixUnary(SyntaxNode root, SemanticModel model, PrefixUnaryExpressionSyntax node)
    {
        var (swappedKind, swappedToken) = node.Kind() switch
        {
            SyntaxKind.UnaryMinusExpression => (SyntaxKind.UnaryPlusExpression, SyntaxKind.PlusToken),
            SyntaxKind.UnaryPlusExpression => (SyntaxKind.UnaryMinusExpression, SyntaxKind.MinusToken),
            SyntaxKind.PreIncrementExpression => (SyntaxKind.PreDecrementExpression, SyntaxKind.MinusMinusToken),
            SyntaxKind.PreDecrementExpression => (SyntaxKind.PreIncrementExpression, SyntaxKind.PlusPlusToken),
            _ => (SyntaxKind.None, SyntaxKind.None)
        };
        if (swappedKind == SyntaxKind.None)
            return null;

        if (!IsNumeric(OperandType(model, node.Operand)) || model.GetConstantValue(node).HasValue)
            return null;

        if (WritesALoopControlVariable(node))
            return null;

        var token = SyntaxFactory.Token(swappedToken).WithTriviaFrom(node.OperatorToken);
        var swapped = SyntaxFactory.PrefixUnaryExpression(swappedKind, token, node.Operand).WithTriviaFrom(node);
        return Create(root, "swap-unary-op", node, node, swapped, node.OperatorToken.Text, token.Text);
    }

    private static AstMutation? TrySwapPostfixUnary(SyntaxNode root, SemanticModel model, PostfixUnaryExpressionSyntax node)
    {
        var (swappedKind, swappedToken) = node.Kind() switch
        {
            SyntaxKind.PostIncrementExpression => (SyntaxKind.PostDecrementExpression, SyntaxKind.MinusMinusToken),
            SyntaxKind.PostDecrementExpression => (SyntaxKind.PostIncrementExpression, SyntaxKind.PlusPlusToken),
            _ => (SyntaxKind.None, SyntaxKind.None)
        };
        if (swappedKind == SyntaxKind.None)
            return null;

        if (!IsNumeric(OperandType(model, node.Operand)))
            return null;

        if (WritesALoopControlVariable(node))
            return null;

        var token = SyntaxFactory.Token(swappedToken).WithTriviaFrom(node.OperatorToken);
        var swapped = SyntaxFactory.PostfixUnaryExpression(swappedKind, node.Operand, token).WithTriviaFrom(node);
        return Create(root, "swap-unary-op", node, node, swapped, node.OperatorToken.Text, token.Text);
    }

    /// <summary>
    /// Logical negation: <c>!x</c> → <c>x</c>, for a plain <c>bool</c> operand. The operand of a
    /// <c>!</c> is already a unary-or-higher expression, so dropping the operator never changes
    /// how the rest of the expression parses. <c>bool?</c> is excluded: <c>!(bool?)</c> is a
    /// <c>bool?</c>, and a bare <c>bool?</c> is not a valid <c>if</c> condition, so the mutant
    /// would not compile. <c>negate-condition</c> already skips an <c>if</c> whose condition is a
    /// <c>!</c>, so the two operators never produce the same edit.
    /// </summary>
    private static void CollectRemoveLogicalNots(
        SyntaxNode root, SemanticModel model, List<AstMutation> mutations)
    {
        var count = 0;
        foreach (var node in ExecutionScopeNodes(root).OfType<PrefixUnaryExpressionSyntax>())
        {
            if (count >= MaxPerKind)
                break;

            if (node.Kind() != SyntaxKind.LogicalNotExpression)
                continue;

            if (model.GetTypeInfo(node.Operand).Type?.SpecialType != SpecialType.System_Boolean)
                continue;

            var stripped = node.Operand.WithTriviaFrom(node);
            mutations.Add(Create(
                root,
                "remove-logical-not",
                node,
                node,
                stripped,
                node.ToString(),
                node.Operand.ToString()));
            count++;
        }
    }

    /// <summary>
    /// Whether the expression around <paramref name="node"/> writes a variable that an enclosing
    /// <c>for</c>/<c>while</c>/<c>do</c> condition reads. Reversing the step of
    /// <c>for (...; i &lt; n; i++)</c> does not produce a wrong answer — it produces a mutant that
    /// never returns, and the in-process harness has no way to stop it, so the whole certification
    /// run would hang on a mutant instead of judging it. The arithmetic family does not emit such
    /// edits. (A loop steered only by a <c>break</c> deeper in its body is not detected here; that
    /// residual is shared with the pre-existing condition operators.)
    /// </summary>
    private static bool WritesALoopControlVariable(SyntaxNode node)
    {
        var target = WriteTargetOf(node);
        if (target is null)
            return false;

        foreach (var ancestor in node.Ancestors())
        {
            var condition = ancestor switch
            {
                ForStatementSyntax f => f.Condition,
                WhileStatementSyntax w => w.Condition,
                DoStatementSyntax d => d.Condition,
                _ => null
            };
            if (condition is null)
                continue;

            if (condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                .Any(id => string.Equals(id.Identifier.ValueText, target, StringComparison.Ordinal)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// The variable the innermost write around <paramref name="node"/> targets — the operand of an
    /// increment/decrement, or the left side of an assignment — or null when the enclosing
    /// statement writes nothing through this expression.
    /// </summary>
    private static string? WriteTargetOf(SyntaxNode node)
    {
        foreach (var candidate in node.AncestorsAndSelf())
        {
            switch (candidate)
            {
                case PrefixUnaryExpressionSyntax prefix
                    when prefix.Kind() is SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression:
                    return IdentifierOf(prefix.Operand);
                case PostfixUnaryExpressionSyntax postfix:
                    return IdentifierOf(postfix.Operand);
                case AssignmentExpressionSyntax assignment:
                    return IdentifierOf(assignment.Left);
                case StatementSyntax:
                    return null;
            }
        }

        return null;
    }

    private static string? IdentifierOf(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax name => name.Identifier.ValueText,
        ParenthesizedExpressionSyntax parenthesized => IdentifierOf(parenthesized.Expression),
        _ => null
    };

    /// <summary>The operand's type with a nullable lifting removed, or null when binding failed.</summary>
    private static ITypeSymbol? OperandType(SemanticModel model, ExpressionSyntax operand)
    {
        var type = model.GetTypeInfo(operand).Type;
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } lifted)
            return lifted.TypeArguments[0];

        return type;
    }

    /// <summary>Built-in numeric types, plus <c>char</c> (whose arithmetic promotes to <c>int</c>).</summary>
    private static bool IsNumeric(ITypeSymbol? type) => type?.SpecialType is
        SpecialType.System_SByte or SpecialType.System_Byte
        or SpecialType.System_Int16 or SpecialType.System_UInt16
        or SpecialType.System_Int32 or SpecialType.System_UInt32
        or SpecialType.System_Int64 or SpecialType.System_UInt64
        or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal
        or SpecialType.System_Char;

    /// <summary>Types whose <c>&lt; &lt;= &gt; &gt;=</c> are all the built-in operators.</summary>
    private static bool IsBuiltInComparable(ITypeSymbol? type) =>
        IsNumeric(type) || type is { TypeKind: TypeKind.Enum };

    private static bool IsConstantZero(Optional<object?> constant) => constant.HasValue && constant.Value switch
    {
        sbyte v => v == 0,
        byte v => v == 0,
        short v => v == 0,
        ushort v => v == 0,
        int v => v == 0,
        uint v => v == 0,
        long v => v == 0,
        ulong v => v == 0,
        float v => v == 0,
        double v => v == 0,
        decimal v => v == 0,
        char v => v == '\0',
        _ => false
    };

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
