#pragma once
#include <cstdint>
#include <map>
#include <string>
#include <vector>

// Complexity tier 13: desktop-style warning API (QMessageBox-shaped) plus
// named-field decoding and a sectioned seek. Adapter must keep warnings
// headless-friendly and map named fields into out.fields.
class WarningFacade {
public:
    explicit WarningFacade(const std::string& path);
    void setFields(const std::vector<std::string>& names);
    bool readNamed(std::map<std::string, std::string>& fields, double& t, uint8_t& type);
    void seekSection(uint32_t sectionIndex);
    uint64_t cursor() const { return cursor_; }
    void warn(const std::string& msg);
    void critical(const std::string& msg);

private:
    std::string path_;
    std::vector<std::string> wanted_;
    uint64_t cursor_ = 0;
    uint32_t section_ = 0;
};
