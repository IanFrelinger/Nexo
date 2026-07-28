#pragma once
#include <cstdint>
#include <memory>
#include <string>

struct PlatformEvent {
    double t;
    uint8_t type;
    std::string name;
    std::string value;
};

class IPlatformStream {
public:
    virtual ~IPlatformStream() = default;
    virtual bool nextEvent(PlatformEvent& out) = 0;
    virtual void seekAbs(uint64_t off) = 0;
    virtual uint64_t tellAbs() const = 0;
};

// Complexity tier 12: factory picks PlatformA vs PlatformB via compile define.
std::unique_ptr<IPlatformStream> openPlatformStream(const std::string& path);
