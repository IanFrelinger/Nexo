#pragma once
// Proprietary-style EVTQ reader used for DepExtract → Poco front-to-back E2E.
// On-disk layout matches Poco tools/gen_evtb.cpp (EVTQ demo queue).
#include <cstdint>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <stdexcept>
#include <string>
#include <vector>

class MockEvtqStream {
public:
    explicit MockEvtqStream(const std::filesystem::path& path)
        : path_(path), in_(path, std::ios::binary)
    {
        if (!in_)
            throw std::runtime_error("MockEvtqStream: cannot open " + path.string());
        char hdr[64] = {};
        in_.read(hdr, 64);
        if (!in_ || std::memcmp(hdr, "EVTQ", 4) != 0)
            throw std::runtime_error("MockEvtqStream: not an EVTQ file");
        std::memcpy(&ver_, hdr + 4, 4);
        std::memcpy(&count_, hdr + 8, 8);
        std::memcpy(&t0_, hdr + 16, 8);
        std::memcpy(&t1_, hdr + 24, 8);
        (void)ver_;
    }

    /// Pull next event. tick_ms is timestamp in milliseconds; last_type() has the type byte.
    bool advance(long& tick_ms)
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
            if (nf >= 1) {
                float v0 = 0;
                std::memcpy(&v0, payload.data(), 4);
                last_value_ = v0;
            }
        }
        tick_ms = static_cast<long>(t * 1000.0);
        index_++;
        return true;
    }

    long last_type() const { return last_type_; }
    float last_value() const { return last_value_; }
    uint64_t event_count() const { return count_; }
    double t_begin() const { return t0_; }
    double t_end() const { return t1_; }
    const std::filesystem::path& path() const { return path_; }

private:
    std::filesystem::path path_;
    std::ifstream in_;
    uint32_t ver_ = 0;
    uint64_t count_ = 0;
    double t0_ = 0;
    double t1_ = 0;
    uint64_t index_ = 0;
    long last_type_ = 0;
    float last_value_ = 0;
};
