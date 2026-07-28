#include "adapted_reader.hpp"
#include <cstdio>
#include <cmath>

struct Expected { double t; uint8_t type; size_t payloadLen; };

static bool approx(double a, double b) { return std::fabs(a - b) < 1e-6; }

int main() {
    using namespace fileproc;
    int failures = 0;

    // Must match the records baked into sample.radar exactly.
    Expected expected[] = {
        {100.0, 1, 3},
        {100.5, 2, 2},
        {101.25, 1, 0},
        {150.0, 3, 5},
    };
    const size_t expectedCount = sizeof(expected) / sizeof(expected[0]);

    CustomEventReader reader(std::filesystem::path("/work/sample.radar"));
    size_t i = 0;
    Event e;
    while (reader.next(e, 4096)) {
        if (i >= expectedCount) {
            std::printf("FAIL: more records than expected (got record #%zu)\n", i);
            ++failures;
            break;
        }
        if (!approx(e.t, expected[i].t)) {
            std::printf("FAIL: record %zu: t=%f expected=%f\n", i, e.t, expected[i].t);
            ++failures;
        }
        if (e.type != expected[i].type) {
            std::printf("FAIL: record %zu: type=%d expected=%d\n", i, (int)e.type, (int)expected[i].type);
            ++failures;
        }
        if (e.data.size() != expected[i].payloadLen) {
            std::printf("FAIL: record %zu: payloadLen=%zu expected=%zu\n", i, e.data.size(), expected[i].payloadLen);
            ++failures;
        }
        std::printf("record %zu: t=%f type=%d payloadLen=%zu -- OK\n", i, e.t, (int)e.type, e.data.size());
        ++i;
    }
    if (i != expectedCount) {
        std::printf("FAIL: got %zu records, expected %zu\n", i, expectedCount);
        ++failures;
    }

    // inspect_custom() functional check
    FileInfo fi = inspect_custom(std::filesystem::path("/work/sample.radar"));
    std::printf("inspect_custom: event_count=%llu t_begin=%f t_end=%f bytes=%llu resumable=%d\n",
        (unsigned long long)fi.event_count, fi.t_begin, fi.t_end, (unsigned long long)fi.bytes, (int)fi.resumable);
    if (fi.event_count != expectedCount) {
        std::printf("FAIL: inspect_custom event_count=%llu expected=%zu\n", (unsigned long long)fi.event_count, expectedCount);
        ++failures;
    }
    if (!approx(fi.t_begin, expected[0].t)) {
        std::printf("FAIL: inspect_custom t_begin=%f expected=%f\n", fi.t_begin, expected[0].t);
        ++failures;
    }
    if (!approx(fi.t_end, expected[expectedCount - 1].t)) {
        std::printf("FAIL: inspect_custom t_end=%f expected=%f\n", fi.t_end, expected[expectedCount - 1].t);
        ++failures;
    }

    // resume_at()/position() functional check: resume after record 1, expect to replay from record 2 onward.
    CustomEventReader reader2(std::filesystem::path("/work/sample.radar"));
    Event tmp;
    reader2.next(tmp, 4096); // consume record 0
    uint64_t posAfterFirst = reader2.position();
    reader2.next(tmp, 4096); // consume record 1
    reader2.resume_at(posAfterFirst);
    Event resumed;
    if (!reader2.next(resumed, 4096)) {
        std::printf("FAIL: resume_at didn't yield a record\n");
        ++failures;
    } else if (!approx(resumed.t, expected[1].t)) {
        std::printf("FAIL: resume_at landed on t=%f, expected record 1's t=%f\n", resumed.t, expected[1].t);
        ++failures;
    } else {
        std::printf("resume_at: correctly replayed record 1 (t=%f) -- OK\n", resumed.t);
    }

    if (failures == 0) {
        std::printf("ALL FUNCTIONAL CHECKS PASSED\n");
        return 0;
    }
    std::printf("%d FUNCTIONAL CHECK(S) FAILED\n", failures);
    return 1;
}
