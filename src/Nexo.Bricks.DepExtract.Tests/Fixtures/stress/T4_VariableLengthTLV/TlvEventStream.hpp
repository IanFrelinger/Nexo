#pragma once
#include <cstdint>
#include <fstream>
#include <string>
#include <vector>

// Complexity tier 4: variable-length TLV (tag-length-value) records — the
// adapter must realize payload size isn't fixed and depends on a per-record
// length prefix, matching evtx's own lazy max_depth model reasonably closely.
struct TlvEvent { double t; uint16_t tag; std::vector<uint8_t> value; };

class TlvEventStream {
public:
    explicit TlvEventStream(const std::string& path);
    bool nextEvent(TlvEvent& out);
    void seekBytes(uint64_t off);
    uint64_t bytesRead() const;

private:
    std::ifstream in_;
    uint64_t pos_ = 0;
};
