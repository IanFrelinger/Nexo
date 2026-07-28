#include "GraphReader.hpp"

GraphReader::GraphReader(const std::string& path)
    : path_(path), bridge_("graph-io") {}

bool GraphReader::open() {
    std::ifstream in(path_, std::ios::binary);
    if (!in) return false;
    buf_.bytes.assign(std::istreambuf_iterator<char>(in), {});
    delete cursor_;
    cursor_ = new ByteCursor(buf_);
    return true;
}

bool GraphReader::next(GraphRecord& out) {
    if (!cursor_ || cursor_->eof()) return false;
    out.id = cursor_->nextU32();
    if (cursor_->eof()) return false;
    out.payload = cursor_->nextU32();
    return true;
}

void GraphReader::seekRecord(size_t index) {
    if (cursor_) cursor_->seek(index * 8);
}
