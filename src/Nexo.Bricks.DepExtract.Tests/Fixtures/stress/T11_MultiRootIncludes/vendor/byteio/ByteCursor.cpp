#include "ByteCursor.hpp"

ByteCursor::ByteCursor(const std::string& path) : in_(path, std::ios::binary) {}

bool ByteCursor::readExact(void* dst, size_t n) {
    if (!in_.read(reinterpret_cast<char*>(dst), static_cast<std::streamsize>(n))) return false;
    pos_ += n;
    return true;
}

void ByteCursor::seek(uint64_t off) {
    in_.clear();
    in_.seekg(static_cast<std::streamoff>(off), std::ios::beg);
    pos_ = off;
}

bool ByteCursor::eof() const { return in_.eof(); }
