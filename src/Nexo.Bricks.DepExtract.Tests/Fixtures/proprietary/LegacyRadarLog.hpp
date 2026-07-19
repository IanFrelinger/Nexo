#pragma once
#include <cstdint>
#include <fstream>
#include <string>
#include <vector>

// A stand-in for an arbitrary proprietary binary log parser — distinctive
// method names on purpose, so a test can verify an adapter actually looked
// at THIS API rather than emitting generic boilerplate.
struct RadarContact {
    double timestampSeconds = 0;
    uint8_t contactType = 0;
    std::vector<uint8_t> rawPayload;
};

class LegacyRadarLog {
public:
    explicit LegacyRadarLog(const std::string& path);

    // Reads one contact record. Returns false at end-of-stream.
    bool readNextContact(RadarContact& out);

    // Seeks the underlying stream to a raw byte offset.
    void seekToByteOffset(uint64_t offset);

    // Byte offset of the next record that would be read.
    uint64_t currentByteOffset() const;

    // Total contact count declared in the file header.
    uint64_t declaredContactCount() const;

private:
    std::ifstream stream_;
    uint64_t offset_ = 0;
    uint64_t declaredCount_ = 0;
};
