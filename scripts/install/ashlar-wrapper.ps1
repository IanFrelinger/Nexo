# `ashlar` on the host (Windows). One box, ONE operator identity: every invocation runs
# against the node's state volume, never against a host build's ~/.ashlar. See
# ashlar-wrapper.sh for the full rationale; the resolution order is identical:
# running node -> stopped node's own image -> refuse with the command to run.
$ErrorActionPreference = "Stop"

$cid = (docker ps -q --filter "label=com.docker.compose.service=node" | Select-Object -First 1)
if ($cid) {
    docker exec -i $cid dotnet /app/Ashlar.CLI.dll @args
    exit $LASTEXITCODE
}

$cid = (docker ps -aq --filter "label=com.docker.compose.service=node" | Select-Object -First 1)
if ($cid) {
    $img = docker inspect --format '{{.Config.Image}}' $cid
    docker run --rm -i -v ashlar-state:/data/state -w /data/state/project $img @args
    exit $LASTEXITCODE
}

[Console]::Error.WriteLine("ashlar: no node on this machine. Start one first, from your Ashlar checkout:")
[Console]::Error.WriteLine("  docker compose -f deploy/node.yml up -d")
exit 1
