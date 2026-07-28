const vscode = require("vscode");
const http = require("http");
const https = require("https");
const { URL } = require("url");
const path = require("path");
const fs = require("fs");

const PATCH_SCHEME = "nexo-patch";

/** @type {vscode.StatusBarItem} */
let statusBar;
/** @type {ChatViewProvider | undefined} */
let chatProvider;
/** @type {PatchStore} */
let patchStore;
/** @type {PatchContentProvider} */
let patchContentProvider;

/** @type {{ clientId: string, runId: string|null, kind: string, req: import('http').ClientRequest } | null} */
let activeRun = null;
/** @type {Array<object>} */
let lastRuns = [];
/** @type {Array<object>} */
let lastAgents = [];
/** @type {{ provider: string, available: boolean, workloads: Array<object> } | null} */
let lastWorkloads = null;

/**
 * @param {vscode.ExtensionContext} context
 */
function activate(context) {
  patchStore = new PatchStore();
  patchContentProvider = new PatchContentProvider(patchStore);
  context.subscriptions.push(
    vscode.workspace.registerTextDocumentContentProvider(PATCH_SCHEME, patchContentProvider)
  );

  statusBar = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
  statusBar.command = "nexo.configureConnection";
  statusBar.text = "$(debug-disconnect) Nexo";
  statusBar.tooltip = "Nexo: Configure Connection";
  statusBar.show();
  context.subscriptions.push(statusBar);

  chatProvider = new ChatViewProvider(context.extensionUri);
  context.subscriptions.push(
    vscode.window.registerWebviewViewProvider("nexo.chatView", chatProvider)
  );

  context.subscriptions.push(
    vscode.commands.registerCommand("nexo.checkConnection", () => checkConnection(true)),
    vscode.commands.registerCommand("nexo.configureConnection", () => configureConnection()),
    vscode.commands.registerCommand("nexo.openChat", () =>
      vscode.commands.executeCommand("nexo.chatView.focus")
    ),
    vscode.commands.registerCommand("nexo.askSelection", () => askSelection()),
    vscode.commands.registerCommand("nexo.planWithPrompt", () => planWithPrompt()),
    vscode.commands.registerCommand("nexo.editWithPrompt", () => editWithPrompt()),
    vscode.commands.registerCommand("nexo.directorGoal", () => directorGoal()),
    vscode.commands.registerCommand("nexo.cancelActiveRun", () => cancelActiveRun()),
    vscode.commands.registerCommand("nexo.showRuns", () => showRuns()),
    vscode.commands.registerCommand("nexo.showWorkloads", () => showWorkloads()),
    vscode.commands.registerCommand("nexo.scaleWorkload", () => scaleWorkload()),
    vscode.commands.registerCommand("nexo.applyAllPatches", () => applyAllPatches()),
    vscode.commands.registerCommand("nexo.rejectAllPatches", () => rejectAllPatches()),
    vscode.commands.registerCommand("nexo.undoLastApply", () => undoLastApply()),
    vscode.commands.registerCommand("nexo.showPendingPatches", () => showPendingPatches()),
    vscode.commands.registerCommand("nexo.refreshModels", () => refreshModels(true)),
    vscode.commands.registerCommand("nexo.startLocalStack", () => startLocalStack()),
    vscode.commands.registerCommand("nexo.applyPatch", (filePath) => applyOnePatch(filePath)),
    vscode.commands.registerCommand("nexo.rejectPatch", (filePath) => rejectOnePatch(filePath)),
    vscode.commands.registerCommand("nexo.diffPatch", (filePath) => openPatchDiff(filePath))
  );

  context.subscriptions.push(
    vscode.workspace.onDidChangeConfiguration((e) => {
      if (
        e.affectsConfiguration("nexo.apiHost") ||
        e.affectsConfiguration("nexo.apiPort") ||
        e.affectsConfiguration("nexo.baseUrl") ||
        e.affectsConfiguration("nexo.ollamaPort")
      ) {
        chatProvider?.postConnectionSettings(getConnection());
        checkConnection(false);
      }
    })
  );

  checkConnection(false);
  refreshRuns().catch(() => {});
  refreshWorkloads().catch(() => {});
  const timer = setInterval(() => checkConnection(false), 30000);
  const pollMs = vscode.workspace.getConfiguration("nexo").get("pollRunsMs", 3000);
  const runTimer =
    pollMs > 0
      ? setInterval(() => {
          refreshRuns().catch(() => {});
        }, pollMs)
      : null;
  const workloadMs = vscode.workspace.getConfiguration("nexo").get("pollWorkloadsMs", 10000);
  const workloadTimer =
    workloadMs > 0
      ? setInterval(() => {
          refreshWorkloads().catch(() => {});
        }, workloadMs)
      : null;
  context.subscriptions.push({
    dispose: () => {
      clearInterval(timer);
      if (runTimer) clearInterval(runTimer);
      if (workloadTimer) clearInterval(workloadTimer);
    },
  });
}

function deactivate() {}

function getConnection() {
  const cfg = vscode.workspace.getConfiguration("nexo");
  const override = String(cfg.get("baseUrl") || "")
    .trim()
    .replace(/\/$/, "");
  if (override) {
    try {
      const u = new URL(override);
      const port = u.port
        ? Number(u.port)
        : u.protocol === "https:"
          ? 443
          : 80;
      return {
        baseUrl: override,
        host: u.hostname,
        port,
        scheme: u.protocol.replace(":", "") || "http",
      };
    } catch {
      return { baseUrl: override, host: "127.0.0.1", port: 8088, scheme: "http" };
    }
  }
  const host = String(cfg.get("apiHost") || "127.0.0.1").trim() || "127.0.0.1";
  const port = Number(cfg.get("apiPort") || 8088);
  const scheme = "http";
  return {
    baseUrl: `${scheme}://${host}:${port}`,
    host,
    port,
    scheme,
  };
}

function getBaseUrl() {
  return getConnection().baseUrl;
}

/**
 * Persist host/port (and optional ollama/model) and re-check.
 * Clears nexo.baseUrl so apiHost/apiPort take effect.
 * @param {{ host: string, port: number, ollamaPort?: number, model?: string|null, notify?: boolean }} opts
 */
async function applyConnectionSettings(opts) {
  const host = String(opts.host || "").trim() || "127.0.0.1";
  const port = Number(opts.port);
  if (!Number.isInteger(port) || port < 1 || port > 65535) {
    throw new Error("Port must be an integer 1–65535");
  }
  const cfg = vscode.workspace.getConfiguration("nexo");
  await cfg.update("apiHost", host, vscode.ConfigurationTarget.Workspace);
  await cfg.update("apiPort", port, vscode.ConfigurationTarget.Workspace);
  await cfg.update("baseUrl", "", vscode.ConfigurationTarget.Workspace);
  if (opts.ollamaPort != null) {
    const op = Number(opts.ollamaPort);
    if (!Number.isInteger(op) || op < 1 || op > 65535) {
      throw new Error("Ollama port must be an integer 1–65535");
    }
    await cfg.update("ollamaPort", op, vscode.ConfigurationTarget.Workspace);
  }
  if (opts.model != null && String(opts.model).trim()) {
    await cfg.update("defaultModel", String(opts.model).trim(), vscode.ConfigurationTarget.Workspace);
  }
  const base = getBaseUrl();
  if (opts.notify !== false) {
    vscode.window.showInformationMessage(`Nexo API set to ${base}`);
  }
  chatProvider?.postConnectionSettings(getConnection());
  await checkConnection(opts.notify !== false);
  return base;
}

/**
 * Interactive host/port (and optional model) configuration.
 */
async function configureConnection() {
  const cfg = vscode.workspace.getConfiguration("nexo");
  const current = getConnection();

  const host = await vscode.window.showInputBox({
    prompt: "Nexo agent-server host",
    value: current.host || "127.0.0.1",
    ignoreFocusOut: true,
  });
  if (host === undefined) return;

  const portStr = await vscode.window.showInputBox({
    prompt: "Nexo agent-server HTTP port (NEXO_AGENT_SERVER_HTTP_PORT)",
    value: String(current.port || 8088),
    ignoreFocusOut: true,
    validateInput: (v) => {
      const n = Number(v);
      if (!Number.isInteger(n) || n < 1 || n > 65535) return "Enter a port 1–65535";
      return undefined;
    },
  });
  if (portStr === undefined) return;

  const ollamaPortStr = await vscode.window.showInputBox({
    prompt: "Host Ollama port (NEXO_OLLAMA_HOST_PORT)",
    value: String(cfg.get("ollamaPort") || 11434),
    ignoreFocusOut: true,
    validateInput: (v) => {
      const n = Number(v);
      if (!Number.isInteger(n) || n < 1 || n > 65535) return "Enter a port 1–65535";
      return undefined;
    },
  });
  if (ollamaPortStr === undefined) return;

  const model = await vscode.window.showInputBox({
    prompt: "Default Ollama model (optional)",
    value: String(cfg.get("defaultModel") || ""),
    ignoreFocusOut: true,
  });
  if (model === undefined) return;

  try {
    await applyConnectionSettings({
      host,
      port: Number(portStr),
      ollamaPort: Number(ollamaPortStr),
      model,
      notify: true,
    });
  } catch (err) {
    vscode.window.showErrorMessage(`Nexo connection settings: ${err.message}`);
  }
}

/**
 * @param {string} method
 * @param {string} apiPath
 * @param {object} [body]
 * @param {{ track?: boolean, kind?: string }} [opts]
 */
function apiRequest(method, apiPath, body, opts = {}) {
  const base = getBaseUrl();
  const url = new URL(apiPath, base.endsWith("/") ? base : base + "/");
  const payload = body ? JSON.stringify(body) : null;
  const lib = url.protocol === "https:" ? https : http;
  const clientId = `c-${Date.now()}-${Math.random().toString(16).slice(2)}`;

  return new Promise((resolve, reject) => {
    const req = lib.request(
      {
        protocol: url.protocol,
        hostname: url.hostname,
        port: url.port || (url.protocol === "https:" ? 443 : 80),
        path: url.pathname + url.search,
        method,
        headers: {
          Accept: "application/json",
          ...(payload
            ? { "Content-Type": "application/json", "Content-Length": Buffer.byteLength(payload) }
            : {}),
        },
        timeout: 180000,
      },
      (res) => {
        const chunks = [];
        res.on("data", (c) => chunks.push(c));
        res.on("end", () => {
          if (activeRun && activeRun.clientId === clientId) activeRun = null;
          updateBusyStatus();
          const text = Buffer.concat(chunks).toString("utf8");
          let data = null;
          try {
            data = text ? JSON.parse(text) : null;
          } catch {
            data = { raw: text };
          }
          if (res.statusCode && res.statusCode >= 400) {
            const err = new Error(data?.title || data?.detail || `HTTP ${res.statusCode}`);
            err.statusCode = res.statusCode;
            err.data = data;
            reject(err);
            return;
          }
          if (data && data.runId && opts.track) {
            // run already finished by the time response arrives
          }
          resolve(data);
        });
      }
    );

    if (opts.track) {
      activeRun = { clientId, runId: null, kind: opts.kind || method, req };
      updateBusyStatus();
      chatProvider?.postActiveRun({ kind: opts.kind || "run", status: "running" });
    }

    req.on("error", (err) => {
      if (activeRun && activeRun.clientId === clientId) activeRun = null;
      updateBusyStatus();
      reject(err);
    });
    req.on("timeout", () => {
      req.destroy();
      if (activeRun && activeRun.clientId === clientId) activeRun = null;
      updateBusyStatus();
      reject(new Error("Request timed out"));
    });
    if (payload) req.write(payload);
    req.end();
  });
}

/**
 * SSE POST for /api/ide/chat/stream.
 * Events: run | delta | done | error
 * @param {object} body
 * @param {{ kind?: string, onDelta?: (fullText: string) => void }} [opts]
 */
function streamChatRequest(body, opts = {}) {
  const base = getBaseUrl();
  const url = new URL("/api/ide/chat/stream", base.endsWith("/") ? base : base + "/");
  const payload = JSON.stringify(body);
  const lib = url.protocol === "https:" ? https : http;
  const clientId = `c-${Date.now()}-${Math.random().toString(16).slice(2)}`;

  return new Promise((resolve, reject) => {
    const req = lib.request(
      {
        protocol: url.protocol,
        hostname: url.hostname,
        port: url.port || (url.protocol === "https:" ? 443 : 80),
        path: url.pathname + url.search,
        method: "POST",
        headers: {
          Accept: "text/event-stream",
          "Content-Type": "application/json",
          "Content-Length": Buffer.byteLength(payload),
        },
        timeout: 180000,
      },
      (res) => {
        if (res.statusCode && res.statusCode >= 400) {
          const chunks = [];
          res.on("data", (c) => chunks.push(c));
          res.on("end", () => {
            if (activeRun && activeRun.clientId === clientId) activeRun = null;
            updateBusyStatus();
            const text = Buffer.concat(chunks).toString("utf8");
            let data = null;
            try {
              data = text ? JSON.parse(text) : null;
            } catch {
              data = { title: text };
            }
            reject(new Error(data?.title || data?.detail || `HTTP ${res.statusCode}`));
          });
          return;
        }

        let buffer = "";
        let runId = null;
        let message = "";
        let settled = false;

        const finish = (fn) => {
          if (settled) return;
          settled = true;
          if (activeRun && activeRun.clientId === clientId) activeRun = null;
          updateBusyStatus();
          fn();
        };

        res.on("data", (chunk) => {
          buffer += chunk.toString("utf8");
          let sep;
          while ((sep = buffer.indexOf("\n\n")) >= 0) {
            const block = buffer.slice(0, sep);
            buffer = buffer.slice(sep + 2);
            let eventName = "message";
            let dataLine = "";
            for (const line of block.split("\n")) {
              if (line.startsWith("event:")) eventName = line.slice(6).trim();
              else if (line.startsWith("data:")) dataLine += line.slice(5).trim();
            }
            if (!dataLine) continue;
            let payloadObj;
            try {
              payloadObj = JSON.parse(dataLine);
            } catch {
              continue;
            }
            if (eventName === "run") {
              runId = payloadObj.runId || runId;
              if (activeRun && activeRun.clientId === clientId) activeRun.runId = runId;
            } else if (eventName === "delta") {
              message += payloadObj.text || "";
              if (opts.onDelta) opts.onDelta(message);
            } else if (eventName === "done") {
              finish(() =>
                resolve({
                  success: payloadObj.success !== false,
                  message: payloadObj.message || message,
                  runId: payloadObj.runId || runId,
                  modelId: payloadObj.modelId,
                  agentId: payloadObj.agentId,
                })
              );
            } else if (eventName === "error") {
              finish(() => reject(new Error(payloadObj.message || "Stream error")));
            }
          }
        });

        res.on("end", () => {
          finish(() => resolve({ success: true, message, runId }));
        });
      }
    );

    activeRun = { clientId, runId: null, kind: opts.kind || "chat", req };
    updateBusyStatus();
    chatProvider?.postActiveRun({ kind: opts.kind || "chat", status: "running" });

    req.on("error", (err) => {
      if (activeRun && activeRun.clientId === clientId) activeRun = null;
      updateBusyStatus();
      reject(err);
    });
    req.on("timeout", () => {
      req.destroy();
      if (activeRun && activeRun.clientId === clientId) activeRun = null;
      updateBusyStatus();
      reject(new Error("Request timed out"));
    });
    req.write(payload);
    req.end();
  });
}

function updateBusyStatus() {
  const pending = patchStore ? patchStore.pendingCount() : 0;
  const conn = getConnection();
  if (activeRun) {
    statusBar.text = `$(sync~spin) Nexo :${conn.port} ${activeRun.kind}`;
    statusBar.tooltip = `Running ${activeRun.kind}… click to change host/port`;
    return;
  }
  if (statusBar && !String(statusBar.text).includes("debug-disconnect")) {
    statusBar.text =
      pending > 0
        ? `$(git-pull-request) Nexo :${conn.port} (${pending})`
        : `$(check) Nexo :${conn.port}`;
  }
}

async function refreshRuns() {
  try {
    const res = await apiRequest("GET", "/api/ide/runs?take=20");
    lastRuns = res.runs || [];
    chatProvider?.postRuns(lastRuns);
    const running = lastRuns.find((r) => r.status === "running");
    if (running && activeRun && !activeRun.runId) {
      activeRun.runId = running.runId;
    }
  } catch {
    // offline — ignore
  }
}

async function refreshWorkloads() {
  try {
    const [provider, list] = await Promise.all([
      apiRequest("GET", "/api/workloads/provider"),
      apiRequest("GET", "/api/workloads"),
    ]);
    const workloads = list.workloads || [];
    const withReplicas = await Promise.all(
      workloads.map(async (w) => {
        const id = w.workloadId || w.id;
        try {
          const snap = await apiRequest("GET", `/api/workloads/${encodeURIComponent(id)}/replicas`);
          return { ...w, ...snap, workloadId: id };
        } catch {
          return { ...w, workloadId: id };
        }
      })
    );
    lastWorkloads = {
      provider: provider.provider || list.provider || "unknown",
      available: !!provider.available,
      workloads: withReplicas,
    };
    chatProvider?.postWorkloads(lastWorkloads);
  } catch {
    lastWorkloads = null;
    chatProvider?.postWorkloads(null);
  }
}

async function showWorkloads() {
  await refreshWorkloads();
  if (!lastWorkloads) {
    vscode.window.showWarningMessage("Could not load workloads (is agent-server up?).");
    return;
  }
  const { provider, available, workloads } = lastWorkloads;
  if (!workloads.length) {
    vscode.window.showInformationMessage(
      `Workload provider: ${provider} (${available ? "available" : "unavailable"}) — no workloads configured.`
    );
    return;
  }
  const picked = await vscode.window.showQuickPick(
    workloads.map((w) => ({
      label: `${w.displayName || w.workloadId}`,
      description: `${w.readyReplicas ?? "?"}/${w.desiredReplicas ?? "?"} ready`,
      detail: `id=${w.workloadId} · min=${w.minReplicas ?? "?"} max=${w.maxReplicas ?? "?"} · ${provider}`,
      workload: w,
    })),
    { placeHolder: `Workloads (${provider})` }
  );
  if (!picked) return;
  const act = await vscode.window.showInformationMessage(
    `${picked.label}: desired=${picked.workload.desiredReplicas ?? "?"} current=${picked.workload.currentReplicas ?? "?"} ready=${picked.workload.readyReplicas ?? "?"}`,
    "Scale…",
    "Close"
  );
  if (act === "Scale…") {
    await scaleWorkload(picked.workload.workloadId);
  }
}

/**
 * @param {string} [workloadId]
 */
async function scaleWorkload(workloadId) {
  await refreshWorkloads();
  if (!lastWorkloads || !lastWorkloads.workloads.length) {
    vscode.window.showWarningMessage(
      lastWorkloads
        ? `No workloads for provider '${lastWorkloads.provider}'. Configure Nexo:WorkloadScaling.`
        : "Could not load workloads."
    );
    return;
  }
  let id = workloadId;
  let meta = lastWorkloads.workloads.find((w) => w.workloadId === id);
  if (!id) {
    const picked = await vscode.window.showQuickPick(
      lastWorkloads.workloads.map((w) => ({
        label: w.displayName || w.workloadId,
        description: `${w.readyReplicas ?? "?"}/${w.desiredReplicas ?? "?"} ready`,
        detail: w.workloadId,
        workload: w,
      })),
      { placeHolder: "Scale which workload?" }
    );
    if (!picked) return;
    id = picked.workload.workloadId;
    meta = picked.workload;
  }
  const min = meta?.minReplicas ?? 0;
  const max = meta?.maxReplicas ?? 32;
  const current = meta?.desiredReplicas ?? 1;
  const input = await vscode.window.showInputBox({
    prompt: `Desired replicas for ${id} (${min}–${max})`,
    value: String(current),
    validateInput: (v) => {
      const n = Number(v);
      if (!Number.isInteger(n) || n < min || n > max) return `Enter an integer between ${min} and ${max}`;
      return undefined;
    },
  });
  if (input === undefined) return;
  const desired = Number(input);
  const reason = await vscode.window.showInputBox({
    prompt: "Reason (optional)",
    value: "ide scale",
  });
  try {
    const result = await apiRequest("PUT", `/api/workloads/${encodeURIComponent(id)}/replicas`, {
      desiredReplicas: desired,
      reason: reason || "ide scale",
    });
    if (result.success === false) {
      vscode.window.showErrorMessage(result.message || "Scale failed");
    } else {
      vscode.window.showInformationMessage(
        `Scaled ${id}: ${result.previousDesiredReplicas ?? "?"} → ${result.desiredReplicas ?? desired}`
      );
    }
    await refreshWorkloads();
  } catch (err) {
    vscode.window.showErrorMessage(`Scale failed: ${err.message}`);
  }
}

async function cancelActiveRun() {
  const run = activeRun;
  if (!run) {
    // try cancel newest server running
    await refreshRuns();
    const running = lastRuns.find((r) => r.status === "running");
    if (!running) {
      vscode.window.showInformationMessage("No active Nexo run.");
      return;
    }
    try {
      await apiRequest("POST", `/api/ide/runs/${running.runId}/cancel`);
      vscode.window.showInformationMessage(`Cancelled run ${running.runId}`);
      await refreshRuns();
    } catch (err) {
      vscode.window.showErrorMessage(`Cancel failed: ${err.message}`);
    }
    return;
  }

  try {
    run.req.destroy(new Error("cancelled"));
  } catch {
    /* ignore */
  }
  if (run.runId) {
    try {
      await apiRequest("POST", `/api/ide/runs/${run.runId}/cancel`);
    } catch {
      /* ignore */
    }
  }
  activeRun = null;
  updateBusyStatus();
  chatProvider?.appendMessage("assistant", "Run cancelled.");
  await refreshRuns();
}

async function showRuns() {
  await refreshRuns();
  if (!lastRuns.length) {
    vscode.window.showInformationMessage("No IDE runs yet.");
    return;
  }
  const picked = await vscode.window.showQuickPick(
    lastRuns.map((r) => ({
      label: `${r.status} · ${r.kind}`,
      description: r.runId,
      detail: `${r.promptPreview || ""} ${r.agentId ? `agent=${r.agentId}` : ""} ${r.modelId ? `model=${r.modelId}` : ""}`.trim(),
      run: r,
    })),
    { placeHolder: "Nexo run timeline" }
  );
  if (!picked) return;
  if (picked.run.status === "running") {
    const act = await vscode.window.showInformationMessage(
      `Run ${picked.run.runId} is running.`,
      "Cancel",
      "Close"
    );
    if (act === "Cancel") {
      await apiRequest("POST", `/api/ide/runs/${picked.run.runId}/cancel`);
      await refreshRuns();
    }
  } else {
    vscode.window.showInformationMessage(
      `${picked.run.kind} ${picked.run.status}: ${picked.run.message || picked.run.error || ""}`
    );
  }
}

/**
 * @param {boolean} notify
 */
async function checkConnection(notify) {
  try {
    const health = await apiRequest("GET", "/api/ide/health");
    const pending = patchStore.pendingCount();
    const ollama = health.ollamaAvailable ? "ollama ok" : "ollama down";
    const conn = getConnection();
    statusBar.text =
      pending > 0
        ? `$(git-pull-request) Nexo :${conn.port} (${pending})`
        : `$(check) Nexo :${conn.port}`;
    statusBar.tooltip = `Connected · ${ollama} · pending ${pending} · ${conn.baseUrl}`;
    statusBar.backgroundColor = undefined;
    if (notify) {
      vscode.window.showInformationMessage(`Nexo connected (${ollama}) at ${conn.baseUrl}`);
    }
    chatProvider?.postConnection(true, health);
    chatProvider?.postPatches(patchStore.listPending());
    refreshWorkloads().catch(() => {});
    return true;
  } catch (err) {
    statusBar.text = "$(debug-disconnect) Nexo";
    statusBar.tooltip = `Offline · ${getBaseUrl()} · ${err.message}`;
    statusBar.backgroundColor = new vscode.ThemeColor("statusBarItem.warningBackground");
    if (notify) {
      vscode.window.showWarningMessage(
        `Nexo offline at ${getBaseUrl()}: ${err.message}. Start the agent-server or run "Nexo: Start Local Stack".`
      );
    }
    chatProvider?.postConnection(false, null);
    lastWorkloads = null;
    chatProvider?.postWorkloads(null);
    return false;
  }
}

/**
 * @param {boolean} [notify]
 */
async function refreshModels(notify = false) {
  try {
    const models = await apiRequest("GET", "/api/ide/models");
    const agents = await apiRequest("GET", "/api/ide/agents");
    lastAgents = agents.agents || [];
    chatProvider?.postCatalog(models, agents);
    chatProvider?.postAgents(lastAgents);
    if (notify) {
      vscode.window.showInformationMessage(
        `Nexo models: ${(models.models || []).length}, agents: ${lastAgents.length}`
      );
    }
  } catch (err) {
    if (notify) {
      vscode.window.showErrorMessage(`Failed to refresh Nexo catalog: ${err.message}`);
    }
  }
}

function isSafeRelativePath(p) {
  if (!p || typeof p !== "string") return false;
  const n = p.replace(/\\/g, "/").trim();
  if (!n || n.startsWith("/") || n.includes(":")) return false;
  return !n.split("/").includes("..");
}

function workspaceRoot() {
  return vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
}

function toWorkspaceUri(relPath) {
  const root = workspaceRoot();
  if (!root) throw new Error("Open a workspace folder first");
  if (!isSafeRelativePath(relPath)) throw new Error(`Unsafe path: ${relPath}`);
  const full = path.resolve(root, relPath);
  const rootResolved = path.resolve(root);
  if (!full.startsWith(rootResolved + path.sep) && full !== rootResolved) {
    throw new Error(`Path escapes workspace: ${relPath}`);
  }
  return vscode.Uri.file(full);
}

function collectContext() {
  const cfg = vscode.workspace.getConfiguration("nexo");
  const maxChars = cfg.get("maxContextChars", 24000);
  const includeActive = cfg.get("includeActiveFile", true);
  const includeOpen = cfg.get("includeOpenEditors", false);
  /** @type {Array<object>} */
  const contexts = [];
  /** @type {Set<string>} */
  const seen = new Set();

  /**
   * @param {vscode.TextDocument} doc
   * @param {vscode.Selection | undefined} selection
   */
  function pushDoc(doc, selection) {
    if (doc.uri.scheme !== "file") return;
    const folder = vscode.workspace.getWorkspaceFolder(doc.uri);
    const rel = folder
      ? path.relative(folder.uri.fsPath, doc.uri.fsPath).replace(/\\/g, "/")
      : path.basename(doc.uri.fsPath);
    if (!isSafeRelativePath(rel) || seen.has(rel)) return;
    seen.add(rel);

    let content = doc.getText();
    let selectionStart;
    let selectionEnd;
    if (selection && !selection.isEmpty) {
      content = doc.getText(selection);
      selectionStart = selection.start.line + 1;
      selectionEnd = selection.end.line + 1;
    }
    if (content.length > maxChars) {
      content = content.slice(0, maxChars) + "\n/* …truncated… */\n";
    }
    contexts.push({
      path: rel,
      content,
      language: doc.languageId,
      selectionStartLine: selectionStart,
      selectionEndLine: selectionEnd,
    });
  }

  const editor = vscode.window.activeTextEditor;
  if (includeActive && editor) {
    pushDoc(editor.document, editor.selection);
  }
  if (includeOpen) {
    for (const ed of vscode.window.visibleTextEditors) {
      pushDoc(ed.document, undefined);
    }
  }
  return contexts;
}

async function askSelection() {
  const editor = vscode.window.activeTextEditor;
  if (!editor || editor.selection.isEmpty) {
    vscode.window.showWarningMessage("Select code first.");
    return;
  }
  const question = await vscode.window.showInputBox({
    prompt: "Ask Nexo about the selection",
    placeHolder: "What does this do? Any bugs?",
  });
  if (!question) return;
  await runChat(question, "ask");
}

async function planWithPrompt() {
  const prompt = await vscode.window.showInputBox({
    prompt: "Describe the goal for a plan (no file writes)",
    placeHolder: "Add retry logic to the HTTP client",
  });
  if (!prompt) return;
  await runPlan(prompt);
}

async function editWithPrompt() {
  const prompt = await vscode.window.showInputBox({
    prompt: "Describe the edit for the active file(s)",
    placeHolder: "Add null checks and improve naming",
  });
  if (!prompt) return;
  await runEdit(prompt);
}

async function directorGoal() {
  const prompt = await vscode.window.showInputBox({
    prompt: "Director goal (multi-agent orchestration)",
    placeHolder: "Harden the IDE edit path and add regression coverage",
  });
  if (!prompt) return;
  await runDirector(prompt);
}

/**
 * @param {string} prompt
 * @param {string} mode
 * @param {string | null} [modelId]
 * @param {string | null} [agentId]
 */
async function runChat(prompt, mode, modelId, agentId) {
  const cfg = vscode.workspace.getConfiguration("nexo");
  const useStream = cfg.get("streamingChat", true) !== false;
  await vscode.window.withProgress(
    { location: vscode.ProgressLocation.Notification, title: "Nexo chat…", cancellable: true },
    async (_prog, token) => {
      token.onCancellationRequested(() => cancelActiveRun());
      try {
        const body = {
          prompt,
          mode,
          modelId: modelId || cfg.get("defaultModel") || null,
          agentId: agentId || cfg.get("defaultAgent") || null,
          contexts: collectContext(),
        };
        chatProvider?.appendMessage("user", prompt);
        let res;
        if (useStream) {
          chatProvider?.beginStream();
          res = await streamChatRequest(body, {
            kind: "chat",
            onDelta: (full) => chatProvider?.updateStream(full),
          });
          chatProvider?.endStream(res.message || "(empty)");
        } else {
          res = await apiRequest("POST", "/api/ide/chat", body, { track: true, kind: "chat" });
          if (activeRun) activeRun.runId = res.runId || activeRun.runId;
          chatProvider?.appendMessage("assistant", res.message || "(empty)");
        }
        await refreshRuns();
        await vscode.commands.executeCommand("nexo.chatView.focus");
      } catch (err) {
        chatProvider?.endStream(null);
        if (String(err.message || err).includes("cancelled") || err.code === "ECONNRESET") {
          chatProvider?.appendMessage("assistant", "Cancelled.");
          return;
        }
        // fall back to non-streaming if stream route missing
        if (useStream && (err.statusCode === 404 || /404|Not Found/i.test(String(err.message)))) {
          try {
            const body = {
              prompt,
              mode,
              modelId: modelId || cfg.get("defaultModel") || null,
              agentId: agentId || cfg.get("defaultAgent") || null,
              contexts: collectContext(),
            };
            const res = await apiRequest("POST", "/api/ide/chat", body, { track: true, kind: "chat" });
            chatProvider?.appendMessage("assistant", res.message || "(empty)");
            await refreshRuns();
            return;
          } catch (err2) {
            vscode.window.showErrorMessage(`Nexo chat failed: ${err2.message}`);
            return;
          }
        }
        vscode.window.showErrorMessage(`Nexo chat failed: ${err.message}`);
      }
    }
  );
}

/**
 * @param {string} prompt
 * @param {string | null} [modelId]
 * @param {string | null} [agentId]
 */
async function runPlan(prompt, modelId, agentId) {
  const cfg = vscode.workspace.getConfiguration("nexo");
  await vscode.window.withProgress(
    { location: vscode.ProgressLocation.Notification, title: "Nexo planning…", cancellable: true },
    async (_prog, token) => {
      token.onCancellationRequested(() => cancelActiveRun());
      try {
        const body = {
          prompt,
          mode: "plan",
          modelId: modelId || cfg.get("defaultModel") || null,
          agentId: agentId || cfg.get("defaultAgent") || null,
          contexts: collectContext(),
        };
        const res = await apiRequest("POST", "/api/ide/plan", body, { track: true, kind: "plan" });
        if (activeRun) activeRun.runId = res.runId || activeRun.runId;
        chatProvider?.appendMessage("user", `[plan] ${prompt}`);
        let text = res.plan || "(empty plan)";
        if (res.steps && res.steps.length) {
          text +=
            "\n\nSteps:\n" +
            res.steps
              .map((s, i) => `${i + 1}. ${s.title}${s.detail ? ` — ${s.detail}` : ""}`)
              .join("\n");
        }
        chatProvider?.appendMessage("assistant", text);
        await refreshRuns();
        await vscode.commands.executeCommand("nexo.chatView.focus");
      } catch (err) {
        if (String(err.message || err).includes("cancelled") || err.code === "ECONNRESET") return;
        vscode.window.showErrorMessage(`Nexo plan failed: ${err.message}`);
      }
    }
  );
}

/**
 * @param {string} prompt
 * @param {string | null} [modelId]
 * @param {string | null} [agentId]
 */
async function runDirector(prompt, modelId, agentId) {
  const cfg = vscode.workspace.getConfiguration("nexo");
  await vscode.window.withProgress(
    { location: vscode.ProgressLocation.Notification, title: "Nexo director…", cancellable: true },
    async (_prog, token) => {
      token.onCancellationRequested(() => cancelActiveRun());
      try {
        const body = {
          goal: prompt,
          modelId: modelId || cfg.get("defaultModel") || null,
          agentId: agentId || cfg.get("defaultAgent") || null,
          contexts: collectContext(),
        };
        const res = await apiRequest("POST", "/api/ide/director", body, { track: true, kind: "director" });
        if (activeRun) activeRun.runId = res.runId || activeRun.runId;
        chatProvider?.appendMessage("user", `[director] ${prompt}`);
        chatProvider?.appendMessage("assistant", res.message || "(empty)");
        await refreshRuns();
        await vscode.commands.executeCommand("nexo.chatView.focus");
      } catch (err) {
        if (String(err.message || err).includes("cancelled") || err.code === "ECONNRESET") return;
        vscode.window.showErrorMessage(`Nexo director failed: ${err.message}`);
      }
    }
  );
}

/**
 * @param {string} prompt
 * @param {string | null} [modelId]
 * @param {string | null} [agentId]
 */
async function runEdit(prompt, modelId, agentId) {
  const cfg = vscode.workspace.getConfiguration("nexo");
  await vscode.window.withProgress(
    { location: vscode.ProgressLocation.Notification, title: "Nexo proposing edits…", cancellable: true },
    async (_prog, token) => {
      token.onCancellationRequested(() => cancelActiveRun());
      try {
        const body = {
          prompt,
          mode: "edit",
          modelId: modelId || cfg.get("defaultModel") || null,
          agentId: agentId || cfg.get("defaultAgent") || null,
          contexts: collectContext(),
        };
        const res = await apiRequest("POST", "/api/ide/edit", body, { track: true, kind: "edit" });
        if (activeRun) activeRun.runId = res.runId || activeRun.runId;
        const patches = (res.patches || []).filter((p) => isSafeRelativePath(p.path));
        if (!patches.length) {
          vscode.window.showWarningMessage("Nexo returned no safe patches.");
          chatProvider?.appendMessage("assistant", res.rawText || res.message || "No patches");
          await refreshRuns();
          return;
        }

        for (const p of patches) {
          if (p.originalContent != null) continue;
          try {
            const uri = toWorkspaceUri(p.path);
            const doc = await vscode.workspace.openTextDocument(uri);
            p.originalContent = doc.getText();
          } catch {
            p.originalContent = "";
          }
        }

        patchStore.setProposal(res.proposalId || `local-${Date.now()}`, patches);
        patchContentProvider.bump();
        chatProvider?.postPatches(patchStore.listPending());
        chatProvider?.appendMessage(
          "assistant",
          `Proposal ${res.proposalId || ""}: ${patches.length} patch(es) pending review.`
        );
        updatePendingStatus();
        await refreshRuns();

        if (cfg.get("autoOpenDiffs", true)) {
          for (const p of patches.slice(0, 5)) {
            await openPatchDiff(p.path);
          }
        }

        const choice = await vscode.window.showInformationMessage(
          `Nexo proposed ${patches.length} file patch(es).`,
          "Apply All",
          "Review Diffs",
          "Reject All"
        );
        if (choice === "Apply All") await applyAllPatches();
        else if (choice === "Reject All") await rejectAllPatches();
        else await vscode.commands.executeCommand("nexo.chatView.focus");
      } catch (err) {
        if (String(err.message || err).includes("cancelled") || err.code === "ECONNRESET") return;
        vscode.window.showErrorMessage(`Nexo edit failed: ${err.message}`);
      }
    }
  );
}

function updatePendingStatus() {
  const pending = patchStore.pendingCount();
  if (statusBar.text.startsWith("$(debug-disconnect)")) return;
  const conn = getConnection();
  statusBar.text =
    pending > 0
      ? `$(git-pull-request) Nexo :${conn.port} (${pending})`
      : `$(check) Nexo :${conn.port}`;
  chatProvider?.postPatches(patchStore.listPending());
}

/**
 * @param {string} filePath
 */
async function openPatchDiff(filePath) {
  const patch = patchStore.get(filePath);
  if (!patch) {
    vscode.window.showWarningMessage(`No pending patch for ${filePath}`);
    return;
  }

  const proposedUri = vscode.Uri.from({
    scheme: PATCH_SCHEME,
    path: "/" + filePath.replace(/\\/g, "/"),
    query: `side=new&v=${patchStore.version}`,
  });

  let leftUri;
  try {
    leftUri = toWorkspaceUri(filePath);
    await vscode.workspace.fs.stat(leftUri);
  } catch {
    leftUri = vscode.Uri.from({
      scheme: PATCH_SCHEME,
      path: "/" + filePath.replace(/\\/g, "/"),
      query: `side=old&v=${patchStore.version}`,
    });
  }

  await vscode.commands.executeCommand(
    "vscode.diff",
    leftUri,
    proposedUri,
    `Nexo: ${filePath}${patch.summary ? ` — ${patch.summary}` : ""}`
  );
}

async function showPendingPatches() {
  const pending = patchStore.listPending();
  if (!pending.length) {
    vscode.window.showInformationMessage("No pending Nexo patches.");
    return;
  }
  const picked = await vscode.window.showQuickPick(
    pending.map((p) => ({
      label: p.path,
      description: p.summary || "",
      detail: "Enter to open diff",
      path: p.path,
    })),
    { placeHolder: "Pending Nexo patches" }
  );
  if (picked) await openPatchDiff(picked.path);
}

/**
 * @param {string} filePath
 */
async function applyOnePatch(filePath) {
  const patch = patchStore.get(filePath);
  if (!patch) return;
  const applied = await applyPatches([patch]);
  if (applied) {
    patchStore.remove(filePath);
    updatePendingStatus();
    vscode.window.showInformationMessage(`Applied ${filePath}`);
  }
}

/**
 * @param {string} filePath
 */
async function rejectOnePatch(filePath) {
  patchStore.remove(filePath);
  updatePendingStatus();
  vscode.window.showInformationMessage(`Rejected ${filePath}`);
}

async function applyAllPatches() {
  const pending = patchStore.listPending();
  if (!pending.length) {
    vscode.window.showInformationMessage("No pending patches.");
    return;
  }
  const ok = await applyPatches(pending);
  if (ok) {
    patchStore.clearPending();
    updatePendingStatus();
    vscode.window.showInformationMessage(`Applied ${pending.length} patch(es). Use "Nexo: Undo Last Apply" to revert.`);
  }
}

async function rejectAllPatches() {
  const n = patchStore.pendingCount();
  patchStore.clearPending();
  updatePendingStatus();
  vscode.window.showInformationMessage(`Rejected ${n} pending patch(es).`);
}

/**
 * @param {Array<object>} patches
 */
async function applyPatches(patches) {
  const root = workspaceRoot();
  if (!root) {
    vscode.window.showErrorMessage("Open a workspace folder to apply patches.");
    return false;
  }

  /** @type {Array<{path:string, before:string|null, after:string, existed:boolean}>} */
  const undoEntries = [];
  const edit = new vscode.WorkspaceEdit();

  for (const patch of patches) {
    if (!isSafeRelativePath(patch.path)) {
      vscode.window.showWarningMessage(`Skipped unsafe path: ${patch.path}`);
      continue;
    }
    const uri = toWorkspaceUri(patch.path);
    let before = null;
    let existed = true;
    try {
      const doc = await vscode.workspace.openTextDocument(uri);
      before = doc.getText();
      const full = new vscode.Range(doc.positionAt(0), doc.positionAt(doc.getText().length));
      edit.replace(uri, full, patch.newContent || "");
    } catch {
      existed = false;
      before = null;
      edit.createFile(uri, { ignoreIfExists: true });
      edit.insert(uri, new vscode.Position(0, 0), patch.newContent || "");
    }
    undoEntries.push({
      path: patch.path,
      before,
      after: patch.newContent || "",
      existed,
    });
  }

  const ok = await vscode.workspace.applyEdit(edit);
  if (!ok) {
    vscode.window.showErrorMessage("WorkspaceEdit was rejected.");
    return false;
  }
  patchStore.pushUndo(undoEntries);
  return true;
}

async function undoLastApply() {
  const batch = patchStore.popUndo();
  if (!batch || !batch.length) {
    vscode.window.showInformationMessage("Nothing to undo.");
    return;
  }

  const edit = new vscode.WorkspaceEdit();
  for (const entry of batch) {
    const uri = toWorkspaceUri(entry.path);
    if (!entry.existed || entry.before == null) {
      edit.deleteFile(uri, { ignoreIfNotExists: true });
      continue;
    }
    try {
      const doc = await vscode.workspace.openTextDocument(uri);
      const full = new vscode.Range(doc.positionAt(0), doc.positionAt(doc.getText().length));
      edit.replace(uri, full, entry.before);
    } catch {
      edit.createFile(uri, { ignoreIfExists: true });
      edit.insert(uri, new vscode.Position(0, 0), entry.before);
    }
  }

  const ok = await vscode.workspace.applyEdit(edit);
  vscode.window.showInformationMessage(
    ok ? `Undid ${batch.length} file change(s).` : "Undo failed."
  );
}

async function startLocalStack() {
  const cfg = vscode.workspace.getConfiguration("nexo");
  let script = cfg.get("startStackScript") || "";
  if (!script) {
    const folder = workspaceRoot();
    if (folder) {
      const candidate = path.join(folder, "scripts", "Start-FullstackAgentServer.ps1");
      if (fs.existsSync(candidate)) script = candidate;
    }
  }
  if (!script || !fs.existsSync(script)) {
    vscode.window.showErrorMessage(
      "Start script not found. Set nexo.startStackScript or open the Nexo repo workspace."
    );
    return;
  }

  const conn = getConnection();
  const ollamaPort = Number(cfg.get("ollamaPort") || 11434);
  const model = String(cfg.get("defaultModel") || "").trim();
  const useBundled = !!cfg.get("useBundledOllama");

  const args = [
    "-ApiHost",
    conn.host,
    "-ApiPort",
    String(conn.port),
    "-OllamaHostPort",
    String(ollamaPort),
  ];
  if (model) {
    args.push("-OllamaModel", model);
  }
  if (useBundled) {
    args.push("-UseBundledOllama");
  }

  const terminal = vscode.window.createTerminal({ name: "Nexo Stack" });
  terminal.show();
  const quotedArgs = args.map((a) => (/\s/.test(a) ? `"${a}"` : a)).join(" ");
  terminal.sendText(
    `powershell -NoProfile -ExecutionPolicy Bypass -File "${script}" ${quotedArgs}`
  );
  vscode.window.showInformationMessage(
    `Starting Nexo local stack on ${conn.host}:${conn.port} (ollama host port ${ollamaPort})…`
  );
  setTimeout(() => checkConnection(true), 15000);
}

class PatchStore {
  constructor() {
    /** @type {Map<string, object>} */
    this.pending = new Map();
    /** @type {Array<Array<object>>} */
    this.undoStack = [];
    this.proposalId = null;
    this.version = 1;
  }

  /**
   * @param {string} proposalId
   * @param {Array<object>} patches
   */
  setProposal(proposalId, patches) {
    this.pending.clear();
    this.proposalId = proposalId;
    for (const p of patches) {
      this.pending.set(p.path.replace(/\\/g, "/"), {
        path: p.path.replace(/\\/g, "/"),
        originalContent: p.originalContent ?? p.original_content ?? "",
        newContent: p.newContent ?? p.new_content ?? "",
        summary: p.summary || "",
      });
    }
    this.version += 1;
  }

  listPending() {
    return [...this.pending.values()];
  }

  pendingCount() {
    return this.pending.size;
  }

  get(filePath) {
    return this.pending.get(String(filePath).replace(/\\/g, "/"));
  }

  remove(filePath) {
    this.pending.delete(String(filePath).replace(/\\/g, "/"));
    this.version += 1;
  }

  clearPending() {
    this.pending.clear();
    this.proposalId = null;
    this.version += 1;
  }

  /**
   * @param {Array<object>} entries
   */
  pushUndo(entries) {
    this.undoStack.push(entries);
    if (this.undoStack.length > 20) this.undoStack.shift();
  }

  popUndo() {
    return this.undoStack.pop() || null;
  }
}

class PatchContentProvider {
  /**
   * @param {PatchStore} store
   */
  constructor(store) {
    this.store = store;
    this._onDidChange = new vscode.EventEmitter();
    this.onDidChange = this._onDidChange.event;
  }

  bump() {
    this._onDidChange.fire(vscode.Uri.parse(`${PATCH_SCHEME}:/`));
    for (const p of this.store.listPending()) {
      this._onDidChange.fire(
        vscode.Uri.from({
          scheme: PATCH_SCHEME,
          path: "/" + p.path,
          query: `side=new&v=${this.store.version}`,
        })
      );
    }
  }

  /**
   * @param {vscode.Uri} uri
   */
  provideTextDocumentContent(uri) {
    const filePath = uri.path.replace(/^\//, "");
    const side = new URLSearchParams(uri.query).get("side") || "new";
    const patch = this.store.get(filePath);
    if (!patch) return "";
    return side === "old" ? patch.originalContent || "" : patch.newContent || "";
  }
}

class ChatViewProvider {
  /**
   * @param {vscode.Uri} extensionUri
   */
  constructor(extensionUri) {
    this.extensionUri = extensionUri;
    /** @type {vscode.WebviewView | undefined} */
    this.view = undefined;
  }

  /**
   * @param {vscode.WebviewView} webviewView
   */
  resolveWebviewView(webviewView) {
    this.view = webviewView;
    webviewView.webview.options = { enableScripts: true };
    webviewView.webview.html = this.getHtml();
    webviewView.webview.onDidReceiveMessage(async (msg) => {
      if (msg.type === "chat") {
        await runChat(msg.prompt, msg.mode || "ask", msg.modelId, msg.agentId);
      } else if (msg.type === "plan") {
        await runPlan(msg.prompt, msg.modelId, msg.agentId);
      } else if (msg.type === "edit") {
        await runEdit(msg.prompt, msg.modelId, msg.agentId);
      } else if (msg.type === "director") {
        await runDirector(msg.prompt, msg.modelId, msg.agentId);
      } else if (msg.type === "refresh") {
        await refreshModels();
        await refreshRuns();
        await refreshWorkloads();
        await checkConnection(false);
      } else if (msg.type === "check") {
        await checkConnection(true);
      } else if (msg.type === "setConnection") {
        try {
          await applyConnectionSettings({
            host: msg.host,
            port: Number(msg.port),
            ollamaPort: msg.ollamaPort != null ? Number(msg.ollamaPort) : undefined,
            notify: true,
          });
        } catch (err) {
          vscode.window.showErrorMessage(`Nexo port/host: ${err.message}`);
        }
      } else if (msg.type === "configureConnection") {
        await configureConnection();
      } else if (msg.type === "cancel") {
        await cancelActiveRun();
      } else if (msg.type === "applyAll") {
        await applyAllPatches();
      } else if (msg.type === "rejectAll") {
        await rejectAllPatches();
      } else if (msg.type === "undo") {
        await undoLastApply();
      } else if (msg.type === "diff") {
        await openPatchDiff(msg.path);
      } else if (msg.type === "applyOne") {
        await applyOnePatch(msg.path);
      } else if (msg.type === "rejectOne") {
        await rejectOnePatch(msg.path);
      } else if (msg.type === "cancelRun") {
        try {
          await apiRequest("POST", `/api/ide/runs/${msg.runId}/cancel`);
          await refreshRuns();
        } catch (err) {
          vscode.window.showErrorMessage(`Cancel failed: ${err.message}`);
        }
      } else if (msg.type === "scaleWorkload") {
        await scaleWorkload(msg.workloadId);
      } else if (msg.type === "refreshWorkloads") {
        await refreshWorkloads();
      }
    });
    refreshModels().catch(() => {});
    refreshRuns().catch(() => {});
    refreshWorkloads().catch(() => {});
    this.postConnectionSettings(getConnection());
    checkConnection(false);
  }

  postConnectionSettings(conn) {
    const cfg = vscode.workspace.getConfiguration("nexo");
    this.view?.webview.postMessage({
      type: "connectionSettings",
      host: conn.host,
      port: conn.port,
      baseUrl: conn.baseUrl,
      ollamaPort: Number(cfg.get("ollamaPort") || 11434),
    });
  }

  postConnection(ok, health) {
    const conn = getConnection();
    this.view?.webview.postMessage({
      type: "connection",
      ok,
      health,
      baseUrl: conn.baseUrl,
      host: conn.host,
      port: conn.port,
    });
  }

  postCatalog(models, agents) {
    this.view?.webview.postMessage({ type: "catalog", models, agents });
  }

  postAgents(agents) {
    this.view?.webview.postMessage({ type: "agentsRoster", agents });
  }

  postPatches(patches) {
    this.view?.webview.postMessage({ type: "patches", patches });
  }

  postRuns(runs) {
    this.view?.webview.postMessage({ type: "runs", runs });
  }

  postWorkloads(data) {
    this.view?.webview.postMessage({ type: "workloads", data });
  }

  beginStream() {
    this.view?.webview.postMessage({ type: "streamStart" });
  }

  updateStream(text) {
    this.view?.webview.postMessage({ type: "stream", text });
  }

  endStream(text) {
    this.view?.webview.postMessage({ type: "streamEnd", text });
  }

  postActiveRun(run) {
    this.view?.webview.postMessage({ type: "activeRun", run });
  }

  appendMessage(role, text) {
    this.view?.webview.postMessage({ type: "message", role, text });
  }

  getHtml() {
    return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'; script-src 'unsafe-inline';" />
  <style>
    :root { color-scheme: light dark; }
    body { font-family: var(--vscode-font-family); font-size: var(--vscode-font-size); margin: 0; padding: 8px; color: var(--vscode-foreground); }
    .status { font-size: 12px; opacity: 0.85; margin-bottom: 6px; }
    .ok { color: var(--vscode-testing-iconPassed); }
    .bad { color: var(--vscode-testing-iconFailed); }
    .conn { display: grid; grid-template-columns: 1fr 72px 64px; gap: 4px; margin-bottom: 8px; align-items: center; }
    .conn label { display: none; }
    .conn input { width: 100%; box-sizing: border-box; background: var(--vscode-input-background); color: var(--vscode-input-foreground); border: 1px solid var(--vscode-input-border); padding: 4px 6px; font-size: 12px; }
    .conn button { flex: none; padding: 4px 6px; font-size: 11px; }
    #log { height: calc(100vh - 400px); min-height: 100px; overflow: auto; border: 1px solid var(--vscode-widget-border); padding: 8px; margin-bottom: 8px; white-space: pre-wrap; }
    .msg { margin: 0 0 10px; }
    .role { font-weight: 600; font-size: 11px; text-transform: uppercase; opacity: 0.7; }
    textarea { width: 100%; box-sizing: border-box; min-height: 56px; resize: vertical; background: var(--vscode-input-background); color: var(--vscode-input-foreground); border: 1px solid var(--vscode-input-border); padding: 6px; }
    .row { display: flex; gap: 6px; margin-top: 6px; }
    button { flex: 1; background: var(--vscode-button-background); color: var(--vscode-button-foreground); border: 0; padding: 6px 8px; cursor: pointer; }
    button.secondary { background: var(--vscode-button-secondaryBackground); color: var(--vscode-button-secondaryForeground); }
    select { width: 100%; margin-bottom: 6px; background: var(--vscode-dropdown-background); color: var(--vscode-dropdown-foreground); border: 1px solid var(--vscode-dropdown-border); padding: 4px; }
    #patches, #runs, #agentsRoster, #workloads { border: 1px solid var(--vscode-widget-border); padding: 6px; margin-bottom: 8px; max-height: 110px; overflow: auto; display: none; }
    #patches h4, #runs h4, #agentsRoster h4, #workloads h4 { margin: 0 0 6px; font-size: 12px; }
    .patch, .run, .agent, .workload { display: flex; flex-wrap: wrap; gap: 4px; align-items: center; margin-bottom: 6px; font-size: 11px; }
    .patch code, .run code, .agent code, .workload code { flex: 1 1 100%; opacity: 0.9; }
    .patch button, .run button, .workload button { flex: 0 0 auto; padding: 2px 6px; font-size: 11px; }
    .running { color: var(--vscode-charts-yellow); }
    .completed { color: var(--vscode-testing-iconPassed); }
    .failed, .cancelled { color: var(--vscode-testing-iconFailed); }
  </style>
</head>
<body>
  <div id="status" class="status bad">Checking Nexo…</div>
  <div class="conn" title="Agent-server host and HTTP port (NEXO_AGENT_SERVER_HTTP_PORT)">
    <input id="apiHost" type="text" placeholder="127.0.0.1" aria-label="Host" />
    <input id="apiPort" type="number" min="1" max="65535" placeholder="8088" aria-label="Port" />
    <button id="applyConn" class="secondary" title="Apply host/port">Apply</button>
  </div>
  <select id="models" title="Model"></select>
  <select id="agents" title="Agent"></select>
  <div id="agentsRoster"></div>
  <div id="workloads"></div>
  <div id="runs"></div>
  <div id="patches"></div>
  <div id="log"></div>
  <textarea id="prompt" placeholder="Ask, plan, edit, or director with Nexo…"></textarea>
  <div class="row">
    <button id="ask">Ask</button>
    <button id="plan" class="secondary">Plan</button>
    <button id="edit">Edit</button>
    <button id="director" class="secondary">Director</button>
  </div>
  <div class="row">
    <button id="cancel" class="secondary">Cancel run</button>
    <button id="applyAll" class="secondary">Apply all</button>
    <button id="rejectAll" class="secondary">Reject all</button>
    <button id="undo" class="secondary">Undo</button>
  </div>
  <div class="row">
    <button id="refresh" class="secondary">Refresh</button>
    <button id="check" class="secondary">Check</button>
  </div>
  <script>
    const vscode = acquireVsCodeApi();
    const log = document.getElementById('log');
    const status = document.getElementById('status');
    const apiHost = document.getElementById('apiHost');
    const apiPort = document.getElementById('apiPort');
    const models = document.getElementById('models');
    const agents = document.getElementById('agents');
    const prompt = document.getElementById('prompt');
    const patchesEl = document.getElementById('patches');
    const runsEl = document.getElementById('runs');
    const agentsRoster = document.getElementById('agentsRoster');
    const workloadsEl = document.getElementById('workloads');

    function fillConnection(host, port) {
      if (host != null) apiHost.value = host;
      if (port != null) apiPort.value = String(port);
    }

    function addMsg(role, text) {
      const div = document.createElement('div');
      div.className = 'msg';
      div.innerHTML = '<div class="role"></div><div></div>';
      div.children[0].textContent = role;
      div.children[1].textContent = text;
      log.appendChild(div);
      log.scrollTop = log.scrollHeight;
    }

    function fillSelect(el, items, getId, getLabel, emptyLabel) {
      el.innerHTML = '';
      const opt0 = document.createElement('option');
      opt0.value = '';
      opt0.textContent = emptyLabel;
      el.appendChild(opt0);
      (items || []).forEach(item => {
        const o = document.createElement('option');
        o.value = getId(item);
        o.textContent = getLabel(item);
        el.appendChild(o);
      });
    }

    function renderPatches(patches) {
      if (!patches || !patches.length) {
        patchesEl.style.display = 'none';
        patchesEl.innerHTML = '';
        return;
      }
      patchesEl.style.display = 'block';
      patchesEl.innerHTML = '<h4>Pending patches (' + patches.length + ')</h4>';
      patches.forEach(p => {
        const row = document.createElement('div');
        row.className = 'patch';
        const code = document.createElement('code');
        code.textContent = p.path + (p.summary ? ' — ' + p.summary : '');
        row.appendChild(code);
        [['Diff','diff'],['Apply','applyOne'],['Reject','rejectOne']].forEach(([label, type]) => {
          const b = document.createElement('button');
          b.textContent = label;
          b.className = 'secondary';
          b.onclick = () => vscode.postMessage({ type, path: p.path });
          row.appendChild(b);
        });
        patchesEl.appendChild(row);
      });
    }

    function renderRuns(runs) {
      if (!runs || !runs.length) {
        runsEl.style.display = 'none';
        runsEl.innerHTML = '';
        return;
      }
      runsEl.style.display = 'block';
      runsEl.innerHTML = '<h4>Run timeline</h4>';
      runs.slice(0, 8).forEach(r => {
        const row = document.createElement('div');
        row.className = 'run';
        const code = document.createElement('code');
        code.className = r.status || '';
        code.textContent = (r.status || '') + ' · ' + (r.kind || '') + ' · ' + (r.promptPreview || r.runId || '');
        row.appendChild(code);
        if (r.status === 'running') {
          const b = document.createElement('button');
          b.textContent = 'Cancel';
          b.className = 'secondary';
          b.onclick = () => vscode.postMessage({ type: 'cancelRun', runId: r.runId });
          row.appendChild(b);
        }
        runsEl.appendChild(row);
      });
    }

    function renderAgents(list) {
      if (!list || !list.length) {
        agentsRoster.style.display = 'none';
        agentsRoster.innerHTML = '';
        return;
      }
      agentsRoster.style.display = 'block';
      agentsRoster.innerHTML = '<h4>Agents</h4>';
      list.slice(0, 8).forEach(a => {
        const row = document.createElement('div');
        row.className = 'agent';
        const code = document.createElement('code');
        const model = a.modelName || a.modelProvider || '';
        code.textContent = (a.name || a.id) + ' [' + (a.role || '') + '] ' + (model ? '· ' + model : '') + ' · ' + (a.state || '');
        row.appendChild(code);
        agentsRoster.appendChild(row);
      });
    }

    function renderWorkloads(data) {
      if (!data) {
        workloadsEl.style.display = 'none';
        workloadsEl.innerHTML = '';
        return;
      }
      workloadsEl.style.display = 'block';
      const list = data.workloads || [];
      const title = 'Workloads · ' + (data.provider || '?') + (data.available ? '' : ' (unavailable)');
      workloadsEl.innerHTML = '<h4>' + title + '</h4>';
      if (!list.length) {
        const empty = document.createElement('code');
        empty.textContent = 'No workloads configured (provider=' + (data.provider || 'null') + ')';
        workloadsEl.appendChild(empty);
        return;
      }
      list.slice(0, 8).forEach(w => {
        const row = document.createElement('div');
        row.className = 'workload';
        const code = document.createElement('code');
        const ready = w.readyReplicas != null ? w.readyReplicas : '?';
        const desired = w.desiredReplicas != null ? w.desiredReplicas : '?';
        const current = w.currentReplicas != null ? w.currentReplicas : '?';
        code.textContent = (w.displayName || w.workloadId) + ' · ready ' + ready + ' / desired ' + desired + ' / current ' + current;
        row.appendChild(code);
        if (data.provider && data.provider !== 'null' && data.available) {
          const b = document.createElement('button');
          b.textContent = 'Scale';
          b.className = 'secondary';
          b.onclick = () => vscode.postMessage({ type: 'scaleWorkload', workloadId: w.workloadId });
          row.appendChild(b);
        }
        workloadsEl.appendChild(row);
      });
    }

    window.addEventListener('message', (event) => {
      const msg = event.data;
      if (msg.type === 'connectionSettings') {
        fillConnection(msg.host, msg.port);
      } else if (msg.type === 'connection') {
        status.className = 'status ' + (msg.ok ? 'ok' : 'bad');
        status.textContent = msg.ok
          ? ('Connected · ' + (msg.health?.ollamaAvailable ? 'Ollama' : 'Ollama offline')
              + (msg.health?.features?.chatStream ? ' · stream' : '')
              + (msg.baseUrl ? ' · ' + msg.baseUrl : ''))
          : ('Offline — ' + (msg.baseUrl || 'start agent-server') + ' · edit port above');
        if (msg.host != null || msg.port != null) fillConnection(msg.host, msg.port);
      } else if (msg.type === 'catalog') {
        fillSelect(models, msg.models?.models, m => m.id, m => m.id + (m.localOnly ? ' (local)' : ''), 'Default model');
        fillSelect(agents, msg.agents?.agents, a => a.id, a => a.name + ' [' + a.role + ']', 'Default agent');
      } else if (msg.type === 'agentsRoster') {
        renderAgents(msg.agents || []);
      } else if (msg.type === 'message') {
        addMsg(msg.role, msg.text);
      } else if (msg.type === 'patches') {
        renderPatches(msg.patches || []);
      } else if (msg.type === 'runs') {
        renderRuns(msg.runs || []);
      } else if (msg.type === 'workloads') {
        renderWorkloads(msg.data);
      } else if (msg.type === 'streamStart') {
        const div = document.createElement('div');
        div.className = 'msg';
        div.id = 'streaming';
        div.innerHTML = '<div class="role"></div><div></div>';
        div.children[0].textContent = 'assistant';
        div.children[1].textContent = '…';
        log.appendChild(div);
        log.scrollTop = log.scrollHeight;
      } else if (msg.type === 'stream') {
        const el = document.getElementById('streaming');
        if (el) {
          el.children[1].textContent = msg.text || '';
          log.scrollTop = log.scrollHeight;
        }
      } else if (msg.type === 'streamEnd') {
        const el = document.getElementById('streaming');
        if (el) {
          if (msg.text == null) el.remove();
          else {
            el.children[1].textContent = msg.text || '(empty)';
            el.removeAttribute('id');
          }
          log.scrollTop = log.scrollHeight;
        } else if (msg.text != null) {
          addMsg('assistant', msg.text || '(empty)');
        }
      }
    });

    function payload(type) {
      const p = prompt.value.trim();
      if (!p) return;
      vscode.postMessage({ type, prompt: p, modelId: models.value || null, agentId: agents.value || null });
      prompt.value = '';
    }

    document.getElementById('ask').onclick = () => payload('chat');
    document.getElementById('plan').onclick = () => payload('plan');
    document.getElementById('edit').onclick = () => payload('edit');
    document.getElementById('director').onclick = () => payload('director');
    document.getElementById('cancel').onclick = () => vscode.postMessage({ type: 'cancel' });
    document.getElementById('applyAll').onclick = () => vscode.postMessage({ type: 'applyAll' });
    document.getElementById('rejectAll').onclick = () => vscode.postMessage({ type: 'rejectAll' });
    document.getElementById('undo').onclick = () => vscode.postMessage({ type: 'undo' });
    document.getElementById('refresh').onclick = () => vscode.postMessage({ type: 'refresh' });
    document.getElementById('check').onclick = () => vscode.postMessage({ type: 'check' });
    document.getElementById('applyConn').onclick = () => {
      const port = Number(apiPort.value);
      if (!Number.isInteger(port) || port < 1 || port > 65535) {
        status.className = 'status bad';
        status.textContent = 'Port must be 1–65535';
        return;
      }
      vscode.postMessage({ type: 'setConnection', host: apiHost.value.trim() || '127.0.0.1', port });
    };
    apiPort.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') document.getElementById('applyConn').click();
    });
    apiHost.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') document.getElementById('applyConn').click();
    });
  </script>
</body>
</html>`;
  }
}

module.exports = { activate, deactivate };
