#pragma once
#include <QDialog>

// Deliberately UNRELATED to the parser — a plain export-options dialog that
// never includes FlightRecorderArchive.h. Should appear in neither lower nor
// upper for any FlightRecorderArchive extraction — a true-negative test.
class ExportDialog : public QDialog {
public:
    explicit ExportDialog(QWidget* parent = nullptr);
};
