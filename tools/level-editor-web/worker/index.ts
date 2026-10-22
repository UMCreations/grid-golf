import { validateLevel } from "../src/levelData";
import type { LevelAuthoringDto } from "../src/types";

interface Env {
  ALLOWED_ORIGINS: string;
  ALLOWED_HOSTNAMES: string;
  GITHUB_OWNER: string;
  GITHUB_REPOSITORY: string;
  GITHUB_BASE_BRANCH: string;
  GITHUB_APP_ID: string;
  GITHUB_INSTALLATION_ID: string;
  GITHUB_APP_PRIVATE_KEY: string;
  TURNSTILE_SECRET: string;
}

const GITHUB_API = "https://api.github.com";
const TURNSTILE_VERIFY = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
const TURNSTILE_ACTION = "turnstile-spin-v2";
const MAX_REQUEST_BYTES = 64 * 1024;

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const origin = request.headers.get("Origin");
    const allowedOrigins = splitList(env.ALLOWED_ORIGINS);
    const headers = corsHeaders(origin, allowedOrigins);
    const url = new URL(request.url);

    if (request.method === "OPTIONS") {
      return new Response(null, { status: 204, headers });
    }

    if (request.method === "GET" && url.pathname === "/health") {
      return json({ ok: true }, 200, headers);
    }

    if (request.method !== "POST" || url.pathname !== "/submit") {
      return json({ error: "Not found." }, 404, headers);
    }

    if (!origin || !allowedOrigins.includes(origin)) {
      return json({ error: "This editor is not allowed to submit levels." }, 403, headers);
    }

    if (!isConfigured(env)) {
      return json({ error: "Community submissions are not configured yet." }, 503, headers);
    }

    const contentLength = Number(request.headers.get("Content-Length") ?? 0);
    if (contentLength > MAX_REQUEST_BYTES) {
      return json({ error: "The level file is too large." }, 413, headers);
    }

    try {
      const rawBody = await request.text();
      if (new TextEncoder().encode(rawBody).byteLength > MAX_REQUEST_BYTES) {
        return json({ error: "The level file is too large." }, 413, headers);
      }

      let body: { level?: unknown; turnstileToken?: unknown };
      try {
        body = JSON.parse(rawBody) as typeof body;
      } catch {
        return json({ error: "The request must contain valid JSON." }, 400, headers);
      }
      if (!body || typeof body !== "object" || Array.isArray(body)) {
        return json({ error: "The request body is invalid." }, 400, headers);
      }
      if (typeof body.turnstileToken !== "string" || body.turnstileToken.length > 2048) {
        return json({ error: "Complete the bot check and try again." }, 400, headers);
      }

      const validation = validateLevel(body.level);
      if (validation.errors.length > 0) {
        return json({ error: "The level has validation errors.", details: validation.errors.slice(0, 8) }, 400, headers);
      }

      const turnstile = await verifyTurnstile(body.turnstileToken, request, env);
      if (!turnstile) {
        return json({ error: "Bot check failed. Please try again." }, 403, headers);
      }

      const level = body.level as LevelAuthoringDto;
      const submission = await createSubmission(level, env);
      return json({ status: "pending_review", ...submission }, 202, headers);
    } catch (error) {
      console.error("Level submission failed.", error instanceof Error ? error.message : "Unknown error");
      return json({ error: "The level could not be submitted. Please try again later." }, 502, headers);
    }
  },
};

async function verifyTurnstile(token: string, request: Request, env: Env): Promise<boolean> {
  try {
    const response = await fetch(TURNSTILE_VERIFY, {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body: new URLSearchParams({
        secret: env.TURNSTILE_SECRET,
        response: token,
        remoteip: request.headers.get("CF-Connecting-IP") ?? "",
      }),
    });

    if (!response.ok) return false;
    const result = await response.json() as { success?: boolean; action?: string; hostname?: string };
    return result.success === true &&
      result.action === TURNSTILE_ACTION &&
      splitList(env.ALLOWED_HOSTNAMES).includes(result.hostname ?? "");
  } catch {
    return false;
  }
}

async function createSubmission(level: LevelAuthoringDto, env: Env): Promise<{ levelId: string; pullRequestUrl: string }> {
  const suffix = crypto.randomUUID().replaceAll("-", "").slice(0, 10);
  const slug = slugify(level.name) || "level";
  const submittedLevel = {
    ...level,
    id: `${level.mode.toLowerCase()}-${slug}-${suffix}`,
    metadata: { ...level.metadata, updatedAt: new Date().toISOString() },
  };
  const branch = `level-submission-${suffix}`;
  const path = `levels/pending/${submittedLevel.id}.json`;
  const token = await createInstallationToken(env);
  const headers = githubHeaders(token);
  const baseRef = await githubRequest<{ object: { sha: string } }>(
    `${GITHUB_API}/repos/${env.GITHUB_OWNER}/${env.GITHUB_REPOSITORY}/git/ref/heads/${encodeURIComponent(env.GITHUB_BASE_BRANCH)}`,
    { headers },
  );

  await githubRequest(`${GITHUB_API}/repos/${env.GITHUB_OWNER}/${env.GITHUB_REPOSITORY}/git/refs`, {
    method: "POST",
    headers,
    body: JSON.stringify({ ref: `refs/heads/${branch}`, sha: baseRef.object.sha }),
  });

  const content = JSON.stringify(submittedLevel, null, 2) + "\n";
  await githubRequest(`${GITHUB_API}/repos/${env.GITHUB_OWNER}/${env.GITHUB_REPOSITORY}/contents/${path}`, {
    method: "PUT",
    headers,
    body: JSON.stringify({
      message: `Submit community level: ${submittedLevel.id}`,
      content: encodeBase64(new TextEncoder().encode(content)),
      branch,
    }),
  });

  const title = `Community level submission: ${submittedLevel.name.replace(/[\r\n]+/g, " ").slice(0, 80)}`;
  const pullRequest = await githubRequest<{ html_url: string }>(
    `${GITHUB_API}/repos/${env.GITHUB_OWNER}/${env.GITHUB_REPOSITORY}/pulls`,
    {
      method: "POST",
      headers,
      body: JSON.stringify({
        title,
        head: branch,
        base: env.GITHUB_BASE_BRANCH,
        body: "Account-free level submission from the browser editor. Review this level before moving it to `levels/published/` and adding it to `catalog.json`.",
      }),
    },
  );

  return { levelId: submittedLevel.id, pullRequestUrl: pullRequest.html_url };
}

async function createInstallationToken(env: Env): Promise<string> {
  if (!env.GITHUB_APP_ID || !env.GITHUB_INSTALLATION_ID || !env.GITHUB_APP_PRIVATE_KEY) {
    throw new Error("GitHub App credentials are not configured.");
  }

  const jwt = await createAppJwt(env.GITHUB_APP_ID, env.GITHUB_APP_PRIVATE_KEY);
  const response = await fetch(
    `${GITHUB_API}/app/installations/${env.GITHUB_INSTALLATION_ID}/access_tokens`,
    { method: "POST", headers: githubHeaders(jwt) },
  );
  if (!response.ok) throw new Error(`GitHub installation token request failed (${response.status}).`);
  const result = await response.json() as { token: string };
  return result.token;
}

async function createAppJwt(appId: string, privateKeyPem: string): Promise<string> {
  const now = Math.floor(Date.now() / 1000);
  const unsigned = `${base64Url(JSON.stringify({ alg: "RS256", typ: "JWT" }))}.${base64Url(JSON.stringify({
    iat: now - 60,
    exp: now + 540,
    iss: appId,
  }))}`;
  const keyBytes = pemToBytes(privateKeyPem);
  const key = await crypto.subtle.importKey(
    "pkcs8",
    keyBytes,
    { name: "RSASSA-PKCS1-v1_5", hash: "SHA-256" },
    false,
    ["sign"],
  );
  const signature = await crypto.subtle.sign("RSASSA-PKCS1-v1_5", key, new TextEncoder().encode(unsigned));
  return `${unsigned}.${base64Url(signature)}`;
}

async function githubRequest<T = unknown>(url: string, init: RequestInit): Promise<T> {
  const response = await fetch(url, init);
  if (!response.ok) throw new Error(`GitHub API request failed (${response.status}).`);
  return await response.json() as T;
}

function githubHeaders(token: string): HeadersInit {
  return {
    Accept: "application/vnd.github+json",
    Authorization: `Bearer ${token}`,
    "Content-Type": "application/json",
    "User-Agent": "GridGolfLevelSubmissions",
    "X-GitHub-Api-Version": "2022-11-28",
  };
}

function pemToBytes(pem: string): ArrayBuffer {
  const cleanPem = pem.replaceAll("\\n", "\n");
  const base64 = cleanPem.replace(/-----BEGIN PRIVATE KEY-----|-----END PRIVATE KEY-----|\s/g, "");
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let index = 0; index < binary.length; index += 1) bytes[index] = binary.charCodeAt(index);
  return bytes.buffer;
}

function encodeBase64(bytes: Uint8Array): string {
  let binary = "";
  for (let index = 0; index < bytes.length; index += 0x8000) {
    binary += String.fromCharCode(...bytes.subarray(index, index + 0x8000));
  }
  return btoa(binary);
}

function base64Url(value: string | ArrayBuffer): string {
  const bytes = typeof value === "string" ? new TextEncoder().encode(value) : new Uint8Array(value);
  let binary = "";
  for (let index = 0; index < bytes.length; index += 0x8000) {
    binary += String.fromCharCode(...bytes.subarray(index, index + 0x8000));
  }
  return btoa(binary).replace(/=/g, "").replace(/\+/g, "-").replace(/\//g, "_");
}

function slugify(value: string): string {
  return value.normalize("NFKD").toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "").slice(0, 48);
}

function splitList(value: string | undefined): string[] {
  return (value ?? "").split(",").map((item) => item.trim()).filter(Boolean);
}

function isConfigured(env: Env): boolean {
  return Boolean(
    env.TURNSTILE_SECRET &&
    env.GITHUB_APP_ID &&
    env.GITHUB_INSTALLATION_ID &&
    env.GITHUB_APP_PRIVATE_KEY &&
    env.GITHUB_OWNER &&
    env.GITHUB_REPOSITORY &&
    env.GITHUB_BASE_BRANCH &&
    splitList(env.ALLOWED_HOSTNAMES).length > 0,
  );
}

function corsHeaders(origin: string | null, allowedOrigins: string[]): Headers {
  const headers = new Headers({
    "Access-Control-Allow-Methods": "POST, OPTIONS",
    "Access-Control-Allow-Headers": "Content-Type",
    "Access-Control-Max-Age": "86400",
    "Cache-Control": "no-store",
    Vary: "Origin",
  });
  if (origin && allowedOrigins.includes(origin)) headers.set("Access-Control-Allow-Origin", origin);
  return headers;
}

function json(value: unknown, status: number, headers: Headers): Response {
  headers.set("Content-Type", "application/json; charset=utf-8");
  return new Response(JSON.stringify(value), { status, headers });
}
