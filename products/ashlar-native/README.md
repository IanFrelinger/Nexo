# ashlar-native

Optional WASM / out-of-process native host. Implements `INativeExecutionHost`.

Hard rule: **never** `dlopen` AI-generated native code into the IDE or API
process. Supported formats are `WebAssembly` and `OutOfProcessWorker`.
Managed assemblies stay in the kernel `AssemblyLoadContext` path.

This tree is extractable to `github.com/IanFrelinger/ashlar-native`.
