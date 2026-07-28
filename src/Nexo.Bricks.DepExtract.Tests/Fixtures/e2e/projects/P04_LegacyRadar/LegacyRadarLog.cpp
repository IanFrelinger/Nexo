#include "LegacyRadarLog.hpp"
#include <cstring>
#include <stdexcept>

LegacyRadarLog::LegacyRadarLog(const std::string& path)
    : stream_(path, std::ios::binary)
{
    if (!stream_)
        throw std::runtime_error("LegacyRadarLog: cannot open " + path);
    char magic[4] = {};
    stream_.read(magic, 4);
    if (!stream_ || std::memcmp(magic, "RADR", 4) != 0)
        throw std::runtime_error("LegacyRadarLog: bad magic");
    stream_.read(reinterpret_cast<char*>(&declaredCount_), sizeof(declaredCount_));
    offset_ = static_cast<uint64_t>(stream_.tellg());
}

bool LegacyRadarLog::readNextContact(RadarContact& out)
{
    if (!stream_) return false;
    double ts = 0;
    uint8_t ty = 0;
    uint16_t n = 0;
    stream_.read(reinterpret_cast<char*>(&ts), sizeof(ts));
    stream_.read(reinterpret_cast<char*>(&ty), sizeof(ty));
    stream_.read(reinterpret_cast<char*>(&n), sizeof(n));
    if (!stream_) return false;
    out.timestampSeconds = ts;
    out.contactType = ty;
    out.rawPayload.resize(n);
    if (n)
        stream_.read(reinterpret_cast<char*>(out.rawPayload.data()), n);
    offset_ = static_cast<uint64_t>(stream_.tellg());
    return static_cast<bool>(stream_) || n == 0;
}

void LegacyRadarLog::seekToByteOffset(uint64_t offset)
{
    stream_.clear();
    stream_.seekg(static_cast<std::streamoff>(offset));
    offset_ = offset;
}

uint64_t LegacyRadarLog::currentByteOffset() const { return offset_; }
uint64_t LegacyRadarLog::declaredContactCount() const { return declaredCount_; }
