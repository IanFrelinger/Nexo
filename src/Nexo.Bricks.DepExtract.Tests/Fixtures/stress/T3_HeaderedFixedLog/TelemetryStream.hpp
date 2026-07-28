#pragma once
#include <cstdint>
#include <fstream>
#include <string>

// Complexity tier 3: fixed-size records plus a small header (magic + count),
// and real seek support. The "known good" shape — same tier as the parser
// used in the original adapter test.
struct TelemetrySample { double timestampSec; uint8_t channel; float reading; };

class TelemetryStream {
public:
    explicit TelemetryStream(const std::string& path);
    bool fetchNext(TelemetrySample& out);
    void jumpTo(uint64_t byteOffset);
    uint64_t tell() const;
    uint32_t sampleCount() const;

private:
    std::ifstream in_;
    uint64_t pos_ = 0;
    uint32_t count_ = 0;
};
