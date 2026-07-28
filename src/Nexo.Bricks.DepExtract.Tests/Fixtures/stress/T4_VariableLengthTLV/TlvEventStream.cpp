#include "TlvEventStream.hpp"
#include <cstring>

// record: [f64 t][u16 tag][u16 len][len bytes value]
TlvEventStream::TlvEventStream(const std::string& path) : in_(path, std::ios::binary) {}

bool TlvEventStream::nextEvent(TlvEvent& out) {
    char hdr[12];
    if (!in_.read(hdr, 12)) return false;
    std::memcpy(&out.t, hdr, 8);
    std::memcpy(&out.tag, hdr + 8, 2);
    uint16_t len;
    std::memcpy(&len, hdr + 10, 2);
    out.value.resize(len);
    if (len) in_.read(reinterpret_cast<char*>(out.value.data()), len);
    pos_ += 12 + len;
    return true;
}

void TlvEventStream::seekBytes(uint64_t off) {
    in_.clear();
    in_.seekg(static_cast<std::streamoff>(off));
    pos_ = off;
}

uint64_t TlvEventStream::bytesRead() const { return pos_; }
