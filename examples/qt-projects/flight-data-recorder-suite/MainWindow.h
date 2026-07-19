#pragma once
#include <QMainWindow>
#include <memory>
#include "FlightRecorderArchive.h"

// App-shell class that USES FlightRecorderArchive (mirrors SessionWindow in
// the medium-scale example) — tests "upper" detection through real Qt GUI
// code. Deliberately does NOT include ExportDialog.h, keeping ExportDialog a
// genuine true-negative for this extraction.
class MainWindow : public QMainWindow {
public:
    explicit MainWindow(const QString& archivePath, QWidget* parent = nullptr);

    void loadSection(quint32 kind);
    int visibleRecordCount() const;

private:
    std::unique_ptr<FlightRecorderArchive> archive_;
    int lastLoadedCount_ = 0;
};
