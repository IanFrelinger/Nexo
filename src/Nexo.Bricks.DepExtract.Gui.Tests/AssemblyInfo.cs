using Xunit;

// GuiIntegrationTests, GuiErrorHandlingTests, and AdaptInstallLoopFailureLoopTests all mutate
// process-wide state (WORKSPACE_ROOT / COMPILE_GATE / SKIP_OLLAMA env vars) around each HTTP
// call. xUnit runs test classes in separate collections in parallel by default, which raced
// these env vars across classes and produced spurious 400/404/500s. Serialize collections so
// the WebApplicationFactory<Program> classes never overlap.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
