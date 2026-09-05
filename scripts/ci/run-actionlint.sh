#!/usr/bin/env bash
# Independent workflow syntax/semantic validation. Kept outside the workflow it validates.
set -euo pipefail

VERSION="1.7.7"
ARCHIVE="actionlint_${VERSION}_linux_amd64.tar.gz"
EXPECTED_SHA256="023070a287cd8cccd71515fedc843f1985bf96c436b7effaecce67290e7e0757"
URL="https://github.com/rhysd/actionlint/releases/download/v${VERSION}/${ARCHIVE}"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

curl --fail --silent --show-error --location "$URL" --output "${WORK}/${ARCHIVE}"
printf '%s  %s\n' "$EXPECTED_SHA256" "${WORK}/${ARCHIVE}" | sha256sum --check -
tar -xzf "${WORK}/${ARCHIVE}" -C "$WORK" actionlint
# Inline-shell style debt is tracked by the shell lanes. Keep this independent
# gate focused on workflow syntax, expressions, action inputs and untrusted
# interpolation; otherwise runner-installed shellcheck makes results vary by
# image and turns pre-existing style notices into unrelated workflow failures.
"${WORK}/actionlint" -shellcheck= -color
