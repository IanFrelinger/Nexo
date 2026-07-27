using FluentAssertions;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

public sealed class AdapterScaffoldTests
{
    [Fact]
    public void LooksSimple_and_builds_for_trivial_counter()
    {
        var src = """
            class SimpleCounter {
            public:
                explicit SimpleCounter(long start) : value_(start) {}
                bool advance(long& out) {
                    if (value_ >= 1000000) return false;
                    out = ++value_;
                    return true;
                }
            private:
                long value_;
            };
            """;
        AdapterScaffold.LooksSimple(src).Should().BeTrue();
        var code = AdapterScaffold.TryBuild(src);
        code.Should().NotBeNullOrWhiteSpace();
        code.Should().Contain("SimpleCounter");
        code.Should().Contain("advance");
        code.Should().Contain("CustomEventReader");
        code.Should().Contain("use_custom_reader");
    }

    [Fact]
    public void TryBuild_wires_path_ctor_and_include_for_file_backed_parser()
    {
        var src = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "e2e", "MockEvtqParser", "MockEvtqStream.hpp"));
        AdapterScaffold.LooksSimple(src).Should().BeTrue();
        var code = AdapterScaffold.TryBuild(src, "MockEvtqStream.hpp");
        code.Should().Contain("#include \"MockEvtqStream.hpp\"");
        code.Should().Contain("std::make_unique<MockEvtqStream>(path)");
        code.Should().Contain("last_type()");
        code.Should().Contain("/ 1000.0");
        code.Should().NotContain("(void)path;");
    }

    [Fact]
    public void LooksSimple_rejects_large_hierarchies()
    {
        var src = string.Join('\n', Enumerable.Range(0, 6).Select(i =>
            $"class C{i} {{ public: void next{i}() {{}} void other{i}() {{}} void more{i}() {{}} }};"));
        AdapterScaffold.LooksSimple(src).Should().BeFalse();
        AdapterScaffold.TryBuild(src).Should().BeNull();
    }
}
