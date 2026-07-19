#include "ChunkedTraceLog.hpp"
#include <cstring>

// file: sequence of chunks, each [u32 rawLen][rawLen XOR-"compressed" bytes]
// decompressed chunk: sequence of [f64 t][u32 code] (12B) events
ChunkedTraceLog::ChunkedTraceLog(const std::string& path) : in_(path, std::ios::binary) {}

std::vector<uint8_t> ChunkedTraceLog::decompressChunk(const std::vector<uint8_t>& raw) {
    std::vector<uint8_t> out(raw.size());
    for (size_t i = 0; i < raw.size(); ++i) out[i] = raw[i] ^ 0xA5;
    return out;
}

bool ChunkedTraceLog::loadNextChunk() {
    uint32_t rawLen;
    if (!in_.read(reinterpret_cast<char*>(&rawLen), 4)) return false;
    std::vector<uint8_t> raw(rawLen);
    if (rawLen && !in_.read(reinterpret_cast<char*>(raw.data()), rawLen)) return false;
    decompressedBuffer_ = decompressChunk(raw);
    bufferPos_ = 0;
    ++chunkIndex_;
    return true;
}

bool ChunkedTraceLog::nextTraceEvent(TraceEvent& out) {
    if (bufferPos_ + 12 > decompressedBuffer_.size()) {
        if (!loadNextChunk()) return false;
        if (decompressedBuffer_.size() < 12) return false;
    }
    std::memcpy(&out.t, decompressedBuffer_.data() + bufferPos_, 8);
    std::memcpy(&out.code, decompressedBuffer_.data() + bufferPos_ + 8, 4);
    bufferPos_ += 12;
    return true;
}

void ChunkedTraceLog::seekToChunk(uint32_t chunkIndex) {
    in_.clear();
    in_.seekg(0);
    decompressedBuffer_.clear();
    bufferPos_ = 0;
    chunkIndex_ = 0;
    while (chunkIndex_ < chunkIndex && loadNextChunk()) { /* skip forward */ }
}

uint32_t ChunkedTraceLog::currentChunk() const { return chunkIndex_; }
