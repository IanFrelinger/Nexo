#pragma once
// Mock proprietary: factory + abstract session (>2 classes / irregular → model).
#include <cstdint>
#include <memory>
#include <string>
#include <vector>

struct SessionRecord {
    double t = 0;
    int typeId = 0;
    std::vector<uint8_t> payload;
};

class ISession {
public:
    virtual ~ISession() = default;
    virtual bool readRecord(SessionRecord& out) = 0;
    virtual void rewind() = 0;
    virtual uint64_t sizeHint() const = 0;
};

class FileSession : public ISession {
public:
    explicit FileSession(const std::string& path);
    bool readRecord(SessionRecord& out) override;
    void rewind() override;
    uint64_t sizeHint() const override;

private:
    std::string path_;
    uint64_t index_ = 0;
    uint64_t total_ = 0;
};

class MemorySession : public ISession {
public:
    explicit MemorySession(std::vector<SessionRecord> records);
    bool readRecord(SessionRecord& out) override;
    void rewind() override;
    uint64_t sizeHint() const override;

private:
    std::vector<SessionRecord> records_;
    size_t index_ = 0;
};

class SessionFactory {
public:
    static std::unique_ptr<ISession> openFile(const std::string& path);
    static std::unique_ptr<ISession> openMemory(std::vector<SessionRecord> records);
    static std::string backendName();
};
