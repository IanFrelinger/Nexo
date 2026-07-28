#pragma once
// Companion header type for P03 (not the pull surface).
#include <cstdint>
#include <string>

struct FlightHeader {
    uint32_t magic = 0;
    uint64_t records = 0;
    std::string mission;
};
