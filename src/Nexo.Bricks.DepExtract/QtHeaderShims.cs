using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Nexo.Bricks.DepExtract;

/// <summary>
/// Headless no-op shims for Qt headers referenced by extracted parsers.
///
/// Desktop parsers commonly use Qt only to float warnings at the user
/// (QMessageBox + QString); that usage is not load-bearing for decode, but the
/// bare <c>#include &lt;QMessageBox&gt;</c> makes every draft fail the Qt-less
/// compile gate and the Dockerfile.custom build. Rather than asking the model
/// (or the scaffold) to rewrite the proprietary header, we satisfy those
/// includes with tiny shims: QMessageBox writes to stderr, QString wraps
/// std::string. Unknown Qt headers get an empty stub — if the parser merely
/// includes them the build proceeds, but any *functional* use of an unshimmed
/// Qt type still fails the compile gate, which is the honest outcome for
/// genuinely load-bearing Qt dependencies.
/// </summary>
public static class QtHeaderShims
{
    static readonly Regex QtInclude = new(
        "#\\s*include\\s*<((?:Qt[A-Za-z0-9]*/)?(Q[A-Za-z0-9_]+))>",
        RegexOptions.Compiled);

    static readonly string[] ScanExtensions = [".hpp", ".h", ".hh", ".hxx", ".cpp", ".cc", ".cxx", ".c"];

    /// <summary>
    /// Scans every C/C++ file in the extract duplicate (companion headers pull
    /// in Qt too, not just the entry — e.g. a project-wide global header) plus
    /// the entry bundle, then writes shims for all Qt includes found.
    /// </summary>
    /// <param name="parserDir">Source tree to scan for Qt includes (read-only; never written to).</param>
    /// <param name="shimDir">Directory to write shim headers into. When null, falls back to
    /// <paramref name="parserDir"/> for backward compatibility, but callers should pass a
    /// dedicated scratch dir so operator/extracted source is not mutated.</param>
    public static Dictionary<string, string> EnsureForDirectory(
        string entrySourceText, string parserDir, string? shimDir = null, ILogger? logger = null)
    {
        var combined = new System.Text.StringBuilder(entrySourceText);
        try
        {
            foreach (var file in Directory.EnumerateFiles(parserDir, "*", SearchOption.AllDirectories))
            {
                if (!ScanExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)) continue;
                var info = new FileInfo(file);
                if (info.Length > 2_000_000) continue;
                combined.AppendLine(File.ReadAllText(file));
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning("Qt shim directory scan failed ({Message}); falling back to entry text only", ex.Message);
        }
        return EnsureForSource(combined.ToString(), shimDir ?? parserDir, logger);
    }

    /// <summary>
    /// Scans <paramref name="sourceText"/> for Qt includes; writes shim headers
    /// under <paramref name="shimDir"/> (both the exact include path and the
    /// flat leaf) so the compile gate's <c>-I/shims</c> resolves them. Returns
    /// leaf-name → content for install-time companions (flattened into
    /// <c>poco/common/</c>, where <c>-Icommon</c> resolves flat includes).
    /// </summary>
    /// <param name="shimDir">Directory to write shim headers into. Should be a dedicated
    /// scratch dir, not operator-provided source. Existing files are never overwritten.</param>
    public static Dictionary<string, string> EnsureForSource(
        string sourceText, string shimDir, ILogger? logger = null)
    {
        var shims = new Dictionary<string, string>(StringComparer.Ordinal);
        var matches = QtInclude.Matches(sourceText);
        if (matches.Count == 0) return shims;

        foreach (Match m in matches)
        {
            var includePath = m.Groups[1].Value;   // e.g. QtWidgets/QMessageBox
            var leaf = m.Groups[2].Value;          // e.g. QMessageBox
            if (shims.ContainsKey(leaf)) continue;

            var content = ContentFor(leaf);
            shims[leaf] = content;

            WriteShim(shimDir, leaf, content, logger);
            if (!string.Equals(includePath, leaf, StringComparison.Ordinal))
                WriteShim(shimDir, includePath, $"#pragma once\n#include <{leaf}>\n", logger);
        }

        // QMessageBox's shim needs QString even when the source didn't include it directly.
        if (shims.ContainsKey("QMessageBox") && !shims.ContainsKey("QString"))
        {
            var qs = ContentFor("QString");
            shims["QString"] = qs;
            WriteShim(shimDir, "QString", qs, logger);
        }

        logger?.LogInformation(
            "Qt includes detected in parser source; generated {Count} headless shim header(s): {Names}",
            shims.Count, string.Join(", ", shims.Keys));
        return shims;
    }

    static void WriteShim(string shimDir, string relativePath, string content, ILogger? logger = null)
    {
        var dest = Path.Combine(shimDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
        // Never clobber a real file that happens to share a Q* name — a shim must not
        // silently replace operator source. (In a dedicated scratch dir nothing pre-exists,
        // so this only bites on the back-compat path that writes into the source tree.)
        if (File.Exists(dest))
        {
            logger?.LogDebug("Qt shim target already exists, not overwriting: {Dest}", dest);
            return;
        }
        var dir = Path.GetDirectoryName(dest);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(dest, content);
    }

    static string ContentFor(string leaf) => leaf switch
    {
        "QString" => QStringShim,
        "QMessageBox" => QMessageBoxShim,
        "QThread" => QThreadShim,
        _ =>
            "#pragma once\n" +
            $"// {leaf}: unknown Qt header stubbed by Nexo DepExtract (headless build).\n" +
            "// Merely including it is fine; functional use of its types will fail the\n" +
            "// compile gate, which means the Qt dependency was load-bearing after all.\n",
    };

    const string QStringShim =
        """
        #pragma once
        // Minimal headless QString shim (Nexo DepExtract). Supports the subset
        // desktop parsers use for user-facing messages: construction, arg()
        // placeholder chaining, and std::string conversion.
        #include <string>
        #include <type_traits>

        class QString {
        public:
            QString() = default;
            QString(const char* s) : s_(s ? s : "") {}
            explicit QString(const std::string& s) : s_(s) {}
            static QString fromStdString(const std::string& s) { return QString(s); }
            const std::string& toStdString() const { return s_; }

            template <typename T>
            QString arg(const T& value) const
            {
                std::string v;
                if constexpr (std::is_same_v<T, QString>) v = value.toStdString();
                else if constexpr (std::is_arithmetic_v<T>) v = std::to_string(value);
                else v = std::string(value);
                // Qt semantics: each arg() fills the lowest remaining %N placeholder.
                for (int n = 1; n <= 9; ++n) {
                    const std::string ph = "%" + std::to_string(n);
                    const auto pos = s_.find(ph);
                    if (pos != std::string::npos) {
                        std::string out = s_;
                        out.replace(pos, ph.size(), v);
                        return QString(out);
                    }
                }
                return *this;
            }

        private:
            std::string s_;
        };

        #ifndef QStringLiteral
        #define QStringLiteral(str) QString(str)
        #endif

        """;

    const string QMessageBoxShim =
        """
        #pragma once
        // Minimal headless QMessageBox shim (Nexo DepExtract): user-facing
        // dialogs become stderr lines so extracted parsers run in the GUI-less
        // pipeline without dragging in Qt.
        #include <QString>
        #include <cstdio>

        class QMessageBox {
        public:
            enum StandardButton { NoButton = 0x0, Ok = 0x400 };

            static StandardButton warning(void*, const QString& title, const QString& text)
            { Emit("warning", title, text); return Ok; }

            static StandardButton information(void*, const QString& title, const QString& text)
            { Emit("information", title, text); return Ok; }

            static StandardButton critical(void*, const QString& title, const QString& text)
            { Emit("critical", title, text); return Ok; }

        private:
            static void Emit(const char* level, const QString& title, const QString& text)
            {
                std::fprintf(stderr, "[qt-shim %s] %s: %s\n",
                    level, title.toStdString().c_str(), text.toStdString().c_str());
            }
        };

        """;

    const string QThreadShim =
        """
        #pragma once
        // Headless QThread shim (Nexo DepExtract). Covers the common worker
        // lifecycle (start/quit/wait/isRunning) when decode logic is plain C++
        // and QThread is only a host-side scheduling wrapper. Does NOT define
        // Q_OBJECT — signal/slot + moc remain a designed hard stop.
        class QThread {
        public:
            QThread() = default;
            virtual ~QThread() = default;

            void start() { running_ = true; run(); running_ = false; }
            void quit() { running_ = false; }
            bool wait(int = -1) { return true; }
            bool isRunning() const { return running_; }
            static void msleep(unsigned long) {}

        protected:
            virtual void run() {}

        private:
            bool running_ = false;
        };

        """;
}
