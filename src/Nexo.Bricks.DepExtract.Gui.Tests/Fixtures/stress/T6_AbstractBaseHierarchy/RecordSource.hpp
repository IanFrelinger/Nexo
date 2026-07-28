#pragma once
#include <cstdint>
#include <fstream>
#include <string>
#include <vector>

// Complexity tier 6: virtual dispatch through an abstract base. Tests
// whether the adapter correctly instantiates the CONCRETE class
// (BinaryRecordSource), not the abstract IRecordSource, while still calling
// through the base's method names.
struct GenericRecord { double t; std::vector<uint8_t> bytes; };

class IRecordSource {
public:
    virtual ~IRecordSource() = default;
    virtual bool pull(GenericRecord& out) = 0;
    virtual void seekTo(uint64_t off) = 0;
    virtual uint64_t currentOffset() const = 0;
};

class BinaryRecordSource : public IRecordSource {
public:
    explicit BinaryRecordSource(const std::string& path);
    bool pull(GenericRecord& out) override;
    void seekTo(uint64_t off) override;
    uint64_t currentOffset() const override;

private:
    std::ifstream in_;
    uint64_t pos_ = 0;
};
