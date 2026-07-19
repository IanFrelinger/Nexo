#pragma once
#ifdef USE_PLATFORM_B
#include "../PlatformB/Impl.hpp"
#else
#include "../PlatformA/Impl.hpp"
#endif
struct Engine { const char* name = platformName(); };
