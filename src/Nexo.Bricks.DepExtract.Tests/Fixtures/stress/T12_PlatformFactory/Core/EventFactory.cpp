#include "EventFactory.hpp"
#ifdef USE_PLATFORM_B
#include "../PlatformB/StreamB.hpp"
#else
#include "../PlatformA/StreamA.hpp"
#endif

std::unique_ptr<IPlatformStream> openPlatformStream(const std::string& path) {
#ifdef USE_PLATFORM_B
    return std::unique_ptr<IPlatformStream>(new StreamB(path));
#else
    return std::unique_ptr<IPlatformStream>(new StreamA(path));
#endif
}
