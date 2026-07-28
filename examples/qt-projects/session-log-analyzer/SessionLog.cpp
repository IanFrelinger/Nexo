#include "SessionLog.h"

// record: [f64 t][i32 code] (12B), no header, little-endian.
SessionLog::SessionLog(const QString& path) : file_(std::make_shared<QFile>(path)) {
    file_->open(QIODevice::ReadOnly);
}

SessionCursor SessionLog::openCursor() { return SessionCursor(file_, 0); }

SessionCursor::SessionCursor(std::shared_ptr<QFile> file, quint64 start)
    : file_(std::move(file)), pos_(start) {
    in_.setDevice(file_.get());
    in_.setByteOrder(QDataStream::LittleEndian);
    file_->seek(static_cast<qint64>(start));
}

bool SessionCursor::next(SessionRecord& out) {
    if (file_->atEnd()) return false;
    double t; qint32 code;
    in_ >> t >> code;
    if (in_.status() != QDataStream::Ok) return false;
    out.t = t; out.code = code;
    pos_ += 12;
    return true;
}

void SessionCursor::seek(quint64 off) {
    file_->seek(static_cast<qint64>(off));
    pos_ = off;
}

quint64 SessionCursor::offset() const { return pos_; }
