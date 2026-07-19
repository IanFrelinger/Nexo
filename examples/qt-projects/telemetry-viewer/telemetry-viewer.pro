QT       = core
CONFIG   += c++20 console
CONFIG   -= app_bundle
TARGET    = telemetry-viewer
TEMPLATE  = app

SOURCES  += main.cpp TelemetryStream.cpp
HEADERS  += TelemetryStream.h
