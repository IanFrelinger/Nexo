#pragma once
#include <cstdint>
#include <fstream>
#include <string>
#include <vector>

// Irregular proprietary binary log — forces model strategy (not scaffoldable).
struct RadarContact {
    double timestampSeconds = 0;
    uint8_t contactType = 0;
    std::vector<uint8_t> rawPayload;
};

class LegacyRadarLog {
public:
    explicit LegacyRadarLog(const std::string& path);

    bool readNextContact(RadarContact& out);
    void seekToByteOffset(uint64_t offset);
    uint64_t currentByteOffset() const;
    uint64_t declaredContactCount() const;

private:
    std::ifstream stream_;
    uint64_t offset_ = 0;
    uint64_t declaredCount_ = 0;
};
