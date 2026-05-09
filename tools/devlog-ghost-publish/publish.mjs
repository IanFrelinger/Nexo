#!/usr/bin/env node
/**
 * Creates a draft post on a Ghost blog via the Admin API.
 * Env: GHOST_URL, GHOST_ADMIN_API_KEY (format id:secret from Ghost Admin → Integrations).
 */
import crypto from "node:crypto";
import process from "node:process";

function usage() {
  console.error(`Usage:
  publish.mjs --title "..." [--html-file path] [--html-string "..."] [--markdown-file path] [--status draft|published]

Or pipe HTML on stdin (title still required):
  cat body.html | publish.mjs --title "..."

Environment:
  GHOST_URL              e.g. https://blog.example.com (no trailing slash)
  GHOST_ADMIN_API_KEY    Admin API key from Ghost (id:secret)`);
}

function parseArgs(argv) {
  const out = {
    title: null,
    htmlFile: null,
    htmlString: null,
    markdownFile: null,
    status: "draft",
  };
  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === "--title" && argv[i + 1]) {
      out.title = argv[++i];
    } else if (a === "--html-file" && argv[i + 1]) {
      out.htmlFile = argv[++i];
    } else if (a === "--html-string" && argv[i + 1]) {
      out.htmlString = argv[++i];
    } else if (a === "--markdown-file" && argv[i + 1]) {
      out.markdownFile = argv[++i];
    } else if (a === "--status" && argv[i + 1]) {
      out.status = argv[++i];
    } else if (a === "--help" || a === "-h") {
      usage();
      process.exit(0);
    }
  }
  return out;
}

function base64UrlEncode(data) {
  return Buffer.from(data)
    .toString("base64")
    .replace(/=/g, "")
    .replace(/\+/g, "-")
    .replace(/\//g, "_");
}

function ghostAdminToken(adminApiKey) {
  const parts = adminApiKey.split(":");
  if (parts.length !== 2 || !parts[0] || !parts[1]) {
    throw new Error("GHOST_ADMIN_API_KEY must be id:secret from Ghost Admin → Integrations");
  }
  const [keyId, secretHex] = parts;
  const header = { alg: "HS256", typ: "JWT", kid: keyId };
  const iat = Math.floor(Date.now() / 1000);
  const payload = { iat, exp: iat + 300, aud: "/admin/" };
  const headerSeg = base64UrlEncode(JSON.stringify(header));
  const payloadSeg = base64UrlEncode(JSON.stringify(payload));
  const signingInput = `${headerSeg}.${payloadSeg}`;
  const secret = Buffer.from(secretHex, "hex");
  const sig = crypto.createHmac("sha256", secret).update(signingInput).digest("base64");
  const sigSeg = sig.replace(/=/g, "").replace(/\+/g, "-").replace(/\//g, "_");
  return `${signingInput}.${sigSeg}`;
}

function escapeHtml(s) {
  return s
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

/** Turn plain GitHub release notes into simple HTML (paragraphs + line breaks). Escapes HTML. */
function releaseMarkdownToHtml(markdown) {
  if (!markdown || !markdown.trim()) {
    return "<p><em>(No release notes provided.)</em></p>";
  }
  return markdown
    .split(/\n\n+/)
    .map((block) => `<p>${escapeHtml(block).replace(/\n/g, "<br/>")}</p>`)
    .join("\n");
}

function normalizeGhostUrl(url) {
  const u = (url || "").trim().replace(/\/+$/, "");
  if (!u.startsWith("http://") && !u.startsWith("https://")) {
    throw new Error("GHOST_URL must start with https:// or http://");
  }
  return u;
}

async function readHtml(args, stdinHtml) {
  if (stdinHtml) {
    return stdinHtml;
  }
  if (args.htmlString) {
    return args.htmlString;
  }
  if (args.htmlFile) {
    const fs = await import("node:fs/promises");
    return fs.readFile(args.htmlFile, "utf8");
  }
  if (args.markdownFile) {
    const fs = await import("node:fs/promises");
    const md = await fs.readFile(args.markdownFile, "utf8");
    return releaseMarkdownToHtml(md);
  }
  throw new Error("Provide --html-file, --html-string, --markdown-file, or pipe HTML on stdin");
}

async function main() {
  const args = parseArgs(process.argv);
  if (!args.title) {
    usage();
    process.exit(1);
  }
  if (args.status !== "draft" && args.status !== "published") {
    console.error('status must be "draft" or "published"');
    process.exit(1);
  }

  const ghostUrl = normalizeGhostUrl(process.env.GHOST_URL || "");
  const adminKey = process.env.GHOST_ADMIN_API_KEY || "";
  if (!ghostUrl || !adminKey) {
    console.error("Missing GHOST_URL or GHOST_ADMIN_API_KEY");
    process.exit(1);
  }

  let stdinHtml = "";
  if (!args.htmlFile && !args.htmlString && !args.markdownFile) {
    stdinHtml = await new Promise((resolve, reject) => {
      const chunks = [];
      process.stdin.on("data", (c) => chunks.push(c));
      process.stdin.on("end", () => resolve(Buffer.concat(chunks).toString("utf8")));
      process.stdin.on("error", reject);
    });
  }

  const html = await readHtml(args, stdinHtml.trim() || null);
  if (!html || !html.trim()) {
    console.error("HTML body is empty");
    process.exit(1);
  }

  const token = ghostAdminToken(adminKey);
  const endpoint = `${ghostUrl}/ghost/api/admin/posts/?source=html`;

  const res = await fetch(endpoint, {
    method: "POST",
    headers: {
      Authorization: `Ghost ${token}`,
      "Content-Type": "application/json",
      Accept: "application/json",
    },
    body: JSON.stringify({
      posts: [
        {
          title: args.title,
          html,
          status: args.status,
        },
      ],
    }),
  });

  const text = await res.text();
  let json;
  try {
    json = JSON.parse(text);
  } catch {
    json = null;
  }

  if (!res.ok) {
    console.error("Ghost API error:", res.status, text);
    process.exit(1);
  }

  const post = json?.posts?.[0];
  if (!post) {
    console.error("Unexpected response:", text);
    process.exit(1);
  }

  console.log(JSON.stringify({ id: post.id, url: post.url, uuid: post.uuid, status: post.status }, null, 2));
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
