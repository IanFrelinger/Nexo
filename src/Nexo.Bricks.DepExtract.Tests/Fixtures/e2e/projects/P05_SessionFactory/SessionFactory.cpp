#include "SessionFactory.hpp"

FileSession::FileSession(const std::string& path) : path_(path), total_(16) {}
bool FileSession::readRecord(SessionRecord& out)
{
    if (index_ >= total_) return false;
    out.t = static_cast<double>(index_) * 0.5;
    out.typeId = static_cast<int>(index_ % 4);
    out.payload = {static_cast<uint8_t>(index_)};
    index_++;
    return true;
}
void FileSession::rewind() { index_ = 0; }
uint64_t FileSession::sizeHint() const { return total_; }

MemorySession::MemorySession(std::vector<SessionRecord> records) : records_(std::move(records)) {}
bool MemorySession::readRecord(SessionRecord& out)
{
    if (index_ >= records_.size()) return false;
    out = records_[index_++];
    return true;
}
void MemorySession::rewind() { index_ = 0; }
uint64_t MemorySession::sizeHint() const { return records_.size(); }

std::unique_ptr<ISession> SessionFactory::openFile(const std::string& path)
{
    return std::make_unique<FileSession>(path);
}
std::unique_ptr<ISession> SessionFactory::openMemory(std::vector<SessionRecord> records)
{
    return std::make_unique<MemorySession>(std::move(records));
}
std::string SessionFactory::backendName() { return "mock-session-factory"; }
