#include "MultiRootLog.hpp"

MultiRootLog::MultiRootLog(const std::string& path) : cursor_(path), fields_{"label", "kind"} {}

bool MultiRootLog::pullContact(radar::Contact& out) {
    uint64_t tbits = 0;
    uint32_t kind = 0;
    uint32_t len = 0;
    if (!cursor_.readExact(&tbits, sizeof(tbits))) return false;
    if (!cursor_.readExact(&kind, sizeof(kind))) return false;
    if (!cursor_.readExact(&len, sizeof(len))) return false;
    std::string label(len, '\0');
    if (len && !cursor_.readExact(label.data(), len)) return false;
    out.t = *reinterpret_cast<double*>(&tbits);
    out.kind = kind;
    out.label = std::move(label);
    return true;
}

void MultiRootLog::jumpTo(uint64_t byteOffset) { cursor_.seek(byteOffset); }
uint64_t MultiRootLog::bytePos() const { return cursor_.tell(); }
std::vector<std::string> MultiRootLog::fieldNames() const { return fields_; }
