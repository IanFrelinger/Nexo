#pragma once
// Hostile mock: configuration holder with no pull/advance/next API (not scaffoldable).
#include <cstdint>
#include <string>
#include <vector>

class ConfigBlob {
public:
    explicit ConfigBlob(std::string name) : name_(std::move(name)) {}

    void set_flag(std::string key, bool value)
    {
        keys_.push_back(std::move(key));
        flags_.push_back(value);
    }

    bool get_flag(const std::string& key) const
    {
        for (size_t i = 0; i < keys_.size(); ++i)
            if (keys_[i] == key) return flags_[i];
        return false;
    }

    std::uint64_t version() const { return version_; }
    void bump() { version_++; }

private:
    std::string name_;
    std::vector<std::string> keys_;
    std::vector<bool> flags_;
    std::uint64_t version_ = 1;
};
