#include "SessionLog.hpp"
#include <cstring>

// record: [f64 t][i32 code] (12B), no header
SessionLog::SessionLog(const std::string& path)
    : stream_(std::make_shared<std::ifstream>(path, std::ios::binary)) {}

SessionCursor SessionLog::openCursor() { return SessionCursor(stream_, 0); }

SessionCursor::SessionCursor(std::shared_ptr<std::ifstream> stream, uint64_t start)
    : stream_(std::move(stream)), pos_(start) {
    stream_->seekg(static_cast<std::streamoff>(start));
}

bool SessionCursor::next(SessionRecord& out) {
    char buf[12];
    if (!stream_->read(buf, 12)) return false;
    std::memcpy(&out.t, buf, 8);
    std::memcpy(&out.code, buf + 8, 4);
    pos_ += 12;
    return true;
}

void SessionCursor::seek(uint64_t off) {
    stream_->clear();
    stream_->seekg(static_cast<std::streamoff>(off));
    pos_ = off;
}

uint64_t SessionCursor::offset() const { return pos_; }
