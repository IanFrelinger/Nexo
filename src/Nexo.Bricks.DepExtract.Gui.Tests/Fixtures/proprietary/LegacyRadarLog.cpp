#include "LegacyRadarLog.hpp"
#include <cstring>

// header: [4B magic "RDR1"][u64 declaredCount]
// record: [f64 timestampSeconds][u8 contactType][u16 payloadLen][payloadLen bytes]
LegacyRadarLog::LegacyRadarLog(const std::string& path) : stream_(path, std::ios::binary) {
    char magic[4];
    stream_.read(magic, 4);
    stream_.read(reinterpret_cast<char*>(&declaredCount_), 8);
    offset_ = 12;
}

bool LegacyRadarLog::readNextContact(RadarContact& out) {
    char hdr[11];
    if (!stream_.read(hdr, 11)) return false;
    std::memcpy(&out.timestampSeconds, hdr, 8);
    out.contactType = static_cast<uint8_t>(hdr[8]);
    uint16_t payloadLen;
    std::memcpy(&payloadLen, hdr + 9, 2);
    out.rawPayload.resize(payloadLen);
    if (payloadLen) stream_.read(reinterpret_cast<char*>(out.rawPayload.data()), payloadLen);
    offset_ += 11 + payloadLen;
    return true;
}

void LegacyRadarLog::seekToByteOffset(uint64_t offset) {
    stream_.clear();
    stream_.seekg(static_cast<std::streamoff>(offset));
    offset_ = offset;
}

uint64_t LegacyRadarLog::currentByteOffset() const { return offset_; }
uint64_t LegacyRadarLog::declaredContactCount() const { return declaredCount_; }
