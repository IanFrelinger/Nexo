#pragma once
// Mock proprietary: nextEvent naming (scaffold should map the pull alias).
#include <cstdint>
#include <filesystem>
#include <fstream>
#include <stdexcept>
#include <string>

class SensorTap {
public:
    explicit SensorTap(const std::filesystem::path& path)
        : path_(path), in_(path, std::ios::binary)
    {
        if (!in_)
            throw std::runtime_error("SensorTap: cannot open " + path.string());
        in_.read(reinterpret_cast<char*>(&count_), sizeof(count_));
        if (!in_) count_ = 0;
    }

    bool nextEvent(long& tick_ms)
    {
        if (index_ >= count_) return false;
        float sample = 0;
        in_.read(reinterpret_cast<char*>(&sample), sizeof(sample));
        if (!in_) return false;
        tick_ms = static_cast<long>(index_ * 10);
        last_value_ = sample;
        index_++;
        return true;
    }

    float last_value() const { return last_value_; }
    uint64_t event_count() const { return count_; }
    double t_begin() const { return 0; }
    double t_end() const { return static_cast<double>(count_) * 0.01; }

private:
    std::filesystem::path path_;
    std::ifstream in_;
    uint64_t count_ = 0;
    uint64_t index_ = 0;
    float last_value_ = 0;
};
