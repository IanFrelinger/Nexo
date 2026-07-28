#pragma once
#include "Buffer.hpp"
#include <cstddef>

class ByteCursor {
public:
    explicit ByteCursor(const ByteBuffer& buf) : buf_(buf), pos_(0) {}
    bool eof() const { return pos_ + 4 > buf_.bytes.size(); }
    uint32_t nextU32() {
        auto v = buf_.readU32Be(pos_);
        pos_ += 4;
        return v;
    }
    void seek(size_t abs) { pos_ = abs; }
    size_t tell() const { return pos_; }

private:
    const ByteBuffer& buf_;
    size_t pos_;
};
