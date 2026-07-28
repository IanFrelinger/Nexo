#include "MainWindow.h"
#include <QLabel>

MainWindow::MainWindow(const QString& archivePath, QWidget* parent)
    : QMainWindow(parent), archive_(std::make_unique<FlightRecorderArchive>(archivePath)) {
    setCentralWidget(new QLabel("Flight Data Recorder Suite", this));
}

void MainWindow::loadSection(quint32 kind) {
    lastLoadedCount_ = 0;
    if (!archive_->selectSection(kind)) return;
    FlightRecord rec;
    while (archive_->nextRecord(rec)) {
        ++lastLoadedCount_;
    }
}

int MainWindow::visibleRecordCount() const { return lastLoadedCount_; }
