QT       = core widgets
CONFIG   += c++20 console
CONFIG   -= app_bundle
TARGET    = session-log-analyzer
TEMPLATE  = app

SOURCES  += main.cpp SessionLog.cpp SessionWindow.cpp
HEADERS  += SessionLog.h SessionWindow.h
