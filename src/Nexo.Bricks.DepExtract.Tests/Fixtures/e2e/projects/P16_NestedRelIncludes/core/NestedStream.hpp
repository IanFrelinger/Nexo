#pragma once
// Nested relative includes: util types via ../ and same-dir companion via "".
// Wire layout matches the shared EVTQ fixture (64-byte file header, 12-byte
// record head, nfields*4 payload) so onboard oracle against mock.evtq succeeds.
#include "../util/Types.hpp"
#include "Checksum.hpp"
#include <cstdint>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <stdexcept>
#include <string>
#include <vector>

class NestedStream {
public:
    static constexpr unsigned kFileHeaderBytes = 64;
    static constexpr unsigned kRecordHeadBytes = 12;

    explicit NestedStream(const std::filesystem::path& path)
        : path_(path), in_(path, std::ios::binary)
    {
        if (!in_) throw std::runtime_error("NestedStream: cannot open " + path.string());
        char hdr[kFileHeaderBytes] = {};
        in_.read(hdr, kFileHeaderBytes);
        if (!in_ || std::memcmp(hdr, "EVTQ", 4) != 0)
            throw std::runtime_error("NestedStream: not an EVTQ file");
        std::memcpy(&count_, hdr + 8, 8);
        std::memcpy(&t0_, hdr + 16, 8);
        std::memcpy(&t1_, hdr + 24, 8);
        (void)path_;
    }

    bool pull(long& tick_ms)
    {
        unsigned char head[kRecordHeadBytes];
        in_.read(reinterpret_cast<char*>(head), kRecordHeadBytes);
        if (!in_ || in_.gcount() != static_cast<std::streamsize>(kRecordHeadBytes)) return false;
        last_type_ = head[0];
        const unsigned nfields = head[1];
        double t = 0;
        std::memcpy(&t, head + 4, 8);
        std::vector<char> payload(static_cast<size_t>(nfields) * 4u);
        if (!payload.empty()) {
            in_.read(payload.data(), static_cast<std::streamsize>(payload.size()));
            if (!in_ || static_cast<size_t>(in_.gcount()) != payload.size()) return false;
            nest::add(checksum_, payload.data(), payload.size());
        }
        tick_ms = static_cast<long>(t * 1000.0);
        nest::add(checksum_, &tick_ms, sizeof(tick_ms));
        return true;
    }

    long last_type() const { return last_type_; }
    uint64_t event_count() const { return count_; }
    double t_begin() const { return t0_; }
    double t_end() const { return t1_; }

private:
    std::filesystem::path path_;
    std::ifstream in_;
    long last_type_ = 0;
    std::uint32_t checksum_ = nest::kSeed;
    uint64_t count_ = 0;
    double t0_ = 0;
    double t1_ = 0;
};
