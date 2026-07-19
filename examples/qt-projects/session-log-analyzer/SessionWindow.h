#pragma once
#include <QMainWindow>
#include <QListWidget>
#include "SessionLog.h"

// App shell — a real Qt GUI class that USES SessionLog, deliberately NOT part
// of the dep-extract entry set. Should show up correctly in "upper" (it
// transitively includes the parser), proving upper-detection also works
// through genuine Qt GUI code, not just plain C++.
class SessionWindow : public QMainWindow {
public:
    explicit SessionWindow(const QString& logPath, QWidget* parent = nullptr);

private:
    void loadAll(const QString& logPath);
    QListWidget* list_;
};
