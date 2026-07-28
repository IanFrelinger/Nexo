#include "adapted_reader.hpp"
#include <cstdio>
#include <cmath>

static bool approx(double a, double b) { return std::fabs(a - b) < 1e-6; }

int main() {
    using namespace fileproc;
    int failures = 0;

    // FileSession is synthetic: 16 records, t=index*0.5, typeId=index%4, payload={index}.
    CustomEventReader reader(std::filesystem::path("/work/sample.session"));
    size_t i = 0;
    Event e;
    while (reader.next(e, 4096)) {
        double expectedT = (double)i * 0.5;
        uint8_t expectedType = (uint8_t)(i % 4);
        if (!approx(e.t, expectedT) || e.type != expectedType || e.data.size() != 1 || e.data[0] != (uint8_t)i) {
            std::printf("FAIL: record %zu: t=%f type=%d dataLen=%zu (expected t=%f type=%d dataLen=1 byte=%zu)\n",
                i, e.t, (int)e.type, e.data.size(), expectedT, (int)expectedType, i);
            ++failures;
        } else {
            std::printf("record %zu: t=%f type=%d -- OK\n", i, e.t, (int)e.type);
        }
        ++i;
        if (i > 32) { std::printf("FAIL: runaway loop, did not stop at 16\n"); ++failures; break; }
    }
    if (i != 16) {
        std::printf("FAIL: got %zu records, expected 16 (FileSession::total_ is hardcoded to 16)\n", i);
        ++failures;
    }

    FileInfo fi = inspect_custom(std::filesystem::path("/work/sample.session"));
    std::printf("inspect_custom: event_count=%llu t_begin=%f t_end=%f\n",
        (unsigned long long)fi.event_count, fi.t_begin, fi.t_end);
    if (fi.event_count != 16) {
        std::printf("FAIL: inspect_custom event_count=%llu expected=16 (only read 1 record instead of counting all)\n", (unsigned long long)fi.event_count);
        ++failures;
    }
    if (!approx(fi.t_end, 7.5)) {
        std::printf("FAIL: inspect_custom t_end=%f expected=7.5 (last record's t)\n", fi.t_end);
        ++failures;
    }

    // can_resume()/resume_at()/position() contract check: does resume_at genuinely honor its offset argument?
    CustomEventReader reader2(std::filesystem::path("/work/sample.session"));
    Event tmp;
    for (int k = 0; k < 5; ++k) reader2.next(tmp, 4096); // consume records 0..4, tmp now holds record 4 (t=2.0)
    uint64_t posAfter5 = reader2.position();
    bool resumable = reader2.can_resume();
    reader2.resume_at(posAfter5);
    Event afterResume;
    bool got = reader2.next(afterResume, 4096);
    std::printf("can_resume()=%d position()after5=%llu next-after-resume t=%f (want continuation from record 5, t=2.5)\n",
        (int)resumable, (unsigned long long)posAfter5, got ? afterResume.t : -1.0);
    if (resumable && got && !approx(afterResume.t, 2.5)) {
        std::printf("FAIL: can_resume()=true but resume_at(%llu) did not continue from record 5 (t=2.5) -- got t=%f. "
            "This means resume_at silently restarted from the beginning instead of honoring the offset -- "
            "a contract violation (can_resume claims seek support the format doesn't have).\n",
            (unsigned long long)posAfter5, afterResume.t);
        ++failures;
    }

    if (failures == 0) {
        std::printf("ALL FUNCTIONAL CHECKS PASSED\n");
        return 0;
    }
    std::printf("%d FUNCTIONAL CHECK(S) FAILED\n", failures);
    return 1;
}
