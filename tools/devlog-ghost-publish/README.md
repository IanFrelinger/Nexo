# Devlog → Ghost (draft publisher)

Creates a **draft** (or published) post on your Ghost site using the [Admin API](https://ghost.org/docs/admin-api/).

## One-time: Ghost

1. Create a site on [Ghost(Pro)](https://ghost.org/pricing/) or self-host Ghost.
2. Admin → **Settings → Integrations → Add custom integration** → copy the **Admin API Key** (`id:secret`).

## GitHub Actions

Add repository **secrets**:

| Secret | Value |
|--------|--------|
| `GHOST_URL` | `https://your-ghost-host.com` (no trailing slash) |
| `GHOST_ADMIN_API_KEY` | Admin API key from Ghost |

Releases: publishing a [GitHub Release](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository) runs `.github/workflows/devlog-ghost-release.yml` and opens a Ghost **draft** titled `Release: <tag>`.

Manual run: **Actions → Devlog Ghost publish → Run workflow** (optional title/body; body defaults to a placeholder).

## Local test

```bash
export GHOST_URL="https://your-ghost-host.com"
export GHOST_ADMIN_API_KEY="paste:id:here"
echo "<p>Hello from Ashlar tooling.</p>" | node publish.mjs --title "Test draft"
```

## Video / interactive posts

Edit the draft in Ghost: add video blocks (YouTube/Vimeo/upload) or embed HTML (CodePen, demos). This tool only seeds the draft from release notes.
