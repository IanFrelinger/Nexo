#pragma once
#include <cstdint>
#include <fstream>
#include <memory>
#include <string>

// Complexity tier 5: two-level indirection — SessionLog just opens the file;
// the actual record-reading API lives on a separate SessionCursor object you
// must obtain via openCursor(). Tests whether the adapter realizes it needs
// to hold the CURSOR, not the SessionLog, to do the real work.
struct SessionRecord { double t; int32_t code; };

class SessionCursor {
public:
    bool next(SessionRecord& out);
    void seek(uint64_t off);
    uint64_t offset() const;

private:
    friend class SessionLog;
    SessionCursor(std::shared_ptr<std::ifstream> stream, uint64_t start);
    std::shared_ptr<std::ifstream> stream_;
    uint64_t pos_;
};

class SessionLog {
public:
    explicit SessionLog(const std::string& path);
    SessionCursor openCursor();   // returns a cursor positioned at the first record

private:
    std::shared_ptr<std::ifstream> stream_;
};
