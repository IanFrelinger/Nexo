#include "ArchiveReader.hpp"
#include <cstring>
#include <stdexcept>

// record: [f64 t][i32 kind] (12B), fixed size, no header
ArchiveReader::ArchiveReader(const std::string& path) : path_(path) {}

void ArchiveReader::open() {
    if (state_ != State::NotOpened) throw std::logic_error("ArchiveReader already open");
    in_.open(path_, std::ios::binary);
    state_ = State::Open;
}

bool ArchiveReader::readNext(ArchiveRecord& out) {
    if (state_ != State::Open) throw std::logic_error("ArchiveReader not open");
    char buf[12];
    if (!in_.read(buf, 12)) { state_ = State::Eof; return false; }
    std::memcpy(&out.t, buf, 8);
    std::memcpy(&out.kind, buf + 8, 4);
    ++index_;
    return true;
}

void ArchiveReader::seekRecord(uint64_t index) {
    in_.clear();
    in_.seekg(static_cast<std::streamoff>(index * 12));
    index_ = index;
    state_ = State::Open;
}

uint64_t ArchiveReader::recordIndex() const { return index_; }

void ArchiveReader::close() { in_.close(); state_ = State::Closed; }
