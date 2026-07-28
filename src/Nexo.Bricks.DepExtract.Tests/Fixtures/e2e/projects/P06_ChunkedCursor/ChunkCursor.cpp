#include "ChunkCursor.hpp"
#include <stdexcept>

ChunkCursor::ChunkCursor(const std::filesystem::path& path)
    : path_(path), in_(path, std::ios::binary)
{
    if (!in_)
        throw std::runtime_error("ChunkCursor: cannot open " + path.string());
    in_.read(reinterpret_cast<char*>(&sections_), sizeof(sections_));
    if (!in_) sections_ = 1;
    pos_ = static_cast<uint64_t>(in_.tellg());
}

bool ChunkCursor::openSection(const std::string& name)
{
    active_ = name;
    return !name.empty();
}

bool ChunkCursor::readChunk(DataChunk& out)
{
    if (!in_) return false;
    uint32_t n = 0;
    in_.read(reinterpret_cast<char*>(&n), sizeof(n));
    if (!in_) return false;
    out.byteOffset = pos_;
    out.t0 = static_cast<double>(pos_);
    out.t1 = out.t0 + 1.0;
    out.bytes.resize(n);
    if (n)
        in_.read(reinterpret_cast<char*>(out.bytes.data()), n);
    pos_ = static_cast<uint64_t>(in_.tellg());
    return true;
}

bool ChunkCursor::seekTo(uint64_t byteOffset)
{
    in_.clear();
    in_.seekg(static_cast<std::streamoff>(byteOffset));
    pos_ = byteOffset;
    return static_cast<bool>(in_);
}

uint64_t ChunkCursor::tell() const { return pos_; }
uint32_t ChunkCursor::sectionCount() const { return sections_; }
std::string ChunkCursor::sectionName(uint32_t index) const
{
    return "section-" + std::to_string(index);
}
