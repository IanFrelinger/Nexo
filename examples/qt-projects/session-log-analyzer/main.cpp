#include <QCoreApplication>
#include "SessionLog.h"
#include <QDebug>

// Headless entry point (avoids requiring a display server in CI/WSL) — the
// GUI class SessionWindow.h/.cpp still exists to exercise "upper" detection
// even though main.cpp itself doesn't instantiate it here.
int main(int argc, char** argv) {
    QCoreApplication app(argc, argv);
    if (argc < 2) { qWarning() << "usage: session-log-analyzer <file.slog>"; return 1; }
    SessionLog log(argv[1]);
    auto cursor = log.openCursor();
    SessionRecord rec;
    int n = 0;
    while (cursor.next(rec)) ++n;
    qInfo() << "read" << n << "records";
    return 0;
}
