using Xunit;

// Test parallelism is disabled by ATTRIBUTE, not only by xunit.runner.json.
//
// The json alone was not enough and had been silently ineffective: the project has
// no copy directive for it, so the stale bin\ copy — which omitted
// parallelizeTestCollections and therefore defaulted it to TRUE with
// maxParallelThreads=2 — is what xunit actually read. Two test classes then ran
// concurrently while several of them redirect the PROCESS-GLOBAL Console.Out to
// their own StringWriter, so one clobbered the other: foreign command output in
// the wrong assertion buffer, ObjectDisposedException from Console.WriteLine, a
// different victim on each run, and nothing reproducible in isolation.
//
// An assembly attribute is compiled in, so it cannot be defeated by a stale or
// uncopied config file. The json is also wired up in the csproj now, but this is
// the guarantee.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
