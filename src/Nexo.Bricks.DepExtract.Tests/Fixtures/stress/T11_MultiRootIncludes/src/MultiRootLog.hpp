#pragma once
#include "api/RadarTypes.hpp"
#include "ByteCursor.hpp"
#include <optional>
#include <string>
#include <vector>

// Complexity tier 11: entry lives under src/, but headers resolve through two
// extra include roots (include/ + vendor/byteio/). Forces --include-dir usage
// and a multi-hop lower closure that is not next to the entry file.
class MultiRootLog {
public:
    explicit MultiRootLog(const std::string& path);
    bool pullContact(radar::Contact& out);
    void jumpTo(uint64_t byteOffset);
    uint64_t bytePos() const;
    std::vector<std::string> fieldNames() const;

private:
    ByteCursor cursor_;
    std::vector<std::string> fields_;
};
