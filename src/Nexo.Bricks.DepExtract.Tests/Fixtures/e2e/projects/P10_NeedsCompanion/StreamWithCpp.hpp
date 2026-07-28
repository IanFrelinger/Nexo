#pragma once
// Hostile mock: pull API declared here, defined only in StreamWithCpp.cpp (companion required).
#include <cstdint>
#include <filesystem>
#include <fstream>
#include <string>

class StreamWithCpp {
public:
    explicit StreamWithCpp(const std::filesystem::path& path);
    bool advance(long& tick_ms);
    long last_type() const { return last_type_; }
    uint64_t event_count() const { return count_; }
    double t_begin() const { return 0; }
    double t_end() const { return static_cast<double>(count_); }

private:
    std::ifstream in_;
    uint64_t count_ = 0;
    uint64_t index_ = 0;
    long last_type_ = 0;
};
