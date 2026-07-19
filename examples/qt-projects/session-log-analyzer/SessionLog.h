#pragma once
#include <QFile>
#include <QDataStream>
#include <QString>
#include <memory>
#include <cstdint>

// Medium-scale example: two-level indirection over Qt I/O. SessionLog just
// opens the file; the actual record-reading API lives on a separate
// SessionCursor obtained via openCursor(). Tests whether extraction/adaptation
// realizes it needs to hold the CURSOR, not the SessionLog, to do real work —
// same shape as the plain-C++ T5 tier, but through Qt's QFile/QDataStream.
struct SessionRecord { double t; qint32 code; };

class SessionCursor {
public:
    bool next(SessionRecord& out);
    void seek(quint64 off);
    quint64 offset() const;

private:
    friend class SessionLog;
    SessionCursor(std::shared_ptr<QFile> file, quint64 start);
    std::shared_ptr<QFile> file_;
    QDataStream in_;
    quint64 pos_;
};

class SessionLog {
public:
    explicit SessionLog(const QString& path);
    SessionCursor openCursor();   // returns a cursor positioned at the first record

private:
    std::shared_ptr<QFile> file_;
};
