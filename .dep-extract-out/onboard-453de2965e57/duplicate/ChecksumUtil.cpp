// Out-of-line definition — must be compiled+linked by every consumer build.
#include "ChecksumUtil.hpp"

namespace chks {

uint32_t add(uint32_t acc, const void* data, std::size_t n)
{
    if (acc == 0) acc = 2166136261u;
    const auto* p = static_cast<const unsigned char*>(data);
    for (std::size_t i = 0; i < n; ++i) {
        acc ^= p[i];
        acc *= 16777619u;
    }
    return acc;
}

} // namespace chks
