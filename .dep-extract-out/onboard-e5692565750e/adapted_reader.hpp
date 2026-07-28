#pragma once
// Deterministic scaffold from DepExtract (review before production use).
#include "processing.hpp"
#include "core/NestedStream.hpp"
#include <memory>

namespace fileproc {

class CustomEventReader : public IEventReader {
public:
    explicit CustomEventReader(const std::filesystem::path& path) {
        parser_ = std::make_unique<NestedStream>(path);
    }
    void set_requested_fields(const std::vector<std::string>& names) override { (void)names; }
    bool decodes_named_fields() const override { return true; }
    bool next(Event& out, size_t max_depth) override {
        (void)max_depth;
        long v = 0;
        if (!parser_ || !parser_->pull(v)) return false;
        out.t = static_cast<double>(v) / 1000.0;
        out.type = static_cast<int>(parser_->last_type());
        out.fields["value"] = std::to_string(v);
        pos_++;
        return true;
    }
    bool can_resume() const override { return true; }
    uint64_t position() const override { return pos_; }
    void resume_at(uint64_t off) override { pos_ = off; /* UNCERTAIN: seek proprietary stream */ }
private:
    std::unique_ptr<NestedStream> parser_;
    uint64_t pos_ = 0;
};

inline FileInfo inspect_custom(const std::filesystem::path& path) {
    FileInfo fi;
    fi.bytes = std::filesystem::file_size(path);
    fi.resumable = true;
    for (auto* n : kTypeNames) fi.types.push_back(n);
    try {
        NestedStream probe(path);
        fi.event_count = probe.event_count();
        fi.t_begin = probe.t_begin();
        fi.t_end = probe.t_end();
    } catch (...) { /* leave defaults */ }
    if (fi.t_end <= fi.t_begin) fi.t_end = 1e300;
    return fi;
}

inline void use_custom_reader(){
    reader_factory() = [](const std::filesystem::path& p){
        return std::unique_ptr<IEventReader>(new CustomEventReader(p));
    };
    inspect_factory() = inspect_custom;
}

} // namespace fileproc

