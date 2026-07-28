#pragma once
// Mock proprietary: two-class surface; body reader is the pull API (scaffoldable).
#include "FlightHeader.hpp"
#include <cstdint>
#include <filesystem>
#include <fstream>
#include <stdexcept>

class FlightBodyReader {
public:
    explicit FlightBodyReader(const std::filesystem::path& path)
        : path_(path), in_(path, std::ios::binary)
    {
        if (!in_)
            throw std::runtime_error("FlightBodyReader: cannot open " + path.string());
        in_.read(reinterpret_cast<char*>(&hdr_.magic), sizeof(hdr_.magic));
        in_.read(reinterpret_cast<char*>(&hdr_.records), sizeof(hdr_.records));
        if (!in_) hdr_.records = 0;
    }

    bool read(long& tick_ms)
    {
        if (index_ >= hdr_.records) return false;
        double t = 0;
        in_.read(reinterpret_cast<char*>(&t), sizeof(t));
        if (!in_) return false;
        tick_ms = static_cast<long>(t * 1000.0);
        index_++;
        return true;
    }

    const FlightHeader& header() const { return hdr_; }
    uint64_t event_count() const { return hdr_.records; }
    double t_begin() const { return 0; }
    double t_end() const { return static_cast<double>(hdr_.records); }

private:
    std::filesystem::path path_;
    std::ifstream in_;
    FlightHeader hdr_{};
    uint64_t index_ = 0;
};
