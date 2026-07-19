#pragma once
#include <fstream>
#include <string>

// Complexity tier 2: fixed-size binary records, sequential-only — no seek
// method exists at all. Tests whether the adapter honestly reports
// can_resume() == false rather than inventing a seek capability.
struct FixedRec { double t; float value; };

class FixedRecordLog {
public:
    explicit FixedRecordLog(const std::string& path);
    bool readRecord(FixedRec& out);

private:
    std::ifstream in_;
};
