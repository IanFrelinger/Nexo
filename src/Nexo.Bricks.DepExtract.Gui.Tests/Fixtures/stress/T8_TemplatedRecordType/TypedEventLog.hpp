#pragma once
#include <cstring>
#include <fstream>
#include <string>

// Complexity tier 8: a template class (necessarily header-only, no .cpp).
// Tests whether the adapter picks a concrete instantiation (TypedEventLog
// <SensorPayload> below) rather than trying to use the template itself.
struct SensorPayload { float x, y, z; };

template <typename PayloadT>
class TypedEventLog {
public:
    explicit TypedEventLog(const std::string& path) : in_(path, std::ios::binary) {}

    bool readTyped(double& t, PayloadT& payload) {
        char tbuf[8];
        if (!in_.read(tbuf, 8)) return false;
        std::memcpy(&t, tbuf, 8);
        if (!in_.read(reinterpret_cast<char*>(&payload), sizeof(PayloadT))) return false;
        pos_ += 8 + sizeof(PayloadT);
        return true;
    }

    void rewindTo(uint64_t offset) {
        in_.clear();
        in_.seekg(static_cast<std::streamoff>(offset));
        pos_ = offset;
    }

    uint64_t offset() const { return pos_; }

private:
    std::ifstream in_;
    uint64_t pos_ = 0;
};

using SensorLog = TypedEventLog<SensorPayload>;
