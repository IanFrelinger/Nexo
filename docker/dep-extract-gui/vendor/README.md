# Vendored Docker Compose plugin

Run from the Nexo repo root (needs network once):

```bash
bash scripts/vendor-docker-compose.sh
```

This drops `docker-compose-linux-x86_64` here so `docker/dep-extract-gui/Dockerfile`
can `COPY` it without downloading from GitHub during `docker build`.
