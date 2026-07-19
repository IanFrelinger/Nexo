#include "SessionWindow.h"
#include <QString>

SessionWindow::SessionWindow(const QString& logPath, QWidget* parent) : QMainWindow(parent) {
    list_ = new QListWidget(this);
    setCentralWidget(list_);
    loadAll(logPath);
}

void SessionWindow::loadAll(const QString& logPath) {
    SessionLog log(logPath);
    auto cursor = log.openCursor();
    SessionRecord rec;
    while (cursor.next(rec)) {
        list_->addItem(QString("t=%1 code=%2").arg(rec.t).arg(rec.code));
    }
}
