#pragma once
#include "../io/Cursor.hpp"
#include "../../plugins/PluginBridge.hpp"
#include <fstream>
#include <string>

struct GraphRecord { uint32_t id; uint32_t payload; };

class GraphReader {
public:
    explicit GraphReader(const std::string& path);
    bool open();
    bool next(GraphRecord& out);
    void seekRecord(size_t index);
    const PluginBridge& bridge() const { return bridge_; }

private:
    std::string path_;
    ByteBuffer buf_;
    ByteCursor* cursor_ = nullptr;
    PluginBridge bridge_;
};
