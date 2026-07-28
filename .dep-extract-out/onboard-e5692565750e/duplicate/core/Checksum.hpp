#pragma once
#include <cstdint>
#include <cstddef>

namespace nest {
constexpr std::uint32_t kSeed = 0xA5A5A5A5u;
void add(std::uint32_t& acc, const void* data, std::size_t n);
}
