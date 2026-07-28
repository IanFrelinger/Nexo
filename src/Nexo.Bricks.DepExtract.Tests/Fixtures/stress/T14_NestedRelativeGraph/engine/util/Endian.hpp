#pragma once
#include <cstdint>

// Complexity tier 14: deep relative-include graph across engine/ + plugins/.
inline uint32_t swap32(uint32_t v) {
    return ((v & 0xffu) << 24) | ((v & 0xff00u) << 8)
         | ((v & 0xff0000u) >> 8) | ((v & 0xff000000u) >> 24);
}
