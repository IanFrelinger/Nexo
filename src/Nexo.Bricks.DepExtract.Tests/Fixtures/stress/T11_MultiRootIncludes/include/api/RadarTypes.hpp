#pragma once
#include <cstdint>
#include <string>

namespace radar {
struct Contact {
    double t;
    uint32_t kind;
    std::string label;
};
enum class ContactKind : uint32_t { Track = 1, Emit = 2, Health = 3 };
} // namespace radar
