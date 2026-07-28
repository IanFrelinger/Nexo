#pragma once
// Mock proprietary: trivial millisecond tick puller (scaffoldable).
#include <cstdint>
#include <filesystem>
#include <fstream>
#include <stdexcept>
#include <string>

class SimpleTickStream {
public:
    explicit SimpleTickStream(const std::filesystem::path& path)
        : path_(path), in_(path, std::ios::binary)
    {
        if (!in_)
            throw std::runtime_error("SimpleTickStream: cannot open " + path.string());
        in_.read(reinterpret_cast<char*>(&count_), sizeof(count_));
        if (!in_) count_ = 0;
    }

    bool advance(long& tick_ms)
    {
        if (index_ >= count_) return false;
        double t = 0;
        in_.read(reinterpret_cast<char*>(&t), sizeof(t));
        if (!in_) return false;
        tick_ms = static_cast<long>(t * 1000.0);
        last_type_ = static_cast<long>(index_ % 3);
        index_++;
        return true;
    }

    long last_type() const { return last_type_; }
    uint64_t event_count() const { return count_; }
    double t_begin() const { return 0; }
    double t_end() const { return static_cast<double>(count_); }

private:
    std::filesystem::path path_;
    std::ifstream in_;
    uint64_t count_ = 0;
    uint64_t index_ = 0;
    long last_type_ = 0;
};
