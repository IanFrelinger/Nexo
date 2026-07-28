#pragma once
// EVTQ-format telemetry reader as found in a Qt desktop application. The
// decode logic is plain C++ (ifstream + memcpy); Qt is used ONLY to float
// non-fatal warnings to the user, the way desktop apps surface parse problems
// in a message box. Fixture for verifying the extract/adapt pipeline handles
// non-load-bearing Qt dependencies.
#include <cstdint>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <stdexcept>
#include <string>
#include <vector>

#include <QMessageBox>
#include <QString>

class QtWarnEvtqStream {
public:
    explicit QtWarnEvtqStream(const std::filesystem::path& path)
        : path_(path), in_(path, std::ios::binary)
    {
        if (!in_) {
            warnUser(QStringLiteral("Cannot open %1").arg(QString::fromStdString(path.string())));
            throw std::runtime_error("QtWarnEvtqStream: cannot open " + path.string());
        }
        char hdr[64] = {};
        in_.read(hdr, 64);
        if (!in_ || std::memcmp(hdr, "EVTQ", 4) != 0) {
            warnUser(QStringLiteral("Not an EVTQ recording: %1").arg(QString::fromStdString(path.string())));
            throw std::runtime_error("QtWarnEvtqStream: not an EVTQ file");
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
        unsigned char head[12];
        in_.read(reinterpret_cast<char*>(head), 12);
        if (!in_ || in_.gcount() != 12) return false;
        last_type_ = head[0];
        const unsigned nf = head[1];
        double t = 0;
        std::memcpy(&t, head + 4, 8);
        std::vector<char> payload(static_cast<size_t>(nf) * 4u);
        if (nf) {
            in_.read(payload.data(), static_cast<std::streamsize>(payload.size()));
            if (!in_ || static_cast<size_t>(in_.gcount()) != payload.size()) {
                // Desktop build floats a truncation warning; decode just stops.
                warnUser(QStringLiteral("Recording truncated at event %1").arg(index_));
                return false;
            }
        }
        tick_ms = static_cast<long>(t * 1000.0);
        index_++;
        return true;
    }

    long last_type() const { return last_type_; }
    uint64_t event_count() const { return count_; }
    double t_begin() const { return t0_; }
    double t_end() const { return t1_; }

private:
    void warnUser(const QString& message)
    {
        // GUI-only nicety — never load-bearing for the decode path.
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
};
