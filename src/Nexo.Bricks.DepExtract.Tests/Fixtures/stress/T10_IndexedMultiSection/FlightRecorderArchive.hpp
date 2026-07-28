#pragma once
#include <cstdint>
#include <fstream>
#include <string>
#include <vector>

// Complexity tier 10: hardest. A table-of-contents read at construction
// lists multiple sections by "kind"; you must selectSection() before
// nextRecord() reads within it, and advanceToNextSection() to move on once
// a section is exhausted — there's no single flat scan across the whole
// file. This is a genuinely hard fit for IEventReader's single next() loop;
// the honest answer may require the adapter to flag that mapping this
// correctly needs a multi-section iteration loop inside next() itself.
struct SectionEntry { uint32_t kind; uint64_t offset; uint64_t count; };
struct FlightRecord { double t; uint32_t kind; float value; };

class FlightRecorderArchive {
public:
    explicit FlightRecorderArchive(const std::string& path);

    const std::vector<SectionEntry>& sections() const;
    bool selectSection(uint32_t kind);       // must call before nextRecord() for that kind
    bool nextRecord(FlightRecord& out);      // reads within the currently selected section only
    void seekWithinSection(uint64_t recordOffset);
    uint64_t positionInSection() const;
    bool advanceToNextSection();             // moves to the next TOC entry after the current one is exhausted

private:
    std::ifstream in_;
    std::vector<SectionEntry> toc_;
    size_t currentSectionIdx_ = 0;
    uint64_t recordsReadInSection_ = 0;
};
