#pragma once
// EVTQ-compatible twin of MockEvtqParser using pull() naming for scaffold+extract E2E.
#include <cstdint>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <stdexcept>
#include <string>
#include <vector>

class TwinEvtqStream {
public:
    explicit TwinEvtqStream(const std::filesystem::path& path)
        : path_(path), in_(path, std::ios::binary)
    {
        if (!in_)
            throw std::runtime_error("TwinEvtqStream: cannot open " + path.string());
        char hdr[64] = {};
        in_.read(hdr, 64);
        if (!in_ || std::memcmp(hdr, "EVTQ", 4) != 0)
            throw std::runtime_error("TwinEvtqStream: not an EVTQ file");
        std::memcpy(&ver_, hdr + 4, 4);
        std::memcpy(&count_, hdr + 8, 8);
        std::memcpy(&t0_, hdr + 16, 8);
        std::memcpy(&t1_, hdr + 24, 8);
        (void)ver_;
    }

    /// Same on-disk EVTQ layout as MockEvtqStream / gen_evtb; pull alias for scaffold mapping.
    bool pull(long& tick_ms)
    {
        if (!in_) return false;
        unsigned char head[12];
        in_.read(reinterpret_cast<char*>(head), 12);
        if (!in_ || in_.gcount() != 12) return false;
        last_type_ = head[0];
        const unsigned nf = head[1];
        double t = 0;
        std::memcpy(&t, head + 4, 8);
        std::vector<char> payload(static_cast<size_t>(nf) * 4u);
        if (nf) {
            in_.read(payload.data(), static_cast<std::streamsize>(payload.size()));
            if (!in_ || static_cast<size_t>(in_.gcount()) != payload.size()) return false;
        }
        tick_ms = static_cast<long>(t * 1000.0);
        index_++;
        return true;
    }

    long last_type() const { return last_type_; }
    uint64_t event_count() const { return count_; }
    double t_begin() const { return t0_; }
    double t_end() const { return t1_; }

private:
    std::filesystem::path path_;
    std::ifstream in_;
    uint32_t ver_ = 0;
    uint64_t count_ = 0;
    double t0_ = 0;
    double t1_ = 0;
    uint64_t index_ = 0;
    long last_type_ = 0;
};
