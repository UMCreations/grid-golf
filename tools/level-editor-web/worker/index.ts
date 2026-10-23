import { validateLevel, validatePlayableLevel } from "../src/levelData";
import type { LevelAuthoringDto } from "../src/types";

interface Env {
  ALLOWED_ORIGINS: string;
  ALLOWED_HOSTNAMES: string;
  GITHUB_OWNER: string;
  GITHUB_REPOSITORY: string;
  GITHUB_BASE_BRANCH: string;
  GITHUB_APP_ID: string;
  GITHUB_APP_CLIENT_ID: string;
  GITHUB_APP_CLIENT_SECRET: string;
  GITHUB_INSTALLATION_ID: string;
  GITHUB_APP_PRIVATE_KEY: string;
  ADMIN_CALLBACK_URL: string;
  ADMIN_EDITOR_URL: string;
  ADMIN_GITHUB_LOGIN: string;
  ADMIN_SESSION_SECRET: string;
  TURNSTILE_SECRET: string;
}

const GITHUB_API = "https://api.github.com";
const TURNSTILE_VERIFY = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
const TURNSTILE_ACTION = "turnstile-spin-v2";
const MAX_REQUEST_BYTES = 64 * 1024;
// shortcut: caps review and solver work at 900 cells; raise after profiling larger authored boards.
const MAX_LEVEL_BOARD_CELLS = 900;
const ADMIN_SESSION_SECONDS = 10 * 60;
const OAUTH_STATE_COOKIE = "__Host-grid-golf-oauth";

interface GitHubContentEntry {
  name: string;
  path: string;
  type: string;
}

interface GitHubContentFile {
  sha: string;
  content: string;
  encoding: string;
}

class HttpError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}

class GitHubApiError extends Error {
  constructor(public status: number) {
    super(`GitHub API request failed (${status}).`);
  }
}

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

    if (request.method === "GET" && url.pathname === "/admin/auth/start") {
      return startAdminLogin(env);
    }

    if (request.method === "GET" && url.pathname === "/admin/auth/callback") {
      if (!hasAdminOAuthConfig(env)) return new Response("Admin login is not configured.", { status: 503 });
      return finishAdminLogin(request, env);
    }

    if (url.pathname.startsWith("/admin/")) {
      if (!isAdminOrigin(origin, env)) {
        return json({ error: "Admin access is not allowed from this site." }, 403, headers);
      }
      if (!await verifyAdminSession(request, env)) {
        return json({ error: "Admin session expired. Sign in again." }, 401, headers);
      }

      try {
        if (request.method === "GET" && url.pathname === "/admin/pending") {
          const token = await createInstallationToken(env);
          const files = await githubRequest<GitHubContentEntry[]>(
            githubContentsUrl("levels/pending", env, env.GITHUB_BASE_BRANCH),
            { headers: githubHeaders(token) },
          );
          const levels = files
            .filter((file) => file.type === "file" && /^[a-z0-9][a-z0-9-]*\.json$/.test(file.name))
            .map((file) => ({ id: file.name.slice(0, -5) }))
            .sort((a, b) => a.id.localeCompare(b.id));
          return json({ levels }, 200, headers);
        }

        const previewMatch = url.pathname.match(/^\/admin\/pending\/([a-z0-9][a-z0-9-]{0,119})$/);
        if (request.method === "GET" && previewMatch) {
          const token = await createInstallationToken(env);
          const preview = await loadPendingLevel(previewMatch[1], env, githubHeaders(token), env.GITHUB_BASE_BRANCH);
          return json(preview, 200, headers);
        }

        if (request.method === "POST" && url.pathname === "/admin/publish") {
          const contentLength = Number(request.headers.get("Content-Length") ?? 0);
          if (contentLength > 2048) throw new HttpError(413, "The request is too large.");
          const rawBody = await request.text();
          if (new TextEncoder().encode(rawBody).byteLength > 2048) throw new HttpError(413, "The request is too large.");

          let body: { levelId?: unknown };
          try {
            body = JSON.parse(rawBody) as typeof body;
          } catch {
            throw new HttpError(400, "The request must contain valid JSON.");
          }
          if (!body || typeof body !== "object" || typeof body.levelId !== "string" ||
              !/^[a-z0-9][a-z0-9-]{0,119}$/.test(body.levelId)) {
            throw new HttpError(400, "Choose a valid pending level.");
          }

          const result = await createPublishPullRequest(body.levelId, env);
          return json(result, 201, headers);
        }

        return json({ error: "Not found." }, 404, headers);
      } catch (error) {
        if (error instanceof HttpError) return json({ error: error.message }, error.status, headers);
        if (error instanceof GitHubApiError && error.status === 404) {
          return json({ error: "Pending level was not found. Refresh the queue." }, 404, headers);
        }
        console.error("Pending level request failed.", error instanceof Error ? error.message : "Unknown error");
        return json({ error: "The pending level request failed. Please try again." }, 502, headers);
      }
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
      const level = body.level as LevelAuthoringDto;
      if (level.width * level.height > MAX_LEVEL_BOARD_CELLS) {
        return json({ error: `Board exceeds the ${MAX_LEVEL_BOARD_CELLS}-cell limit.` }, 400, headers);
      }
      const playable = validatePlayableLevel(level);
      if (playable.errors.length > 0) {
        return json({ error: "The level has validation errors.", details: playable.errors.slice(0, 8) }, 400, headers);
      }

      const turnstile = await verifyTurnstile(body.turnstileToken, request, env);
      if (!turnstile) {
        return json({ error: "Bot check failed. Please try again." }, 403, headers);
      }

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
        body: "Account-free level submission from the browser editor. Review this level before moving it to `levels/published/`. A catalog-sync pull request will update `catalog.json` after publication.",
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
    {
      method: "POST",
      headers: githubHeaders(jwt),
      body: JSON.stringify({
        repositories: [env.GITHUB_REPOSITORY],
        permissions: { contents: "write", pull_requests: "write" },
      }),
    },
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
  if (!response.ok) throw new GitHubApiError(response.status);
  if (response.status === 204) return undefined as T;
  return await response.json() as T;
}

async function startAdminLogin(env: Env): Promise<Response> {
  if (!hasAdminOAuthConfig(env)) return new Response("Admin login is not configured.", { status: 503 });

  const state = randomToken(32);
  const verifier = randomToken(32);
  const challenge = base64Url(await crypto.subtle.digest("SHA-256", new TextEncoder().encode(verifier)));
  const authorizationUrl = new URL("https://github.com/login/oauth/authorize");
  authorizationUrl.search = new URLSearchParams({
    client_id: env.GITHUB_APP_CLIENT_ID,
    redirect_uri: env.ADMIN_CALLBACK_URL,
    state,
    code_challenge: challenge,
    code_challenge_method: "S256",
    login: env.ADMIN_GITHUB_LOGIN,
    allow_signup: "false",
  }).toString();

  return new Response(null, {
    status: 302,
    headers: {
      Location: authorizationUrl.toString(),
      "Cache-Control": "no-store",
      "Referrer-Policy": "no-referrer",
      "Set-Cookie": `${OAUTH_STATE_COOKIE}=${state}.${verifier}; Path=/; Max-Age=600; HttpOnly; Secure; SameSite=Lax`,
    },
  });
}

async function finishAdminLogin(request: Request, env: Env): Promise<Response> {
  const url = new URL(request.url);
  const cookie = getCookie(request, OAUTH_STATE_COOKIE);
  const [cookieState, verifier] = (cookie ?? "").split(".");
  const state = url.searchParams.get("state") ?? "";
  const code = url.searchParams.get("code") ?? "";

  if (!hasAdminOAuthConfig(env) || !state || !code || !cookieState || !verifier || state !== cookieState) {
    return adminAuthRedirect(env, { admin_error: "login_failed" });
  }

  try {
    const tokenResponse = await fetch("https://github.com/login/oauth/access_token", {
      method: "POST",
      headers: { Accept: "application/json", "Content-Type": "application/x-www-form-urlencoded" },
      body: new URLSearchParams({
        client_id: env.GITHUB_APP_CLIENT_ID,
        client_secret: env.GITHUB_APP_CLIENT_SECRET,
        code,
        redirect_uri: env.ADMIN_CALLBACK_URL,
        code_verifier: verifier,
      }),
    });
    if (!tokenResponse.ok) throw new Error("OAuth code exchange failed.");
    const tokenResult = await tokenResponse.json() as { access_token?: string; error?: string };
    if (!tokenResult.access_token) throw new Error("OAuth response did not contain a token.");

    const userResponse = await fetch(`${GITHUB_API}/user`, { headers: githubHeaders(tokenResult.access_token) });
    if (!userResponse.ok) throw new Error("GitHub identity lookup failed.");
    const user = await userResponse.json() as { login?: string };
    if ((user.login ?? "").toLowerCase() !== env.ADMIN_GITHUB_LOGIN.toLowerCase()) {
      return adminAuthRedirect(env, { admin_error: "not_authorized" });
    }

    const session = await createAdminSession(env, user.login ?? env.ADMIN_GITHUB_LOGIN);
    return adminAuthRedirect(env, { admin_session: session });
  } catch {
    console.error("Admin OAuth callback failed.");
    return adminAuthRedirect(env, { admin_error: "login_failed" });
  }
}

function adminAuthRedirect(env: Env, fragment: Record<string, string>): Response {
  const destination = new URL(env.ADMIN_EDITOR_URL);
  destination.hash = new URLSearchParams(fragment).toString();
  return new Response(null, {
    status: 302,
    headers: {
      Location: destination.toString(),
      "Cache-Control": "no-store",
      "Referrer-Policy": "no-referrer",
      "Set-Cookie": `${OAUTH_STATE_COOKIE}=; Path=/; Max-Age=0; HttpOnly; Secure; SameSite=Lax`,
    },
  });
}

function getCookie(request: Request, name: string): string | null {
  const prefix = `${name}=`;
  const cookie = request.headers.get("Cookie")?.split(";").map((part) => part.trim()).find((part) => part.startsWith(prefix));
  return cookie ? cookie.slice(prefix.length) : null;
}

async function createAdminSession(env: Env, login: string): Promise<string> {
  const payload = base64Url(JSON.stringify({
    sub: login,
    aud: "grid-golf-level-admin",
    exp: Math.floor(Date.now() / 1000) + ADMIN_SESSION_SECONDS,
  }));
  const key = await importSessionKey(env.ADMIN_SESSION_SECRET, ["sign"]);
  const signature = await crypto.subtle.sign("HMAC", key, new TextEncoder().encode(payload));
  return `${payload}.${base64Url(signature)}`;
}

async function verifyAdminSession(request: Request, env: Env): Promise<boolean> {
  if (!env.ADMIN_SESSION_SECRET || env.ADMIN_SESSION_SECRET.length < 32) return false;
  const authorization = request.headers.get("Authorization") ?? "";
  const match = authorization.match(/^Bearer ([A-Za-z0-9_.-]{1,2048})$/);
  if (!match) return false;

  try {
    const [payload, signature, extra] = match[1].split(".");
    if (!payload || !signature || extra !== undefined) return false;
    const key = await importSessionKey(env.ADMIN_SESSION_SECRET, ["verify"]);
    if (!await crypto.subtle.verify("HMAC", key, decodeBase64Url(signature), new TextEncoder().encode(payload))) return false;
    const claims = JSON.parse(new TextDecoder().decode(decodeBase64Url(payload))) as {
      sub?: string;
      aud?: string;
      exp?: number;
    };
    return claims.aud === "grid-golf-level-admin" &&
      claims.sub?.toLowerCase() === env.ADMIN_GITHUB_LOGIN.toLowerCase() &&
      typeof claims.exp === "number" && claims.exp > Math.floor(Date.now() / 1000);
  } catch {
    return false;
  }
}

async function importSessionKey(secret: string, usages: KeyUsage[]): Promise<CryptoKey> {
  return crypto.subtle.importKey("raw", new TextEncoder().encode(secret), { name: "HMAC", hash: "SHA-256" }, false, usages);
}

function isAdminOrigin(origin: string | null, env: Env): boolean {
  if (!origin || !env.ADMIN_EDITOR_URL) return false;
  try {
    return new URL(env.ADMIN_EDITOR_URL).origin === origin;
  } catch {
    return false;
  }
}

function hasAdminOAuthConfig(env: Env): boolean {
  return Boolean(
    env.GITHUB_APP_CLIENT_ID && env.GITHUB_APP_CLIENT_SECRET && env.ADMIN_CALLBACK_URL &&
    env.ADMIN_EDITOR_URL && env.ADMIN_GITHUB_LOGIN && env.ADMIN_SESSION_SECRET?.length >= 32,
  );
}

function randomToken(byteLength: number): string {
  const bytes = new Uint8Array(byteLength);
  crypto.getRandomValues(bytes);
  return base64Url(bytes.buffer);
}

function decodeBase64Url(value: string): Uint8Array {
  const base64 = value.replace(/-/g, "+").replace(/_/g, "/");
  const binary = atob(base64.padEnd(Math.ceil(base64.length / 4) * 4, "="));
  return Uint8Array.from(binary, (character) => character.charCodeAt(0));
}

function githubContentsUrl(path: string, env: Env, ref?: string): string {
  const encodedPath = path.split("/").map(encodeURIComponent).join("/");
  const url = new URL(`${GITHUB_API}/repos/${env.GITHUB_OWNER}/${env.GITHUB_REPOSITORY}/contents/${encodedPath}`);
  if (ref) url.searchParams.set("ref", ref);
  return url.toString();
}

async function readGithubFile(path: string, ref: string, headers: HeadersInit, env: Env): Promise<{ sha: string; text: string }> {
  const file = await githubRequest<GitHubContentFile>(githubContentsUrl(path, env, ref), { headers });
  if (!file.sha || file.encoding !== "base64" || typeof file.content !== "string") {
    throw new HttpError(422, "A level repository file could not be read.");
  }
  return { sha: file.sha, text: decodeBase64Text(file.content) };
}

function decodeBase64Text(value: string): string {
  const binary = atob(value.replace(/\s/g, ""));
  const bytes = Uint8Array.from(binary, (character) => character.charCodeAt(0));
  return new TextDecoder().decode(bytes);
}

async function loadPendingLevel(
  id: string,
  env: Env,
  headers: HeadersInit,
  ref: string,
): Promise<{ level: unknown; validation: { errors: string[]; warnings: string[] }; solvable: boolean | null; shortestPathStrokes: number | null; boardRenderable: boolean }> {
  if (!/^[a-z0-9][a-z0-9-]{0,119}$/.test(id)) throw new HttpError(400, "Choose a valid pending level.");
  const file = await readGithubFile(`levels/pending/${id}.json`, ref, headers, env);
  let level: unknown;
  try {
    level = JSON.parse(file.text);
  } catch {
    throw new HttpError(422, "This level file contains invalid JSON.");
  }

  const validation = validateLevel(level);
  const value = level as Partial<LevelAuthoringDto> | null;
  if (!value || value.id !== id) validation.errors.push("Level ID does not match its pending filename.");
  if (typeof value?.width === "number" && typeof value.height === "number" &&
      value.width > 0 && value.height > 0 && value.width * value.height > MAX_LEVEL_BOARD_CELLS) {
    validation.errors.push(`Board exceeds the ${MAX_LEVEL_BOARD_CELLS}-cell review limit.`);
  }

  const boardRenderable = validation.errors.length === 0;
  let solvable: boolean | null = null;
  let shortestPathStrokes: number | null = null;
  if (validation.errors.length === 0) {
    const playable = validatePlayableLevel(level as LevelAuthoringDto);
    validation.errors.push(...playable.errors);
    solvable = playable.solvable;
    shortestPathStrokes = playable.shortestPathStrokes ?? null;
  }

  return { level, validation, solvable, shortestPathStrokes, boardRenderable };
}

async function createPublishPullRequest(levelId: string, env: Env): Promise<{ levelId: string; pullRequestUrl: string; alreadyOpen: boolean }> {
  const token = await createInstallationToken(env);
  const headers = githubHeaders(token);
  const branch = `publish-${levelId}`;
  const head = `${env.GITHUB_OWNER}:${branch}`;
  const pullsUrl = new URL(`${GITHUB_API}/repos/${env.GITHUB_OWNER}/${env.GITHUB_REPOSITORY}/pulls`);
  pullsUrl.search = new URLSearchParams({ state: "open", head, per_page: "100" }).toString();
  const openPulls = await githubRequest<Array<{ html_url: string }>>(pullsUrl.toString(), { headers });
  if (openPulls.length > 0) return { levelId, pullRequestUrl: openPulls[0].html_url, alreadyOpen: true };

  const mainRef = await githubRequest<{ object: { sha: string } }>(
    `${GITHUB_API}/repos/${env.GITHUB_OWNER}/${env.GITHUB_REPOSITORY}/git/ref/heads/${encodeURIComponent(env.GITHUB_BASE_BRANCH)}`,
    { headers },
  );
  const baseSha = mainRef.object.sha;
  const pendingPath = `levels/pending/${levelId}.json`;
  const pendingFile = await readGithubFile(pendingPath, baseSha, headers, env);
  const preview = await loadPendingLevel(levelId, env, headers, baseSha);
  if (preview.validation.errors.length > 0 || preview.solvable !== true) {
    throw new HttpError(422, `This level cannot be published: ${preview.validation.errors.slice(0, 4).join(" ")}`);
  }
  const level = preview.level as LevelAuthoringDto;
  const publishedPath = `levels/published/${level.mode.toLowerCase()}/${levelId}.json`;
  const targetUrl = githubContentsUrl(publishedPath, env, baseSha);
  const targetResponse = await fetch(targetUrl, { headers });
  if (targetResponse.ok) throw new HttpError(409, "A published file with this path already exists.");
  if (targetResponse.status !== 404) throw new GitHubApiError(targetResponse.status);

  const refUrl = `${GITHUB_API}/repos/${env.GITHUB_OWNER}/${env.GITHUB_REPOSITORY}/git/refs`;
  const branchRefUrl = `${GITHUB_API}/repos/${env.GITHUB_OWNER}/${env.GITHUB_REPOSITORY}/git/ref/heads/${encodeURIComponent(branch)}`;
  const oldBranch = await fetch(branchRefUrl, { headers });
  if (oldBranch.ok) throw new HttpError(409, "A branch with this publish name already exists. Resolve it before retrying.");
  if (oldBranch.status !== 404) throw new GitHubApiError(oldBranch.status);

  await githubRequest(refUrl, {
    method: "POST",
    headers,
    body: JSON.stringify({ ref: `refs/heads/${branch}`, sha: baseSha }),
  });

  try {
    await githubRequest(githubContentsUrl(publishedPath, env), {
      method: "PUT",
      headers,
      body: JSON.stringify({
        message: `Publish community level: ${levelId}`,
        content: encodeBase64(new TextEncoder().encode(pendingFile.text)),
        branch,
      }),
    });
    await githubRequest(githubContentsUrl(pendingPath, env), {
      method: "DELETE",
      headers,
      body: JSON.stringify({ message: `Move level to published: ${levelId}`, sha: pendingFile.sha, branch }),
    });

    const pull = await githubRequest<{ html_url: string }>(
      `${GITHUB_API}/repos/${env.GITHUB_OWNER}/${env.GITHUB_REPOSITORY}/pulls`,
      {
        method: "POST",
        headers,
        body: JSON.stringify({
          title: `Publish community level: ${level.name.replace(/[\r\n]+/g, " ").slice(0, 80)}`,
          head: branch,
          base: env.GITHUB_BASE_BRANCH,
          body: `Promotes \`${levelId}\` from pending review. This PR only moves the level JSON. After it merges, GitHub Actions updates the catalog in a separate pull request.`,
        }),
      },
    );
    return { levelId, pullRequestUrl: pull.html_url, alreadyOpen: false };
  } catch (error) {
    try {
      await githubRequest(branchRefUrl, { method: "DELETE", headers });
    } catch {
      console.warn("Could not clean up incomplete publish branch.");
    }
    throw error;
  }
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
    "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
    "Access-Control-Allow-Headers": "Authorization, Content-Type",
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
