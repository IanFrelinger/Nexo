#include "ExportDialog.h"
#include <QVBoxLayout>
#include <QLabel>

ExportDialog::ExportDialog(QWidget* parent) : QDialog(parent) {
    auto* layout = new QVBoxLayout(this);
    layout->addWidget(new QLabel("Export options go here.", this));
}
