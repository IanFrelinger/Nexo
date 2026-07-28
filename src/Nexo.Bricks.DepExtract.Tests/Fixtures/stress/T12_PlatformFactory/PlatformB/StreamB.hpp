#pragma once
#include "../Core/EventFactory.hpp"
#include <fstream>

class StreamB : public IPlatformStream {
public:
    explicit StreamB(const std::string& path) : in_(path, std::ios::binary) {}
    bool nextEvent(PlatformEvent& out) override {
        if (!in_.read(reinterpret_cast<char*>(&out.t), sizeof(out.t))) return false;
        if (!in_.read(reinterpret_cast<char*>(&out.type), sizeof(out.type))) return false;
        out.name = "platformB";
        out.value = "ok";
        pos_ = static_cast<uint64_t>(in_.tellg());
        return true;
    }
    void seekAbs(uint64_t off) override {
        in_.clear();
        in_.seekg(static_cast<std::streamoff>(off));
        pos_ = off;
    }
    uint64_t tellAbs() const override { return pos_; }

private:
    std::ifstream in_;
    uint64_t pos_ = 0;
};
