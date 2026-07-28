#pragma once
// Innermost internal dependency (2 levels below the entry header) — proves the
// extractor's transitive include closure, not just direct includes.
#include <cstdint>

namespace wire {

inline constexpr char kMagic[4] = {'E', 'V', 'T', 'Q'};
inline constexpr unsigned kFileHeaderBytes = 64;
inline constexpr unsigned kRecordHeadBytes = 12;

enum class RecordKind : uint8_t {
    Sample = 0,
    Marker = 1,
    Fault  = 2,
};

} // namespace wire
