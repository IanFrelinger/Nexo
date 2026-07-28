#include "StreamWithCpp.hpp"
#include <stdexcept>

StreamWithCpp::StreamWithCpp(const std::filesystem::path& path)
    : in_(path, std::ios::binary)
{
    if (!in_)
        throw std::runtime_error("StreamWithCpp: cannot open " + path.string());
    in_.read(reinterpret_cast<char*>(&count_), sizeof(count_));
    if (!in_) count_ = 0;
}

bool StreamWithCpp::advance(long& tick_ms)
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
