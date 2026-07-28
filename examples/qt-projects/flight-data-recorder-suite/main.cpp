#include <QCoreApplication>
#include "FlightRecorderArchive.h"
#include <QDebug>

// Headless entry point (avoids requiring a display server in CI/WSL) — the
// GUI class MainWindow.h/.cpp still exists to exercise "upper" detection
// even though main.cpp itself doesn't instantiate it here.
int main(int argc, char** argv) {
    QCoreApplication app(argc, argv);
    if (argc < 2) { qWarning() << "usage: flight-data-recorder-suite <file.flt>"; return 1; }
    FlightRecorderArchive archive(argv[1]);
    int total = 0;
    for (const auto& sec : archive.sections()) {
        archive.selectSection(sec.kind);
        FlightRecord rec;
        while (archive.nextRecord(rec)) ++total;
    }
    qInfo() << "read" << total << "records across" << archive.sections().size() << "sections";
    return 0;
}
