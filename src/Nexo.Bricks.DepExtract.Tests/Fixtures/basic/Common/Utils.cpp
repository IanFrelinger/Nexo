#include "Utils.hpp"
RecordId nextId() { static RecordId n = 0; return ++n; }
