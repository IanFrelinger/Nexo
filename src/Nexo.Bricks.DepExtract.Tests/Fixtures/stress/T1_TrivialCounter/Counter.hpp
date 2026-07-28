#pragma once

// Complexity tier 1: trivial. No file I/O, no seeking, no state beyond one
// integer. About as simple as a "record source" can be.
class SimpleCounter {
public:
    explicit SimpleCounter(long start) : value_(start) {}

    // Returns the next value; false once the counter reaches its ceiling.
    bool advance(long& out) {
        if (value_ >= 1000000) return false;
        out = ++value_;
        return true;
    }

private:
    long value_;
};
