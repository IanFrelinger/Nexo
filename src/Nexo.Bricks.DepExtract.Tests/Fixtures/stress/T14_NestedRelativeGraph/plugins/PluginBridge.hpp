#pragma once
#include <string>

// Cross-tree include target (engine/core -> ../../plugins).
struct PluginBridge {
    explicit PluginBridge(std::string name) : name_(std::move(name)) {}
    const std::string& name() const { return name_; }
    int capabilityMask() const { return 0x3; }

private:
    std::string name_;
};
