#include "TelemetryStream.h"

// header: [4B magic "TLM1"][u32 count] ; record: [f64 t][u8 channel][f32 reading] (13B), little-endian.
TelemetryStream::TelemetryStream(const QString& path) : file_(path), in_(&file_) {
    file_.open(QIODevice::ReadOnly);
    in_.setByteOrder(QDataStream::LittleEndian);   // QDataStream defaults to big-endian — the classic Qt-parser gotcha
    char magic[4];
    in_.readRawData(magic, 4);
    in_ >> count_;
    pos_ = 8;
}

bool TelemetryStream::fetchNext(TelemetrySample& out) {
    if (file_.atEnd()) return false;
    double t; quint8 channel; float reading;
    in_ >> t >> channel >> reading;
    if (in_.status() != QDataStream::Ok) return false;
    out.timestampSec = t; out.channel = channel; out.reading = reading;
    pos_ += 13;
    return true;
}

void TelemetryStream::jumpTo(quint64 byteOffset) {
    file_.seek(static_cast<qint64>(byteOffset));
    pos_ = byteOffset;
}

quint64 TelemetryStream::tell() const { return pos_; }
quint32 TelemetryStream::sampleCount() const { return count_; }
