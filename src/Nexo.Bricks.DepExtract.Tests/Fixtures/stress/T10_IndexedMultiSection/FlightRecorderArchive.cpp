#include "FlightRecorderArchive.hpp"
#include <cstring>

// header: [4B magic "FLT1"][u32 sectionCount], then sectionCount x
// [u32 kind][u64 offset][u64 count]. record (within a section, at `offset`):
// [f64 t][u32 kind][f32 value] (16B), `count` of them back-to-back.
FlightRecorderArchive::FlightRecorderArchive(const std::string& path) : in_(path, std::ios::binary) {
    char magic[4];
    in_.read(magic, 4);
    uint32_t sectionCount = 0;
    in_.read(reinterpret_cast<char*>(&sectionCount), 4);
    toc_.resize(sectionCount);
    for (auto& e : toc_) {
        in_.read(reinterpret_cast<char*>(&e.kind), 4);
        in_.read(reinterpret_cast<char*>(&e.offset), 8);
        in_.read(reinterpret_cast<char*>(&e.count), 8);
    }
}

const std::vector<SectionEntry>& FlightRecorderArchive::sections() const { return toc_; }

bool FlightRecorderArchive::selectSection(uint32_t kind) {
    for (size_t i = 0; i < toc_.size(); ++i) {
        if (toc_[i].kind == kind) {
            currentSectionIdx_ = i;
            recordsReadInSection_ = 0;
            in_.clear();
            in_.seekg(static_cast<std::streamoff>(toc_[i].offset));
            return true;
        }
    }
    return false;
}

bool FlightRecorderArchive::nextRecord(FlightRecord& out) {
    if (currentSectionIdx_ >= toc_.size()) return false;
    const auto& sec = toc_[currentSectionIdx_];
    if (recordsReadInSection_ >= sec.count) return false;
    char buf[16];
    if (!in_.read(buf, 16)) return false;
    std::memcpy(&out.t, buf, 8);
    std::memcpy(&out.kind, buf + 8, 4);
    std::memcpy(&out.value, buf + 12, 4);
    ++recordsReadInSection_;
    return true;
}

void FlightRecorderArchive::seekWithinSection(uint64_t recordOffset) {
    const auto& sec = toc_[currentSectionIdx_];
    in_.clear();
    in_.seekg(static_cast<std::streamoff>(sec.offset + recordOffset * 16));
    recordsReadInSection_ = recordOffset;
}

uint64_t FlightRecorderArchive::positionInSection() const { return recordsReadInSection_; }

bool FlightRecorderArchive::advanceToNextSection() {
    if (currentSectionIdx_ + 1 >= toc_.size()) return false;
    ++currentSectionIdx_;
    recordsReadInSection_ = 0;
    in_.clear();
    in_.seekg(static_cast<std::streamoff>(toc_[currentSectionIdx_].offset));
    return true;
}
