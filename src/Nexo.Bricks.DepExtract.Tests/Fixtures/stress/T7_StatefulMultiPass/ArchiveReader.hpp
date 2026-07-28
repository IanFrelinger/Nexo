#pragma once
#include <cstdint>
#include <fstream>
#include <string>

// Complexity tier 7: an explicit lifecycle (must call open() before
// readNext(), throws otherwise) AND a semantic trap — seekRecord/recordIndex
// operate on RECORD INDEX, not byte offset, unlike every earlier tier. Tests
// whether the adapter notices the unit mismatch against IEventReader's
// position()/resume_at() (which are byte-offset-shaped) rather than silently
// treating an index as if it were a byte offset.
struct ArchiveRecord { double t; int32_t kind; };

class ArchiveReader {
public:
    explicit ArchiveReader(const std::string& path);
    void open();                          // must be called before readNext(); throws if already open
    bool readNext(ArchiveRecord& out);    // throws std::logic_error if not open
    void seekRecord(uint64_t index);      // index is a RECORD COUNT, not a byte offset
    uint64_t recordIndex() const;         // current record index, not byte offset
    void close();

private:
    enum class State { NotOpened, Open, Eof, Closed };
    std::string path_;
    std::ifstream in_;
    State state_ = State::NotOpened;
    uint64_t index_ = 0;
};
