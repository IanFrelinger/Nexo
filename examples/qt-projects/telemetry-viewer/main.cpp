#include <QCoreApplication>
#include <QDebug>
#include "TelemetryStream.h"

// App shell — deliberately NOT part of the dep-extract entry set, to prove
// extraction correctly scopes to just the parser, not the whole Qt app.
int main(int argc, char** argv) {
    QCoreApplication app(argc, argv);
    if (argc < 2) { qWarning() << "usage: telemetry-viewer <file.tlm>"; return 1; }
    TelemetryStream stream(argv[1]);
    TelemetrySample s;
    int n = 0;
    while (stream.fetchNext(s)) ++n;
    qInfo() << "read" << n << "of" << stream.sampleCount() << "declared samples";
    return 0;
}
