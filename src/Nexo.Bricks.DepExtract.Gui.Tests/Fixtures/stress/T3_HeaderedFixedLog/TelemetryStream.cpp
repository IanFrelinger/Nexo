#include "TelemetryStream.hpp"
#include <cstring>

// header: [4B magic "TLM1"][u32 count] ; record: [f64 t][u8 channel][f32 reading] (13B)
TelemetryStream::TelemetryStream(const std::string& path) : in_(path, std::ios::binary) {
    char magic[4];
    in_.read(magic, 4);
    in_.read(reinterpret_cast<char*>(&count_), 4);
    pos_ = 8;
}

bool TelemetryStream::fetchNext(TelemetrySample& out) {
    char buf[13];
    if (!in_.read(buf, 13)) return false;
    std::memcpy(&out.timestampSec, buf, 8);
    out.channel = static_cast<uint8_t>(buf[8]);
    std::memcpy(&out.reading, buf + 9, 4);
    pos_ += 13;
    return true;
}

void TelemetryStream::jumpTo(uint64_t byteOffset) {
    in_.clear();
    in_.seekg(static_cast<std::streamoff>(byteOffset));
    pos_ = byteOffset;
}

uint64_t TelemetryStream::tell() const { return pos_; }
uint32_t TelemetryStream::sampleCount() const { return count_; }
