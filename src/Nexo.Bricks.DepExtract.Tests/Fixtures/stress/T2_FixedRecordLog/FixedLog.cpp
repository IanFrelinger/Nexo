#include "FixedLog.hpp"

FixedRecordLog::FixedRecordLog(const std::string& path) : in_(path, std::ios::binary) {}

bool FixedRecordLog::readRecord(FixedRec& out) {
    return static_cast<bool>(in_.read(reinterpret_cast<char*>(&out), sizeof(out)));
}
