#pragma once
// Entry header for the internal-deps fixture: EVTQ decode built on the app's
// OWN internal types (RecordHeader -> WireTypes transitively, plus an
// out-of-line ChecksumUtil.cpp helper), with Qt floated warnings like the real
// desktop app. Exercises: transitive closure extraction, .cpp companion
// install/link, and Qt shimming — together.
#include <cstdint>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <stdexcept>
#include <string>
#include <vector>

#include <QMessageBox>
#include <QString>

#include "ChecksumUtil.hpp"
#include "RecordHeader.hpp"

class InternalDepsStream {
public:
    explicit InternalDepsStream(const std::filesystem::path& path)
        : path_(path), in_(path, std::ios::binary)
    {
        if (!in_) {
            warnUser(QStringLiteral("Cannot open %1").arg(QString::fromStdString(path.string())));
            throw std::runtime_error("InternalDepsStream: cannot open " + path.string());
        }
        char hdr[wire::kFileHeaderBytes] = {};
        in_.read(hdr, wire::kFileHeaderBytes);
        if (!in_ || std::memcmp(hdr, wire::kMagic, sizeof(wire::kMagic)) != 0) {
            warnUser(QStringLiteral("Not an EVTQ recording: %1").arg(QString::fromStdString(path.string())));
            throw std::runtime_error("InternalDepsStream: not an EVTQ file");
        }
        std::memcpy(&ver_, hdr + 4, 4);
        std::memcpy(&count_, hdr + 8, 8);
        std::memcpy(&t0_, hdr + 16, 8);
        std::memcpy(&t1_, hdr + 24, 8);
        (void)ver_;
    }

    bool pull(long& tick_ms)
    {
        if (!in_) return false;
        unsigned char head[wire::kRecordHeadBytes];
        in_.read(reinterpret_cast<char*>(head), wire::kRecordHeadBytes);
        if (!in_ || in_.gcount() != wire::kRecordHeadBytes) return false;

        wire::RecordHead rh;
        if (!wire::decode_head(head, rh)) return false;
        last_type_ = rh.raw_kind;

        std::vector<char> payload(static_cast<size_t>(rh.nfields) * 4u);
        if (!payload.empty()) {
            in_.read(payload.data(), static_cast<std::streamsize>(payload.size()));
            if (!in_ || static_cast<size_t>(in_.gcount()) != payload.size()) {
                warnUser(QStringLiteral("Recording truncated at event %1").arg(index_));
                return false;
            }
            crc_ = chks::add(crc_, payload.data(), payload.size());
        }

        tick_ms = static_cast<long>(rh.t * 1000.0);
        index_++;
        return true;
    }

    long last_type() const { return last_type_; }
    uint64_t event_count() const { return count_; }
    double t_begin() const { return t0_; }
    double t_end() const { return t1_; }
    uint32_t payload_checksum() const { return crc_; }

private:
    void warnUser(const QString& message)
    {
        QMessageBox::warning(nullptr, QStringLiteral("EVTQ Import"), message);
    }

    std::filesystem::path path_;
    std::ifstream in_;
    uint32_t ver_ = 0;
    uint64_t count_ = 0;
    double t0_ = 0;
    double t1_ = 0;
    uint64_t index_ = 0;
    long last_type_ = 0;
    uint32_t crc_ = 0;
};
