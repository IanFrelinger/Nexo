#pragma once
#include <cstdint>
#include <fstream>
#include <string>
#include <vector>

// Complexity tier 9: records are packed into compressed chunks; the public
// API only exposes per-event reads and per-CHUNK seeking (not per-byte or
// per-record). Internal buffer refill logic is private. Tests whether the
// adapter respects the public/private boundary (never calls loadNextChunk
// or decompressChunk directly) and honestly represents that seeking is
// chunk-granular, coarser than IEventReader's byte-offset resume_at().
struct TraceEvent { double t; uint32_t code; };

class ChunkedTraceLog {
public:
    explicit ChunkedTraceLog(const std::string& path);
    bool nextTraceEvent(TraceEvent& out);
    void seekToChunk(uint32_t chunkIndex);
    uint32_t currentChunk() const;

private:
    bool loadNextChunk();
    static std::vector<uint8_t> decompressChunk(const std::vector<uint8_t>& raw);

    std::ifstream in_;
    std::vector<uint8_t> decompressedBuffer_;
    size_t bufferPos_ = 0;
    uint32_t chunkIndex_ = 0;
};
