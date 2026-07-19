#pragma once
#include <QFile>
#include <QDataStream>
#include <QString>
#include <cstdint>

// Small-scale example: a Qt-native parser (QFile/QDataStream, not std::ifstream)
// for a small header + fixed-size-record format. Real seek support. This is
// the unit dep-extract should pull out of the surrounding Qt application.
struct TelemetrySample { double timestampSec; quint8 channel; float reading; };

class TelemetryStream {
public:
    explicit TelemetryStream(const QString& path);
    bool fetchNext(TelemetrySample& out);
    void jumpTo(quint64 byteOffset);
    quint64 tell() const;
    quint32 sampleCount() const;

private:
    QFile file_;
    QDataStream in_;
    quint64 pos_ = 0;
    quint32 count_ = 0;
};
