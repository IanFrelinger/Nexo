QT       = core widgets
CONFIG   += c++20 console
CONFIG   -= app_bundle
TARGET    = flight-data-recorder-suite
TEMPLATE  = app

SOURCES  += main.cpp FlightRecorderArchive.cpp ExportDialog.cpp MainWindow.cpp
HEADERS  += FlightRecorderArchive.h ExportDialog.h MainWindow.h
