#include "adapted_reader.hpp"
#include <cstdio>
#include <cmath>

static bool approx(double a, double b) { return std::fabs(a - b) < 1e-6; }

int main() {
    using namespace fileproc;
    int failures = 0;

    // sample.chunks: header uint32 sections_=1, then 4 chunks with payloads "AAA","BB","","CCCCC".
    // byte layout: [4B sections_][4B n=3]"AAA"[4B n=2]"BB"[4B n=0][4B n=5]"CCCCC"
    // ChunkCursor.cpp sets out.t0 = pos_ BEFORE reading this chunk's length+payload.
    double expectedT0[] = {4.0, 11.0, 17.0, 21.0};
    size_t expectedLen[] = {3, 2, 0, 5};
    const size_t expectedCount = 4;

    CustomEventReader reader(std::filesystem::path("/work/sample.chunks"));
    size_t i = 0;
    Event e;
    while (reader.next(e, 4096)) {
        if (i >= expectedCount) { std::printf("FAIL: extra record %zu\n", i); ++failures; break; }
        bool tOk = approx(e.t, expectedT0[i]);
        bool lenOk = e.data.size() == expectedLen[i];
        if (!tOk || !lenOk) {
            std::printf("FAIL: record %zu: t=%f (want %f) dataLen=%zu (want %zu)\n",
                i, e.t, expectedT0[i], e.data.size(), expectedLen[i]);
            ++failures;
        } else {
            std::printf("record %zu: t=%f dataLen=%zu -- OK\n", i, e.t, e.data.size());
        }
        ++i;
    }
    if (i != expectedCount) {
        std::printf("FAIL: got %zu records, expected %zu\n", i, expectedCount);
        ++failures;
    }

    // Named-field decode check: request two DIFFERENT field names and see if they get
    // distinct values or (per the code-review finding) identical raw-byte dumps.
    CustomEventReader reader2(std::filesystem::path("/work/sample.chunks"));
    reader2.set_requested_fields({"lat", "lon"});
    Event fe;
    if (reader2.next(fe, 4096)) {
        auto itLat = fe.fields.find("lat");
        auto itLon = fe.fields.find("lon");
        if (itLat != fe.fields.end() && itLon != fe.fields.end()) {
            std::printf("named-field decode: lat=%s lon=%s\n", itLat->second.c_str(), itLon->second.c_str());
            if (itLat->second == itLon->second) {
                std::printf("FAIL: 'lat' and 'lon' got IDENTICAL values -- the adapter dumps the whole raw "
                    "chunk into every requested field name instead of decoding each field's own sub-value. "
                    "Any caller requesting 2+ named fields gets the same garbage for all of them.\n");
                ++failures;
            }
        } else {
            std::printf("FAIL: decodes_named_fields()=true but requested fields were not populated\n");
            ++failures;
        }
    }

    FileInfo fi = inspect_custom(std::filesystem::path("/work/sample.chunks"));
    std::printf("inspect_custom: event_count=%llu t_begin=%f t_end=%f\n",
        (unsigned long long)fi.event_count, fi.t_begin, fi.t_end);
    if (fi.event_count != expectedCount) {
        std::printf("FAIL: inspect_custom event_count=%llu expected=%zu (only read first chunk instead of counting all)\n",
            (unsigned long long)fi.event_count, expectedCount);
        ++failures;
    }
    double expectedTEnd = expectedT0[expectedCount - 1] + 1.0;
    if (!approx(fi.t_end, expectedTEnd)) {
        std::printf("FAIL: inspect_custom t_end=%f expected=%f (last chunk's t1, not the first chunk's)\n", fi.t_end, expectedTEnd);
        ++failures;
    }

    // position()/resume_at() contract check -- ChunkCursor DOES support real seeking, so this should work.
    CustomEventReader reader3(std::filesystem::path("/work/sample.chunks"));
    Event tmp;
    reader3.next(tmp, 4096); // record 0
    uint64_t posAfter0 = reader3.position();
    reader3.next(tmp, 4096); // record 1
    reader3.resume_at(posAfter0);
    Event resumed;
    bool got = reader3.next(resumed, 4096);
    if (!got || !approx(resumed.t, expectedT0[1])) {
        std::printf("FAIL: resume_at(%llu) did not replay record 1 (t=%f) -- got t=%f\n",
            (unsigned long long)posAfter0, expectedT0[1], got ? resumed.t : -1.0);
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
