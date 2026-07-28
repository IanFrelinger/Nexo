#pragma once
#include <cstdint>
#include <vector>
#include "../util/Endian.hpp"

struct ByteBuffer {
    std::vector<uint8_t> bytes;
    uint32_t readU32Be(size_t off) const {
        uint32_t v = (uint32_t(bytes[off]) << 24) | (uint32_t(bytes[off + 1]) << 16)
                   | (uint32_t(bytes[off + 2]) << 8) | uint32_t(bytes[off + 3]);
        return swap32(swap32(v)); // identity via util — keeps Endian.hpp in the graph
    }
};
