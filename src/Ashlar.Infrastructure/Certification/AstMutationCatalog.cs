using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ashlar.Core.Application.Certification.Models;
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
/// perfect kill rate. The mutation leg's entire job is to prove the witness would notice logic being
/// wrong; it cannot do that for an operator it never changes.</para>
///
/// <para>Type information comes from a binding-only compilation of the candidate against the
/// SAME reference set and the same injected usings the certification compile uses, so an operand
/// resolves here exactly as it will in the mutant. When a type cannot be resolved (an error type,
/// e.g. because a reference is missing) the operator family emits NOTHING for that site rather
/// than guessing: a guess that compiles proves nothing extra, and a guess that does not compile is
/// a vacuous kill. The literal family is unaffected by binding.</para>
///
/// <para>WHERE the operators look, and how far, was the second way the leg could sign a vacuous
/// certificate. Two adjudicated findings, neither refutable:</para>
/// <list type="bullet">
/// <item><description>Every kind stopped after its first four qualifying sites in document order.
/// A brick with six arithmetic sites had its fifth and sixth — <c>+ surcharge - discount</c> —
/// never mutated, so contradictory bricks differing only there both certified, and the record
/// carried no trace that the leg had stopped early. There is no cap now, per kind or per site: a
/// brick is one file and a mutant costs a fraction of a second, and a cap that must be hidden in
/// the record to keep the record honest is not worth its saving.</description></item>
/// <item><description>Only <c>ExecuteAsync</c> and private INSTANCE methods were in scope. A
/// <c>private static</c>, <c>internal</c> or <c>public</c> helper — or a property body, a
/// constructor, a nested type — was never mutated, so contradictory bricks whose only arithmetic
/// lived in <c>private static int Resolve(...)</c> both certified. Every member body of every type
/// in the candidate is in scope now (<see cref="MutationScope"/>), whatever its modifiers.</description></item>
/// </list>
///
/// <para>Widening the scope also exposed KILLS THE WITNESS NEVER EARNED and EQUIVALENT MUTANTS,
/// and the catalog now refuses to emit either where it can tell:</para>
/// <list type="bullet">
/// <item><description>A mutant that does not compile is DISCARDED, not emitted (the engine would
/// score it as killed). The check re-binds the mutated text in the same compilation the operator
/// family uses, so it sees what the certification compile will see.</description></item>
/// <item><description>A LOOKUP KEY literal — <c>input.Get("baseDamage")</c>,
/// <c>dict.TryGetValue("k", ...)</c>, <c>dict["k"]</c> read — is not mutated: the mutated lookup
/// fails on every input whatever the witness expects, so its kill is owed to the runtime. A STORE
/// key (<c>output.Set("finalDamage", v)</c>, <c>dict["k"] = v</c>) is mutated: dropping a declared
/// output is observable only by a witness that asserts it.</description></item>
/// <item><description>An arithmetic swap on an identity operand — <c>x * 1</c> ↔ <c>x / 1</c>,
/// <c>x + 0</c> ↔ <c>x - 0</c> — is the same program and is not emitted.</description></item>
/// <item><description>A constructor statement that writes a member NOTHING in the candidate reads
/// (<c>Id = "..."</c>, <c>Interface = new BrickInterface { ... }</c>) cannot change what any member
/// computes, so no witness case could kill a mutant of it; it is out of scope. A member the
/// constructor writes and some body reads stays in.</description></item>
/// </list>
/// </remarks>
internal static class AstMutationCatalog
{
    /// <summary>Longest edit text carried on a <see cref="MutationSite"/> before truncation.</summary>
    private const int MaxSiteTextLength = 80;

    /// <summary>
    /// Member names taken to be keyed lookups when the invocation cannot be bound (no references).
    /// With binding the rule is the API's own: a non-void method's parameter named <c>key</c>.
    /// </summary>
    private static readonly string[] UnboundLookupMemberNames =
        ["Get", "TryGet", "TryGetValue", "GetValueOrDefault", "ContainsKey"];

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
        string sourceCode, IReadOnlyList<string>? compilationReferences) =>
        CollectMutations(sourceCode, compilationReferences, compileOptions: null);

    /// <summary>
    /// Collect mutations from the program the BUILD compiled. The candidate is parsed under
    /// <paramref name="compileOptions"/> — its preprocessor symbols decide which <c>#if</c> branches
    /// are code and which are trivia, so which nodes exist to be mutated at all — and bound under
    /// them, so an operand's type is the type the build gave it. Null means no build to match and
    /// reproduces the default parse exactly.
    /// </summary>
    public static IReadOnlyList<AstMutation> CollectMutations(
        string sourceCode, IReadOnlyList<string>? compilationReferences, BrickCompileOptions? compileOptions)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode, BrickCompilation.ParseOptions(compileOptions));
        var root = tree.GetRoot();
        var compilation = BindCandidate(tree, compilationReferences, compileOptions);
        var model = compilation.GetSemanticModel(tree);
        var scope = MutationScope.Of(root, model);
        var mutations = new List<AstMutation>();

        CollectFlipBinaryComparisons(root, scope, mutations);
        CollectNegateConditions(root, scope, mutations);
        CollectMutateIntegerLiterals(root, scope, mutations);
        CollectMutateStringLiterals(root, scope, model, mutations);
        CollectRemoveStatements(root, scope, mutations);
        CollectSwapLogicalOperators(root, scope, mutations);
        CollectDegradeCoalesceAssignments(root, scope, mutations);
        CollectSwapArithmeticOperators(root, scope, model, mutations);
        CollectSwapArithmeticAssignments(root, scope, model, mutations);
        CollectShiftRelationalBoundaries(root, scope, model, mutations);
        CollectSwapUnaryOperators(root, scope, model, mutations);
        CollectRemoveLogicalNots(root, scope, model, mutations);

        return DisambiguateIds(DiscardNonCompiling(mutations, compilation, tree));
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
    /// Drops every mutant whose text does not compile. The engine scores a non-compiling mutant as
    /// killed, and that kill is owed to the compiler, not to a witness case — the canonical case is
    /// <c>remove-statement</c> dropping <c>var x = input.Get(...)</c> and leaving <c>x</c>
    /// dangling. Each mutant is re-bound in the same compilation the operator family binds against,
    /// so what fails here is what the certification compile would fail. When the CANDIDATE itself
    /// does not bind (a reference is missing, so every mutant would "fail" for the same reason)
    /// nothing can be judged and nothing is discarded.
    /// </summary>
    private static List<AstMutation> DiscardNonCompiling(
        List<AstMutation> mutations, CSharpCompilation compilation, SyntaxTree candidate)
    {
        if (mutations.Count == 0 || HasErrors(compilation, candidate))
            return mutations;

        var kept = new List<AstMutation>(mutations.Count);
        foreach (var mutation in mutations)
        {
            // Re-parsed under the candidate's own (the build's) parse options, so the same #if
            // branches are code here as they were when the mutant was collected.
            var mutatedTree = CSharpSyntaxTree.ParseText(mutation.ToSource(), (CSharpParseOptions)candidate.Options);
            if (!HasErrors(compilation.ReplaceSyntaxTree(candidate, mutatedTree), mutatedTree))
                kept.Add(mutation);
        }

        return kept;
    }

    private static bool HasErrors(CSharpCompilation compilation, SyntaxTree tree) =>
        compilation.GetSemanticModel(tree).GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// A binding-only compilation of the candidate, so operator mutants can ask what type an
    /// operand has before rewriting it, and so a mutant can be re-bound to see whether it compiles.
    /// The candidate is parsed ALONE (so every mutation keeps the candidate's own line numbers and
    /// <c>ToSource()</c> returns unwrapped candidate text, exactly as before); the usings and audit
    /// context the certification compile injects are hoisted into a sibling tree as
    /// <c>global using</c>s, which bind identically. The reference set is the one every mutant
    /// will be compiled against, so a type that resolves here resolves there.
    /// </summary>
    private static CSharpCompilation BindCandidate(
        SyntaxTree candidate, IReadOnlyList<string>? compilationReferences, BrickCompileOptions? compileOptions)
    {
        var parseOptions = BrickCompilation.ParseOptions(compileOptions);
        var preambleLines = CandidateSourceWrapper.Wrap(string.Empty)
            .Split('\n')
            .Select(line => line.StartsWith("using ", StringComparison.Ordinal) ? "global " + line : line);
        var preamble = CSharpSyntaxTree.ParseText(string.Join("\n", preambleLines), parseOptions);

        // The build's own global usings bind beside the candidate too, as they did for csc.
        var trees = new List<SyntaxTree> { candidate, preamble };
        trees.AddRange(BrickCompilation.CompanionTrees(compileOptions));

        return CSharpCompilation.Create(
            "AshlarMutationCatalogBinding",
            trees,
            RoslynCodeAnalysisService.BuildReferenceSet(compilationReferences),
            BrickCompilation.CompilationOptions(compileOptions, OutputKind.DynamicallyLinkedLibrary));
    }

    private static void CollectFlipBinaryComparisons(SyntaxNode root, MutationScope scope, List<AstMutation> mutations)
    {
        foreach (var node in scope.Nodes.OfType<BinaryExpressionSyntax>())
        {
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
        }
    }

    private static void CollectNegateConditions(SyntaxNode root, MutationScope scope, List<AstMutation> mutations)
    {
        foreach (var node in scope.Nodes.OfType<IfStatementSyntax>())
        {
            if (node.Condition.Kind() == SyntaxKind.LogicalNotExpression)
                continue;

            // The new condition takes the old condition's trivia; the statement keeps its OWN. Its
            // leading trivia can carry a `#if` directive, and a mutant that drops it leaves a dangling
            // `#endif` — a mutant of the preprocessor, not of the program, which never compiles.
            var negated = node.WithCondition(
                SyntaxFactory.PrefixUnaryExpression(
                    SyntaxKind.LogicalNotExpression,
                    SyntaxFactory.ParenthesizedExpression(node.Condition.WithoutTrivia()))
                .WithTriviaFrom(node.Condition));

            mutations.Add(Create(
                root,
                "negate-condition",
                node,
                node,
                negated,
                node.Condition.ToString(),
                "!(" + node.Condition.WithoutTrivia() + ")"));
        }
    }

    private static void CollectMutateIntegerLiterals(SyntaxNode root, MutationScope scope, List<AstMutation> mutations)
    {
        foreach (var node in scope.Nodes.OfType<LiteralExpressionSyntax>())
        {
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
        }
    }

    /// <summary>
    /// Rewrites the last character of a string literal — except a LOOKUP KEY, see
    /// <see cref="IsLookupKey"/>: a mutated <c>input.Get("baseDamagX")</c> throws on every input
    /// and is killed by any witness with a case, so its kill says nothing about the witness's
    /// expectations, and counting it inflated <c>mutants_killed</c> on every brick ever certified.
    /// </summary>
    private static void CollectMutateStringLiterals(
        SyntaxNode root, MutationScope scope, SemanticModel model, List<AstMutation> mutations)
    {
        foreach (var node in scope.Nodes.OfType<LiteralExpressionSyntax>())
        {
            if (node.Kind() != SyntaxKind.StringLiteralExpression)
                continue;

            var text = node.Token.ValueText;
            if (string.IsNullOrEmpty(text))
                continue;

            if (IsLookupKey(node, model))
                continue;

            var mutated = text.Length == 1 ? text + "X" : text[..^1] + "X";
            var replacement = SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(mutated));

            mutations.Add(Create(root, "mutate-string-literal", node, replacement));
        }
    }

    /// <summary>
    /// Whether a string literal is the KEY of a keyed lookup, precisely: (1) the index of an
    /// element access that is read, not assigned (<c>dict["k"]</c> but not <c>dict["k"] = v</c>);
    /// or (2) an argument of an invocation that binds to a NON-VOID method, in the position of a
    /// parameter named <c>key</c> — the .NET convention every keyed lookup follows
    /// (<c>BrickInput.Get&lt;T&gt;(string key)</c>, <c>TryGetValue(TKey key, out ...)</c>,
    /// <c>ContainsKey(TKey key)</c>, <c>GetValueOrDefault(TKey key)</c>). Void methods are stores
    /// (<c>BrickOutput.Set(string key, object value)</c>, <c>Add(key, value)</c>): a wrong store key
    /// drops a declared output, which only a witness asserting that output can notice, so those keys
    /// stay mutable. When the invocation cannot be bound at all the member name decides, from a
    /// short list of lookup names, so a blind run behaves the same way on the common shapes.
    /// </summary>
    private static bool IsLookupKey(LiteralExpressionSyntax literal, SemanticModel model)
    {
        SyntaxNode node = literal;
        while (node.Parent is ParenthesizedExpressionSyntax parenthesized)
            node = parenthesized;

        if (node.Parent is not ArgumentSyntax argument)
            return false;

        switch (argument.Parent)
        {
            case BracketedArgumentListSyntax { Parent: ElementAccessExpressionSyntax access }:
                return !IsSimpleAssignmentTarget(access);

            case ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } arguments:
                var info = model.GetSymbolInfo(invocation);
                var method = info.Symbol as IMethodSymbol ?? info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
                if (method is null)
                    return UnboundLookupMemberNames.Contains(InvokedMemberName(invocation), StringComparer.Ordinal);

                if (method.ReturnsVoid)
                    return false;

                var parameter = ParameterFor(method, argument, arguments);
                return parameter is not null && string.Equals(parameter.Name, "key", StringComparison.OrdinalIgnoreCase);

            default:
                return false;
        }
    }

    private static string? InvokedMemberName(InvocationExpressionSyntax invocation) => invocation.Expression switch
    {
        MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
        MemberBindingExpressionSyntax binding => binding.Name.Identifier.ValueText,
        IdentifierNameSyntax name => name.Identifier.ValueText,
        GenericNameSyntax generic => generic.Identifier.ValueText,
        _ => null
    };

    /// <summary>The parameter an argument binds to: by name when named, else by position (a
    /// reduced extension method's parameters already exclude <c>this</c>), else <c>params</c>.</summary>
    private static IParameterSymbol? ParameterFor(IMethodSymbol method, ArgumentSyntax argument, ArgumentListSyntax arguments)
    {
        if (argument.NameColon is not null)
        {
            var name = argument.NameColon.Name.Identifier.ValueText;
            return method.Parameters.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.Ordinal));
        }

        var index = arguments.Arguments.IndexOf(argument);
        if (index >= 0 && index < method.Parameters.Length)
            return method.Parameters[index];

        return method.Parameters.Length > 0 && method.Parameters[^1].IsParams ? method.Parameters[^1] : null;
    }

    private static bool IsSimpleAssignmentTarget(SyntaxNode node) =>
        node.Parent is AssignmentExpressionSyntax assignment
        && assignment.Kind() == SyntaxKind.SimpleAssignmentExpression
        && assignment.Left == node;

    /// <summary>
    /// Removes the first in-scope statement of every block (a constructor's metadata writes are
    /// not in scope, so a constructor block's first LOGIC statement is the one removed). A removal
    /// that leaves a later use dangling does not compile and is discarded downstream rather than
    /// scored as a kill.
    /// </summary>
    private static void CollectRemoveStatements(SyntaxNode root, MutationScope scope, List<AstMutation> mutations)
    {
        foreach (var block in scope.Nodes.OfType<BlockSyntax>())
        {
            var statement = block.Statements.FirstOrDefault(scope.Contains);
            if (statement is null || statement is ReturnStatementSyntax)
                continue;

            var newBlock = block.RemoveNode(statement, SyntaxRemoveOptions.KeepNoTrivia);
            if (newBlock is null)
                continue;

            mutations.Add(Create(
                root,
                "remove-statement",
                statement,
                block,
                newBlock,
                statement.ToString(),
                "(statement removed)"));
        }
    }

    private static void CollectSwapLogicalOperators(SyntaxNode root, MutationScope scope, List<AstMutation> mutations)
    {
        foreach (var node in scope.Nodes.OfType<BinaryExpressionSyntax>())
        {
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
    private static void CollectDegradeCoalesceAssignments(SyntaxNode root, MutationScope scope, List<AstMutation> mutations)
    {
        foreach (var node in scope.Nodes.OfType<AssignmentExpressionSyntax>())
        {
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
    /// <c>1 / 0</c>), and so is <c>*</c> → <c>/</c> over a constant-zero divisor. An IDENTITY
    /// operand is skipped too: <c>x * 1</c> and <c>x / 1</c>, <c>x + 0</c> and <c>x - 0</c>, are
    /// the same program, so the swap is an equivalent mutant no witness could kill.
    /// </summary>
    private static void CollectSwapArithmeticOperators(
        SyntaxNode root, MutationScope scope, SemanticModel model, List<AstMutation> mutations)
    {
        foreach (var node in scope.Nodes.OfType<BinaryExpressionSyntax>())
        {
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

            if (IsIdentityOperand(node.Kind(), model.GetConstantValue(node.Right)))
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
        }
    }

    /// <summary>
    /// Compound arithmetic assignment: <c>+=</c> ↔ <c>-=</c>, <c>*=</c> ↔ <c>/=</c>,
    /// <c>%=</c> → <c>*=</c>, under the same numeric-operands and identity-operand rules as the
    /// binary form. A <c>string +=</c> (concatenation) and a delegate <c>+=</c> (event
    /// subscription) have no <c>-=</c> with the same meaning — the former does not even compile
    /// — so both are excluded by the type check.
    /// </summary>
    private static void CollectSwapArithmeticAssignments(
        SyntaxNode root, MutationScope scope, SemanticModel model, List<AstMutation> mutations)
    {
        foreach (var node in scope.Nodes.OfType<AssignmentExpressionSyntax>())
        {
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

            if (IsIdentityOperand(node.Kind(), model.GetConstantValue(node.Right)))
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
        SyntaxNode root, MutationScope scope, SemanticModel model, List<AstMutation> mutations)
    {
        foreach (var node in scope.Nodes.OfType<BinaryExpressionSyntax>())
        {
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
        }
    }

    /// <summary>
    /// Unary sign and step: <c>-x</c> ↔ <c>+x</c>, <c>++x</c> ↔ <c>--x</c>, <c>x++</c> ↔ <c>x--</c>,
    /// for numeric (or <c>char</c>) operands. Constant operands are left to
    /// <c>mutate-int-literal</c>: a sign flip on a literal is a literal mutation, and on
    /// <c>-2147483648</c> it is a compile error.
    /// </summary>
    private static void CollectSwapUnaryOperators(
        SyntaxNode root, MutationScope scope, SemanticModel model, List<AstMutation> mutations)
    {
        foreach (var node in scope.Nodes)
        {
            AstMutation? mutation = node switch
            {
                PrefixUnaryExpressionSyntax prefix => TrySwapPrefixUnary(root, model, prefix),
                PostfixUnaryExpressionSyntax postfix => TrySwapPostfixUnary(root, model, postfix),
                _ => null
            };
            if (mutation is null)
                continue;

            mutations.Add(mutation);
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
        SyntaxNode root, MutationScope scope, SemanticModel model, List<AstMutation> mutations)
    {
        foreach (var node in scope.Nodes.OfType<PrefixUnaryExpressionSyntax>())
        {
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

    /// <summary>
    /// Whether swapping the operator over a constant right operand leaves the program unchanged:
    /// <c>+</c>/<c>-</c> over 0, <c>*</c>/<c>/</c> over 1 (<c>%</c> is never an identity).
    /// </summary>
    private static bool IsIdentityOperand(SyntaxKind kind, Optional<object?> right) => kind switch
    {
        SyntaxKind.AddExpression or SyntaxKind.SubtractExpression
            or SyntaxKind.AddAssignmentExpression or SyntaxKind.SubtractAssignmentExpression => IsConstantZero(right),
        SyntaxKind.MultiplyExpression or SyntaxKind.DivideExpression
            or SyntaxKind.MultiplyAssignmentExpression or SyntaxKind.DivideAssignmentExpression => IsConstantEqualTo(right, 1),
        _ => false
    };

    private static bool IsConstantZero(Optional<object?> constant) => IsConstantEqualTo(constant, 0);

    private static bool IsConstantEqualTo(Optional<object?> constant, int expected) => constant.HasValue && constant.Value switch
    {
        sbyte v => v == expected,
        byte v => v == expected,
        short v => v == expected,
        ushort v => v == expected,
        int v => v == expected,
        uint v => v == expected,
        long v => v == expected,
        ulong v => v == (ulong)expected,
        float v => v == expected,
        double v => v == expected,
        decimal v => v == expected,
        char v => v == expected,
        _ => false
    };

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

    /// <summary>
    /// The nodes the operators may rewrite: every member body of every type in the candidate —
    /// method, constructor (body and <c>: this(...)</c>/<c>: base(...)</c> initializer),
    /// destructor, operator and conversion bodies; property, indexer and event accessor bodies;
    /// expression-bodied members; property and field initializers — and everything inside them,
    /// which is where local functions and lambdas live. Modifiers play no part: a
    /// <c>private static</c> helper carries the same logic as a private instance one, and the
    /// witness is judged on whether it observes that logic, not on who may call it.
    /// </summary>
    /// <remarks>
    /// One shape is excluded, by data flow rather than by name: a constructor statement that
    /// assigns a field or property NOTHING in the candidate reads — <c>Id = "damage-resolver"</c>,
    /// <c>Version = "1.0.0"</c>, <c>Interface = new BrickInterface { ... }</c>. No member computes
    /// anything from such a write, so no witness case could ever tell a mutant of it from the
    /// original, and every literal in it would survive as an equivalent mutant on every honest
    /// brick. The same statement stays IN scope the moment any body reads the member (a
    /// <c>Summary = $"{Name} ..."</c> makes <c>Name</c> observable). A read the candidate cannot see
    /// — a base-class method consulting the property — is not detected; that residual only ever
    /// removes mutants, never manufactures a kill. When binding fails the target is unresolved and
    /// the statement is kept.
    /// </remarks>
    private sealed class MutationScope
    {
        private readonly HashSet<SyntaxNode> _nodes;

        private MutationScope(List<SyntaxNode> nodes)
        {
            Nodes = nodes;
            _nodes = new HashSet<SyntaxNode>(nodes);
        }

        /// <summary>In document order, each body followed by its descendants.</summary>
        public IReadOnlyList<SyntaxNode> Nodes { get; }

        public bool Contains(SyntaxNode node) => _nodes.Contains(node);

        public static MutationScope Of(SyntaxNode root, SemanticModel model)
        {
            var readMembers = MembersReadAnywhere(root, model);
            var nodes = new List<SyntaxNode>();

            foreach (var member in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
            {
                foreach (var body in BodiesOf(member))
                {
                    if (member is ConstructorDeclarationSyntax && body is BlockSyntax block)
                    {
                        var unobservable = block.Statements
                            .Where(statement => WritesOnlyWhatNothingReads(statement, model, readMembers))
                            .ToHashSet();
                        nodes.Add(block);
                        foreach (var node in block.DescendantNodes(descendIntoChildren: child => !unobservable.Contains(child)))
                        {
                            if (!unobservable.Contains(node))
                                nodes.Add(node);
                        }

                        continue;
                    }

                    nodes.Add(body);
                    nodes.AddRange(body.DescendantNodes());
                }
            }

            return new MutationScope(nodes);
        }

        private static IEnumerable<SyntaxNode> BodiesOf(MemberDeclarationSyntax member)
        {
            switch (member)
            {
                case BaseMethodDeclarationSyntax method:
                    if (method.Body is not null)
                        yield return method.Body;
                    if (method.ExpressionBody is not null)
                        yield return method.ExpressionBody;
                    if (method is ConstructorDeclarationSyntax { Initializer: { } initializer })
                        yield return initializer;
                    break;

                case BasePropertyDeclarationSyntax property:
                    if (property.AccessorList is not null)
                    {
                        foreach (var accessor in property.AccessorList.Accessors)
                        {
                            if (accessor.Body is not null)
                                yield return accessor.Body;
                            if (accessor.ExpressionBody is not null)
                                yield return accessor.ExpressionBody;
                        }
                    }

                    switch (property)
                    {
                        case PropertyDeclarationSyntax { ExpressionBody: { } expression }:
                            yield return expression;
                            break;
                        case IndexerDeclarationSyntax { ExpressionBody: { } expression }:
                            yield return expression;
                            break;
                    }

                    if (property is PropertyDeclarationSyntax { Initializer: { } propertyInitializer })
                        yield return propertyInitializer;
                    break;

                case BaseFieldDeclarationSyntax field:
                    foreach (var variable in field.Declaration.Variables)
                    {
                        if (variable.Initializer is not null)
                            yield return variable.Initializer;
                    }
                    break;
            }
        }

        /// <summary>
        /// Every field or property symbol some expression in the candidate READS: any name or
        /// member access that is not the target of a plain assignment (a compound assignment reads
        /// too). The name half of a member access is skipped so <c>this.Id = ...</c> is not counted
        /// as a read of <c>Id</c>.
        /// </summary>
        private static HashSet<ISymbol> MembersReadAnywhere(SyntaxNode root, SemanticModel model)
        {
            var read = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            foreach (var node in root.DescendantNodes())
            {
                if (node is not (IdentifierNameSyntax or MemberAccessExpressionSyntax))
                    continue;

                if (node.Parent is MemberAccessExpressionSyntax access && access.Name == node)
                    continue;

                if (IsSimpleAssignmentTarget(node))
                    continue;

                var symbol = model.GetSymbolInfo(node).Symbol;
                if (symbol is IPropertySymbol or IFieldSymbol)
                    read.Add(symbol.OriginalDefinition);
            }

            return read;
        }

        private static bool WritesOnlyWhatNothingReads(
            StatementSyntax statement, SemanticModel model, HashSet<ISymbol> readMembers)
        {
            if (statement is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment }
                || assignment.Kind() != SyntaxKind.SimpleAssignmentExpression)
                return false;

            var target = model.GetSymbolInfo(assignment.Left).Symbol;
            return target is IPropertySymbol or IFieldSymbol
                && !readMembers.Contains(target.OriginalDefinition);
        }
    }
}
