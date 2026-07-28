#include "RecordSource.hpp"
#include <cstring>

// record: [f64 t][u16 len][len bytes]
BinaryRecordSource::BinaryRecordSource(const std::string& path) : in_(path, std::ios::binary) {}

bool BinaryRecordSource::pull(GenericRecord& out) {
    char hdr[10];
    if (!in_.read(hdr, 10)) return false;
    std::memcpy(&out.t, hdr, 8);
    uint16_t len;
    std::memcpy(&len, hdr + 8, 2);
    out.bytes.resize(len);
    if (len) in_.read(reinterpret_cast<char*>(out.bytes.data()), len);
    pos_ += 10 + len;
    return true;
}

void BinaryRecordSource::seekTo(uint64_t off) {
    in_.clear();
    in_.seekg(static_cast<std::streamoff>(off));
    pos_ = off;
}

uint64_t BinaryRecordSource::currentOffset() const { return pos_; }
