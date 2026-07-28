#pragma once
// Internal helper DECLARED here but DEFINED out-of-line in ChecksumUtil.cpp —
// the hostile case for install: the companion .cpp must survive extraction,
// install into poco/common/, and be linked into the final image build.
#include <cstddef>
#include <cstdint>

namespace chks {

/// FNV-1a accumulate over a byte range (desktop app integrity counter).
uint32_t add(uint32_t acc, const void* data, std::size_t n);

} // namespace chks
