#include "WarningFacade.hpp"
#include <algorithm>
#include <fstream>

// Intentionally shaped like Qt Widgets dialogs — headless builds swap this.
#ifndef QMessageBox
struct QMessageBox {
    static void warning(void*, const char*, const std::string& m) { (void)m; }
    static void critical(void*, const char*, const std::string& m) { (void)m; }
};
#endif

WarningFacade::WarningFacade(const std::string& path) : path_(path) {}

void WarningFacade::setFields(const std::vector<std::string>& names) { wanted_ = names; }

bool WarningFacade::readNamed(std::map<std::string, std::string>& fields, double& t, uint8_t& type) {
    std::ifstream in(path_, std::ios::binary);
    if (!in) { critical("cannot open " + path_); return false; }
    in.seekg(static_cast<std::streamoff>(cursor_));
    if (!in.read(reinterpret_cast<char*>(&t), sizeof(t))) return false;
    if (!in.read(reinterpret_cast<char*>(&type), sizeof(type))) return false;
    uint16_t n = 0;
    if (!in.read(reinterpret_cast<char*>(&n), sizeof(n))) return false;
    fields.clear();
    for (uint16_t i = 0; i < n; ++i) {
        uint8_t klen = 0, vlen = 0;
        if (!in.read(reinterpret_cast<char*>(&klen), 1)) return false;
        std::string k(klen, '\0');
        if (!in.read(k.data(), klen)) return false;
        if (!in.read(reinterpret_cast<char*>(&vlen), 1)) return false;
        std::string v(vlen, '\0');
        if (!in.read(v.data(), vlen)) return false;
        if (wanted_.empty() || std::find(wanted_.begin(), wanted_.end(), k) != wanted_.end())
            fields[k] = v;
    }
    cursor_ = static_cast<uint64_t>(in.tellg());
    if (fields.empty()) warn("no requested fields present in record");
    return true;
}

void WarningFacade::seekSection(uint32_t sectionIndex) {
    section_ = sectionIndex;
    cursor_ = static_cast<uint64_t>(sectionIndex) * 4096ull;
}

void WarningFacade::warn(const std::string& msg) { QMessageBox::warning(nullptr, "warn", msg); }
void WarningFacade::critical(const std::string& msg) { QMessageBox::critical(nullptr, "crit", msg); }
