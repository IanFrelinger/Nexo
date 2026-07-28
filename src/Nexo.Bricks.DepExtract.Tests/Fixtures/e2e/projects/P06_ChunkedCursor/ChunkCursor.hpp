#pragma once
// Mock proprietary: chunked cursor with seek (irregular → model).
#include <cstdint>
#include <filesystem>
#include <fstream>
#include <string>
#include <vector>

struct DataChunk {
    uint64_t byteOffset = 0;
    double t0 = 0;
    double t1 = 0;
    std::vector<uint8_t> bytes;
};

class ChunkCursor {
public:
    explicit ChunkCursor(const std::filesystem::path& path);

    bool openSection(const std::string& name);
    bool readChunk(DataChunk& out);
    bool seekTo(uint64_t byteOffset);
    uint64_t tell() const;
    uint32_t sectionCount() const;
    std::string sectionName(uint32_t index) const;

private:
    std::filesystem::path path_;
    std::ifstream in_;
    uint64_t pos_ = 0;
    uint32_t sections_ = 0;
    std::string active_;
};
