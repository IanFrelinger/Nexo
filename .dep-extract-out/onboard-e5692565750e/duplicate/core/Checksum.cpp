#include "Checksum.hpp"

namespace nest {
void add(std::uint32_t& acc, const void* data, std::size_t n)
{
    const auto* p = static_cast<const unsigned char*>(data);
    for (std::size_t i = 0; i < n; ++i)
        acc = (acc * 33u) ^ p[i];
}
}
