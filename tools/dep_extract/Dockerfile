# syntax=docker/dockerfile:1
# dep-extract — standalone containerized #include dependency-closure tool.
# Not evtx-specific: run it against ANY C/C++ source tree. Nothing you mount
# is copied anywhere outside the container's own bind-mounted volumes.
FROM ubuntu:26.04
RUN apt-get update && apt-get install -y --no-install-recommends \
        g++ clang bash findutils coreutils \
    && rm -rf /var/lib/apt/lists/*
COPY extract_deps.sh /usr/local/bin/extract_deps.sh
RUN chmod +x /usr/local/bin/extract_deps.sh
WORKDIR /src
ENTRYPOINT ["/usr/local/bin/extract_deps.sh"]
