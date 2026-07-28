#include "FlightRecorderArchive.h"

// header: [4B magic "FLT1"][u32 sectionCount], then sectionCount x
// [u32 kind][u64 offset][u64 count]. record (within a section, at `offset`):
// [f64 t][u32 kind][f32 value] (16B), `count` of them back-to-back. Little-endian.
FlightRecorderArchive::FlightRecorderArchive(const QString& path) : file_(path), in_(&file_) {
    file_.open(QIODevice::ReadOnly);
    in_.setByteOrder(QDataStream::LittleEndian);
    char magic[4];
    in_.readRawData(magic, 4);
    quint32 sectionCount = 0;
    in_ >> sectionCount;
    toc_.resize(static_cast<int>(sectionCount));
    for (auto& e : toc_) in_ >> e.kind >> e.offset >> e.count;
}

const QVector<SectionEntry>& FlightRecorderArchive::sections() const { return toc_; }

bool FlightRecorderArchive::selectSection(quint32 kind) {
    for (int i = 0; i < toc_.size(); ++i) {
        if (toc_[i].kind == kind) {
            currentSectionIdx_ = i;
            recordsReadInSection_ = 0;
            file_.seek(static_cast<qint64>(toc_[i].offset));
            return true;
        }
    }
    return false;
}

bool FlightRecorderArchive::nextRecord(FlightRecord& out) {
    if (currentSectionIdx_ < 0) return false;
    const auto& sec = toc_[currentSectionIdx_];
    if (recordsReadInSection_ >= sec.count) return false;
    double t; quint32 kind; float value;
    in_ >> t >> kind >> value;
    if (in_.status() != QDataStream::Ok) return false;
    out.t = t; out.kind = kind; out.value = value;
    ++recordsReadInSection_;
    return true;
}

void FlightRecorderArchive::seekWithinSection(quint64 recordOffset) {
    const auto& sec = toc_[currentSectionIdx_];
    file_.seek(static_cast<qint64>(sec.offset + recordOffset * 16));
    recordsReadInSection_ = recordOffset;
}

quint64 FlightRecorderArchive::positionInSection() const { return recordsReadInSection_; }

bool FlightRecorderArchive::advanceToNextSection() {
    if (currentSectionIdx_ + 1 >= toc_.size()) return false;
    ++currentSectionIdx_;
    recordsReadInSection_ = 0;
    file_.seek(static_cast<qint64>(toc_[currentSectionIdx_].offset));
    return true;
}
