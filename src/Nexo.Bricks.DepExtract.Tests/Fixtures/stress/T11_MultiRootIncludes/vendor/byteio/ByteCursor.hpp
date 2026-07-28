#pragma once
#include <cstdint>
#include <fstream>
#include <vector>

// Third-party-style helper living outside the main src/ tree (extra -I).
class ByteCursor {
public:
    explicit ByteCursor(const std::string& path);
    bool readExact(void* dst, size_t n);
    void seek(uint64_t off);
    uint64_t tell() const { return pos_; }
    bool eof() const;

private:
    std::ifstream in_;
    uint64_t pos_ = 0;
};
