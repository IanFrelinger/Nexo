#pragma once
#include <QFile>
#include <QDataStream>
#include <QVector>
#include <cstdint>

// Large-scale example: a table-of-contents-indexed multi-section archive,
// via QFile/QDataStream. Same shape as the plain-C++ T10 tier (the hardest
// one) — select a section, read within it, advance to the next section.
struct SectionEntry { quint32 kind; quint64 offset; quint64 count; };
struct FlightRecord { double t; quint32 kind; float value; };

class FlightRecorderArchive {
public:
    explicit FlightRecorderArchive(const QString& path);

    const QVector<SectionEntry>& sections() const;
    bool selectSection(quint32 kind);
    bool nextRecord(FlightRecord& out);
    void seekWithinSection(quint64 recordOffset);
    quint64 positionInSection() const;
    bool advanceToNextSection();

private:
    QFile file_;
    QDataStream in_;
    QVector<SectionEntry> toc_;
    int currentSectionIdx_ = -1;
    quint64 recordsReadInSection_ = 0;
};
