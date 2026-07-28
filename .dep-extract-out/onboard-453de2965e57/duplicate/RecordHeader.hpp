#pragma once
// Mid-level internal dependency: record head decode shared across the desktop
// app. Includes WireTypes.hpp so the closure must walk entry -> this -> wire.
#include <cstdint>
#include <cstring>

#include "WireTypes.hpp"

namespace wire {

struct RecordHead {
    uint8_t raw_kind = 0;
    RecordKind kind = RecordKind::Sample;
    unsigned nfields = 0;
    double t = 0.0;
};

/// Decodes the fixed 12-byte record head (kind, field count, f64 timestamp).
inline bool decode_head(const unsigned char (&raw)[kRecordHeadBytes], RecordHead& out)
{
    out.raw_kind = raw[0];
    out.kind = static_cast<RecordKind>(raw[0]);
    out.nfields = raw[1];
    std::memcpy(&out.t, raw + 4, sizeof(double));
    return true;
}

} // namespace wire
