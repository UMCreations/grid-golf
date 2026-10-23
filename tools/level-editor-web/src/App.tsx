import { useEffect, useMemo, useRef, useState, type SetStateAction } from "react";
import {
  createEmptyLevel,
  findLevelRoutes,
  getTile,
  normalizeLevelGrid,
  normalizeSpecialTiles,
  resizeLevel,
  solveLevel,
  TILE_TYPES,
  validateLevel,
} from "./levelData";
import type { Difficulty, GameMode, GridPosition, LevelAuthoringDto, TileType, ValidationResult } from "./types";

type PaintMode = "tile" | "start" | "hole";
type EditorValidation = ValidationResult & {
  solvable?: boolean;
  shortestPathStrokes?: number;
  routes?: GridPosition[][];
  routesTruncated?: boolean;
};
type PendingPreview = {
  id: string;
  level: unknown;
  validation: ValidationResult;
  solvable: boolean | null;
  shortestPathStrokes: number | null;
  boardRenderable: boolean;
};
const DRAFT_STORAGE_KEY = "puzzlegolf-level-editor-draft-v1";
const turnstileAction = "turnstile-spin-v2";
let turnstileScriptPromise: Promise<void> | null = null;

function loadTurnstileScript(): Promise<void> {
  if (window.turnstile) return Promise.resolve();
  if (turnstileScriptPromise) return turnstileScriptPromise;

  turnstileScriptPromise = new Promise((resolve, reject) => {
    const script = document.createElement("script");
    script.src = "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit";
    script.async = true;
    script.defer = true;
    script.onload = () => resolve();
    script.onerror = () => reject(new Error("Could not load the bot check."));
    document.head.appendChild(script);
  });

  return turnstileScriptPromise;
}

async function adminRequest<T>(baseUrl: string, session: string, path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("Authorization", `Bearer ${session}`);
  if (init.body) headers.set("Content-Type", "application/json");
  const response = await fetch(`${baseUrl}${path}`, { ...init, headers });
  const result = await response.json() as T & { error?: string };
  if (!response.ok) throw new Error(result.error || "The review request failed.");
  return result;
}

function loadDraft() {
  try {
    const raw = window.localStorage.getItem(DRAFT_STORAGE_KEY);
    if (!raw) {
      return {
        level: createEmptyLevel(),
        hasDraft: false,
        status: "Drafts auto-save in this browser.",
      };
    }

    const parsed: unknown = JSON.parse(raw);
    const validation = validateLevel(parsed);
    if (validation.errors.length > 0) {
      return {
        level: createEmptyLevel(),
        hasDraft: true,
        status: "Saved draft could not be restored. It remains stored until you edit or start a new level.",
      };
    }

    return {
      level: normalizeLevelGrid(parsed as LevelAuthoringDto),
      hasDraft: true,
      status: "Restored draft from this browser.",
    };
  } catch {
    return {
      level: createEmptyLevel(),
      hasDraft: true,
      status: "Could not read the saved draft. It will remain untouched until you edit or start a new level.",
    };
  }
}

function App() {
  const [initialDraft] = useState(loadDraft);
  const [level, setLevel] = useState<LevelAuthoringDto>(initialDraft.level);
  const [hasDraft, setHasDraft] = useState(initialDraft.hasDraft);
  const [shouldPersistDraft, setShouldPersistDraft] = useState(false);
  const [draftStatus, setDraftStatus] = useState(initialDraft.status);
  const [notice, setNotice] = useState("");
  const [validation, setValidation] = useState<EditorValidation | null>(null);
  const [selectedRouteIndex, setSelectedRouteIndex] = useState(0);
  const [paintType, setPaintType] = useState<TileType>("Standard");
  const [paintPower, setPaintPower] = useState(1);
  const [paintMode, setPaintMode] = useState<PaintMode>("tile");
  const [boardScale, setBoardScale] = useState(1);
  const [turnstileToken, setTurnstileToken] = useState("");
  const [turnstileError, setTurnstileError] = useState("");
  const [isSubmittingLevel, setIsSubmittingLevel] = useState(false);
  const [submissionUrl, setSubmissionUrl] = useState("");
  const [adminSession, setAdminSession] = useState("");
  const [showPendingReview, setShowPendingReview] = useState(false);
  const [pendingLevelIds, setPendingLevelIds] = useState<string[]>([]);
  const [pendingPreview, setPendingPreview] = useState<PendingPreview | null>(null);
  const [pendingReviewError, setPendingReviewError] = useState("");
  const [publishPullRequest, setPublishPullRequest] = useState("");
  const [isLoadingPending, setIsLoadingPending] = useState(false);
  const [isPublishingPending, setIsPublishingPending] = useState(false);
  const [reviewRefresh, setReviewRefresh] = useState(0);
  const gridShellRef = useRef<HTMLDivElement>(null);
  const turnstileContainerRef = useRef<HTMLDivElement>(null);
  const turnstileWidgetRef = useRef<string | null>(null);
  const [gridShellWidth, setGridShellWidth] = useState(0);
  const submissionEndpoint = import.meta.env.VITE_LEVEL_SUBMISSION_URL?.trim() ?? "";
  const adminApiBase = submissionEndpoint.replace(/\/submit\/?$/, "");
  const turnstileSiteKey = import.meta.env.VITE_TURNSTILE_SITE_KEY?.trim() ?? "";

  const selectedTile = useMemo(() => getTile(level, level.startPosition), [level]);
  const activeRoute = validation?.routes?.[selectedRouteIndex];
  const routeSteps = useMemo(() => {
    const steps = new Map<string, number[]>();
    activeRoute?.forEach(({ x, y }, index) => {
      const key = `${x}:${y}`;
      steps.set(key, [...(steps.get(key) ?? []), index + 1]);
    });
    return steps;
  }, [activeRoute]);
  const tileSize = Math.max(
    44,
    Math.min(72 * boardScale, ((gridShellWidth - (level.width - 1) * 8) / level.width) * boardScale),
  );
  const noticeIsError = ["Import rejected", "Import failed", "Submission failed", "Fix validation"].some((prefix) =>
    notice.startsWith(prefix),
  );

  useEffect(() => {
    if (!shouldPersistDraft) return;

    try {
      window.localStorage.setItem(DRAFT_STORAGE_KEY, JSON.stringify(level));
      setDraftStatus("Draft auto-saved in this browser.");
    } catch {
      setDraftStatus("Browser storage is unavailable. Export JSON to keep this draft.");
    }
  }, [level, shouldPersistDraft]);

  useEffect(() => {
    const redirect = new URLSearchParams(window.location.hash.slice(1));
    const session = redirect.get("admin_session");
    const error = redirect.get("admin_error");
    if (!session && !error) return;

    window.history.replaceState(null, "", `${window.location.pathname}${window.location.search}`);
    if (session) {
      setAdminSession(session);
      setShowPendingReview(true);
    } else {
      setPendingReviewError(error === "not_authorized"
        ? "This GitHub account is not allowed to review levels."
        : "GitHub sign-in failed. Please try again.");
      setShowPendingReview(true);
    }
  }, []);

  useEffect(() => {
    if (!showPendingReview || !adminSession || !adminApiBase) return;
    let cancelled = false;

    async function loadPendingQueue() {
      setIsLoadingPending(true);
      setPendingReviewError("");
      setPendingPreview(null);
      setPublishPullRequest("");
      try {
        const result = await adminRequest<{ levels: Array<{ id: string }> }>(adminApiBase, adminSession, "/admin/pending");
        if (cancelled) return;
        const ids = result.levels.map(({ id }) => id);
        setPendingLevelIds(ids);
        if (ids.length > 0) {
          const preview = await adminRequest<Omit<PendingPreview, "id">>(
            adminApiBase,
            adminSession,
            `/admin/pending/${encodeURIComponent(ids[0])}`,
          );
          if (!cancelled) setPendingPreview({ ...preview, id: ids[0] });
        }
      } catch (error) {
        if (!cancelled) {
          setPendingReviewError(error instanceof Error ? error.message : "Could not load pending levels.");
          if (error instanceof Error && error.message.includes("session expired")) setAdminSession("");
        }
      } finally {
        if (!cancelled) setIsLoadingPending(false);
      }
    }

    void loadPendingQueue();
    return () => { cancelled = true; };
  }, [adminApiBase, adminSession, reviewRefresh, showPendingReview]);

  useEffect(() => {
    const gridShell = gridShellRef.current;
    if (!gridShell) return;

    const observer = new ResizeObserver(([entry]) => setGridShellWidth(entry.contentRect.width));
    observer.observe(gridShell);
    return () => observer.disconnect();
  }, []);

  useEffect(() => {
    if (!turnstileSiteKey || !turnstileContainerRef.current) return;
    let disposed = false;

    loadTurnstileScript()
      .then(() => {
        if (disposed || !turnstileContainerRef.current || !window.turnstile) return;
        turnstileWidgetRef.current = window.turnstile.render(turnstileContainerRef.current, {
          sitekey: turnstileSiteKey,
          action: turnstileAction,
          callback: (token) => {
            setTurnstileToken(token);
            setTurnstileError("");
          },
          "expired-callback": () => setTurnstileToken(""),
          "error-callback": () => {
            setTurnstileToken("");
            setTurnstileError("Bot check failed to load. Refresh and try again.");
          },
        });
      })
      .catch((error: unknown) => {
        setTurnstileError(error instanceof Error ? error.message : "Could not load the bot check.");
      });

    return () => {
      disposed = true;
      if (turnstileWidgetRef.current && window.turnstile) {
        window.turnstile.remove(turnstileWidgetRef.current);
        turnstileWidgetRef.current = null;
      }
    };
  }, [turnstileSiteKey]);

  function updateLevel(nextLevel: SetStateAction<LevelAuthoringDto>) {
    setLevel(nextLevel);
    setHasDraft(true);
    setShouldPersistDraft(true);
    setValidation(null);
    setSelectedRouteIndex(0);
  }

  function handleValidate() {
    const result: EditorValidation = validateLevel(level);
    if (result.errors.length === 0) {
      const solution = solveLevel(level);
      result.solvable = solution.isSolvable;
      if (solution.isSolvable) {
        result.shortestPathStrokes = solution.shortestPathStrokes;
        const routeResult = findLevelRoutes(level);
        result.routes = routeResult.routes;
        result.routesTruncated = routeResult.truncated;
      } else {
        result.errors.push("No playable route from start to hole.");
      }
    }
    setValidation(result);
    setSelectedRouteIndex(0);
  }

  function updateTile(x: number, y: number) {
    updateLevel((current) => {
      if (paintMode === "start") {
        return normalizeSpecialTiles({
          ...current,
          startPosition: { x, y },
        });
      }

      if (paintMode === "hole") {
        return normalizeSpecialTiles({
          ...current,
          holePosition: { x, y },
        });
      }

      const nextTiles = current.tiles.map((tile) => {
        if (tile.x !== x || tile.y !== y) return tile;

        return {
          ...tile,
          type: paintType,
          power: paintType === "Hole" ? 0 : Math.max(0, paintPower),
        };
      });

      let nextLevel: LevelAuthoringDto = { ...current, tiles: nextTiles };
      if (paintType === "Start") nextLevel = { ...nextLevel, startPosition: { x, y } };
      if (paintType === "Hole") nextLevel = { ...nextLevel, holePosition: { x, y } };
      return normalizeSpecialTiles(nextLevel);
    });
  }

  function handleRandomFill() {
    const cellCount = level.width * level.height;
    if (cellCount < 2) {
      setNotice("Random fill needs at least two cells for separate Start and Hole tiles.");
      return;
    }
    if (hasDraft && !window.confirm("Replace the current tile layout with random powers and positions?")) return;

    const maxPower = Math.max(1, Math.floor(level.levelPar));
    const startIndex = Math.floor(Math.random() * cellCount);
    const holeIndex = (startIndex + 1 + Math.floor(Math.random() * (cellCount - 1))) % cellCount;
    const startPosition = { x: startIndex % level.width, y: Math.floor(startIndex / level.width) };
    const holePosition = { x: holeIndex % level.width, y: Math.floor(holeIndex / level.width) };
    const tiles = Array.from({ length: cellCount }, (_, index) => ({
      x: index % level.width,
      y: Math.floor(index / level.width),
      type: index === holeIndex ? "Hole" as TileType : index === startIndex ? "Start" as TileType : "Standard" as TileType,
      power: index === holeIndex ? 0 : Math.floor(Math.random() * maxPower) + 1,
    }));

    updateLevel({ ...level, tiles, startPosition, holePosition });
    setNotice("Random fill applied. Validate before exporting.");
  }

  async function handleImport(event: React.ChangeEvent<HTMLInputElement>) {
    const input = event.currentTarget;
    const file = input.files?.[0];
    input.value = "";
    if (!file) return;

    try {
      const parsed: unknown = JSON.parse(await file.text());
      const result = validateLevel(parsed);
      if (result.errors.length > 0) {
        const shownErrors = result.errors.slice(0, 5).join(" ");
        const remaining = result.errors.length - 5;
        setNotice(
          `Import rejected: ${shownErrors}${remaining > 0 ? ` And ${remaining} more error(s).` : ""}`,
        );
        return;
      }

      if (hasDraft && !window.confirm("Replace the current draft with this imported level?")) {
        setNotice("Import cancelled. Current draft kept.");
        return;
      }

      updateLevel(normalizeLevelGrid(parsed as LevelAuthoringDto));
      const warning = result.warnings.length > 0 ? ` ${result.warnings.join(" ")}` : "";
      setNotice(`Imported ${file.name}.${warning}`);
    } catch (error) {
      const message = error instanceof Error ? error.message : "The file could not be read.";
      setNotice(`Import failed: ${message}`);
    }
  }

  function handleExport() {
    if (!validation) {
      setNotice("Validate the level before exporting.");
      return;
    }
    if (validation.errors.length > 0) {
      setNotice("Fix validation errors before exporting.");
      return;
    }

    const payload: LevelAuthoringDto = {
      ...level,
      metadata: {
        ...level.metadata,
        updatedAt: new Date().toISOString(),
      },
    };

    const blob = new Blob([JSON.stringify(payload, null, 2)], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = `${payload.id || "level"}.json`;
    anchor.click();
    window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    setNotice(`Exported ${anchor.download}.`);
  }

  async function handleSubmitLevel() {
    if (!submissionEndpoint || !turnstileSiteKey) {
      setNotice("Community submissions are not configured yet.");
      return;
    }
    if (!validation) {
      setNotice("Validate the level before submitting.");
      return;
    }
    if (validation.errors.length > 0 || validation.solvable !== true) {
      setNotice("Fix validation errors before submitting.");
      return;
    }
    if (!turnstileToken) {
      setNotice("Complete the bot check before submitting.");
      return;
    }

    setIsSubmittingLevel(true);
    setSubmissionUrl("");
    try {
      const response = await fetch(submissionEndpoint, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          level: {
            ...level,
            metadata: { ...level.metadata, updatedAt: new Date().toISOString() },
          },
          turnstileToken,
        }),
      });
      const result = await response.json() as {
        error?: string;
        levelId?: string;
        pullRequestUrl?: string;
      };
      if (!response.ok) throw new Error(result.error || "The level could not be submitted.");
      if (!result.pullRequestUrl || !result.levelId) {
        throw new Error("The submission service returned an incomplete response.");
      }

      setSubmissionUrl(result.pullRequestUrl);
      setNotice(`Submitted for review as ${result.levelId}.`);
    } catch (error) {
      const message = error instanceof Error ? error.message : "The level could not be submitted.";
      setNotice(`Submission failed: ${message}`);
    } finally {
      setIsSubmittingLevel(false);
      setTurnstileToken("");
      if (turnstileWidgetRef.current) window.turnstile?.reset(turnstileWidgetRef.current);
    }
  }

  function handleOpenPendingReview() {
    if (!adminApiBase) {
      setNotice("Review service is not configured yet.");
      return;
    }
    if (!adminSession) {
      window.location.assign(`${adminApiBase}/admin/auth/start`);
      return;
    }
    setShowPendingReview(true);
  }

  async function handleSelectPending(id: string) {
    if (!adminSession) return;
    setPendingPreview(null);
    setPendingReviewError("");
    setPublishPullRequest("");
    try {
      const preview = await adminRequest<Omit<PendingPreview, "id">>(
        adminApiBase,
        adminSession,
        `/admin/pending/${encodeURIComponent(id)}`,
      );
      setPendingPreview({ ...preview, id });
    } catch (error) {
      setPendingReviewError(error instanceof Error ? error.message : "Could not load this level.");
      if (error instanceof Error && error.message.includes("session expired")) setAdminSession("");
    }
  }

  async function handlePublishPending() {
    if (!adminSession || !pendingPreview || pendingPreview.validation.errors.length > 0 || pendingPreview.solvable !== true) return;
    if (!window.confirm(`Create a publish PR for “${(pendingPreview.level as LevelAuthoringDto).name}”?`)) return;

    setIsPublishingPending(true);
    setPendingReviewError("");
    try {
      const result = await adminRequest<{ pullRequestUrl: string; alreadyOpen: boolean }>(
        adminApiBase,
        adminSession,
        "/admin/publish",
        { method: "POST", body: JSON.stringify({ levelId: pendingPreview.id }) },
      );
      setPublishPullRequest(result.pullRequestUrl);
      setPendingReviewError(result.alreadyOpen
        ? "A publish PR already exists for this level."
        : "Publish PR created. Merge it to move the level into the published catalog.");
    } catch (error) {
      setPendingReviewError(error instanceof Error ? error.message : "Could not create the publish PR.");
      if (error instanceof Error && error.message.includes("session expired")) setAdminSession("");
    } finally {
      setIsPublishingPending(false);
    }
  }

  function handleNewLevel() {
    if (hasDraft && !window.confirm("Start a new level? The current draft will be replaced.")) return;

    const nextLevel = createEmptyLevel(level.width, level.height);
    nextLevel.id = `${level.mode.toLowerCase()}-${window.crypto.randomUUID()}`;
    nextLevel.mode = level.mode;
    nextLevel.difficulty = level.difficulty;
    setLevel(nextLevel);
    setHasDraft(false);
    setShouldPersistDraft(true);
    setValidation(null);
    setNotice("New level created.");
  }

  const reviewLevel = pendingPreview?.boardRenderable ? pendingPreview.level as LevelAuthoringDto : null;

  return (
    <main className="app-shell">
      <section className="hero">
        <div>
          <p className="eyebrow">PuzzleGolf Team Tool</p>
          <h1>Level Editor</h1>
          <p className="lede">
            Build handcrafted levels in the browser, export JSON, and import into Unity.
          </p>
          <p className="draft-status" role="status" aria-live="polite">{draftStatus}</p>
          {notice && (
            <p className={`draft-notice ${noticeIsError ? "is-error" : ""}`} role="status" aria-live="polite">
              {notice}
            </p>
          )}
        </div>
        <div className="hero-actions">
          <button onClick={handleOpenPendingReview}>
            Review Pending
          </button>
          <button className="primary" onClick={handleNewLevel}>
            New Level
          </button>
          <button onClick={handleExport} disabled={!validation || validation.errors.length > 0}>
            Export JSON
          </button>
          <label className="file-button">
            Import JSON
            <input type="file" accept=".json,application/json" onChange={handleImport} />
          </label>
        </div>
      </section>

      {showPendingReview && (
        <section className="pending-review" aria-labelledby="pending-review-title">
          <div className="review-heading">
            <div>
              <p className="eyebrow">Maintainer tools</p>
              <h2 id="pending-review-title">Pending level review</h2>
              <p>Only submissions merged into <code>levels/pending/</code> on the repository’s main branch appear here.</p>
            </div>
            <div className="review-heading-actions">
              <button onClick={() => setReviewRefresh((value) => value + 1)} disabled={isLoadingPending}>
                {isLoadingPending ? "Refreshing…" : "Refresh"}
              </button>
              <button onClick={() => setShowPendingReview(false)}>Close</button>
            </div>
          </div>

          <div className="review-layout">
            <aside className="pending-queue" aria-label="Pending levels">
              <h3>Queue <span>{pendingLevelIds.length}</span></h3>
              {pendingLevelIds.length === 0 && !isLoadingPending ? (
                <p className="quiet">No pending levels found.</p>
              ) : (
                <div className="pending-level-list">
                  {pendingLevelIds.map((id) => (
                    <button
                      key={id}
                      className={pendingPreview?.id === id ? "selected" : ""}
                      aria-pressed={pendingPreview?.id === id}
                      onClick={() => void handleSelectPending(id)}
                    >
                      {id}
                    </button>
                  ))}
                </div>
              )}
            </aside>

            <div className="review-detail" aria-live="polite">
              {pendingReviewError && (
                <p
                  className={`review-message ${/failed|could not|not allowed|not found|expired|invalid|exists|cannot be published/i.test(pendingReviewError) ? "is-error" : ""}`}
                  role="status"
                >
                  {pendingReviewError}
                </p>
              )}
              {isLoadingPending && <p className="quiet">Loading pending levels…</p>}
              {!isLoadingPending && pendingPreview && reviewLevel && (
                <>
                  <div className="review-level-heading">
                    <div>
                      <p className="eyebrow">{reviewLevel.mode} · {reviewLevel.difficulty} · Par {reviewLevel.levelPar}</p>
                      <h3>{reviewLevel.name}</h3>
                      <p className="quiet">{pendingPreview.id}</p>
                    </div>
                    <span className={pendingPreview.solvable ? "status-good" : "status-bad"}>
                      {pendingPreview.solvable ? "Solvable" : "Needs fixes"}
                    </span>
                  </div>
                  <div className="review-board-shell">
                    <div
                      className="review-grid"
                      style={{ gridTemplateColumns: `repeat(${reviewLevel.width}, minmax(0, 1fr))` }}
                      aria-label={`Preview of ${reviewLevel.name}`}
                    >
                      {[...reviewLevel.tiles]
                        .sort((a, b) => (b.y - a.y) || (a.x - b.x))
                        .map((tile) => (
                          <div key={`${tile.x}-${tile.y}`} className={`tile review-tile tile-${tile.type.toLowerCase()}`}>
                            <span className="tile-type">{tile.type.slice(0, 1)}</span>
                            <strong>{tile.power}</strong>
                            <small>{tile.x},{tile.y}</small>
                          </div>
                        ))}
                    </div>
                  </div>
                  {pendingPreview.validation.errors.length > 0 && (
                    <ul className="review-errors">
                      {pendingPreview.validation.errors.map((error) => <li key={error}>{error}</li>)}
                    </ul>
                  )}
                  <div className="review-summary">
                    <span>{reviewLevel.width} × {reviewLevel.height}</span>
                    {pendingPreview.shortestPathStrokes !== null && (
                      <span>Shortest route: {pendingPreview.shortestPathStrokes} strokes</span>
                    )}
                    <span>{reviewLevel.metadata?.author ? `By ${reviewLevel.metadata.author}` : "Author not provided"}</span>
                  </div>
                  {pendingPreview.validation.warnings.length > 0 && (
                    <ul className="review-warnings">
                      {pendingPreview.validation.warnings.map((warning) => <li key={warning}>{warning}</li>)}
                    </ul>
                  )}
                  <div className="review-actions">
                    <button
                      className="primary"
                      onClick={() => void handlePublishPending()}
                      disabled={!pendingPreview.solvable || isPublishingPending}
                    >
                      {isPublishingPending ? "Creating PR…" : "Move to Published"}
                    </button>
                    {publishPullRequest && (
                      <a href={publishPullRequest} target="_blank" rel="noreferrer">Open publish PR</a>
                    )}
                  </div>
                </>
              )}
              {!isLoadingPending && pendingPreview && !reviewLevel && (
                <div className="review-invalid">
                  <h3>This level needs fixes before publishing</h3>
                  <ul>{pendingPreview.validation.errors.map((error) => <li key={error}>{error}</li>)}</ul>
                </div>
              )}
              {!isLoadingPending && pendingLevelIds.length > 0 && !pendingPreview && !pendingReviewError && (
                <p className="quiet">Select a level to preview its board.</p>
              )}
            </div>
          </div>
        </section>
      )}

      <section className="workspace">
        <aside className="panel">
          <h2>Level</h2>
          <label>
            <span>ID</span>
            <input
              value={level.id}
              onChange={(event) => updateLevel({ ...level, id: event.target.value })}
            />
          </label>
          <label>
            <span>Name</span>
            <input
              value={level.name}
              onChange={(event) => updateLevel({ ...level, name: event.target.value })}
            />
          </label>
          <div className="pair">
            <label>
              <span>Mode</span>
              <select
                value={level.mode}
                onChange={(event) => updateLevel({ ...level, mode: event.target.value as GameMode })}
              >
                <option value="Classic">Classic</option>
                <option value="Adventure">Adventure</option>
              </select>
            </label>
            <label>
              <span>Difficulty</span>
              <select
                value={level.difficulty}
                onChange={(event) => updateLevel({ ...level, difficulty: event.target.value as Difficulty })}
              >
                <option value="Easy">Easy</option>
                <option value="Medium">Medium</option>
                <option value="Hard">Hard</option>
              </select>
            </label>
          </div>
          <div className="pair">
            <label>
              <span>Width</span>
              <input
                type="number"
                min={1}
                value={level.width}
                onChange={(event) => {
                  const width = Math.max(1, Number(event.target.value) || 1);
                  updateLevel((current) => resizeLevel(current, width, current.height));
                }}
              />
            </label>
            <label>
              <span>Height</span>
              <input
                type="number"
                min={1}
                value={level.height}
                onChange={(event) => {
                  const height = Math.max(1, Number(event.target.value) || 1);
                  updateLevel((current) => resizeLevel(current, current.width, height));
                }}
              />
            </label>
          </div>
          <label>
            <span>Par</span>
            <input
              type="number"
              min={1}
              value={level.levelPar}
              onChange={(event) => updateLevel({ ...level, levelPar: Math.max(1, Number(event.target.value) || 1) })}
            />
          </label>
          <label>
            <span>Author</span>
            <input
              value={level.metadata?.author ?? ""}
              onChange={(event) =>
                updateLevel({
                  ...level,
                  metadata: { ...level.metadata, author: event.target.value },
                })
              }
            />
          </label>
          <label>
            <span>Tags (comma separated)</span>
            <input
              value={(level.metadata?.tags ?? []).join(", ")}
              onChange={(event) =>
                updateLevel({
                  ...level,
                  metadata: {
                    ...level.metadata,
                    tags: event.target.value
                      .split(",")
                      .map((tag) => tag.trim())
                      .filter(Boolean),
                  },
                })
              }
            />
          </label>
          <label>
            <span>Notes</span>
            <textarea
              rows={5}
              value={level.metadata?.notes ?? ""}
              onChange={(event) =>
                updateLevel({
                  ...level,
                  metadata: { ...level.metadata, notes: event.target.value },
                })
              }
            />
          </label>
        </aside>

        <section className="editor">
          <div className="toolbar">
            <div className="paint-group">
              <button
                className={paintMode === "tile" ? "active" : ""}
                onClick={() => setPaintMode("tile")}
              >
                Paint Tiles
              </button>
              <button
                className={paintMode === "start" ? "active" : ""}
                onClick={() => setPaintMode("start")}
              >
                Place Start
              </button>
              <button
                className={paintMode === "hole" ? "active" : ""}
                onClick={() => setPaintMode("hole")}
              >
                Place Hole
              </button>
              <button onClick={handleRandomFill}>Random Fill</button>
            </div>

            <div className="paint-settings">
              <label className="scale-control">
                <span>Board Scale</span>
                <input
                  type="range"
                  min={0.55}
                  max={1.6}
                  step={0.05}
                  value={boardScale}
                  onChange={(event) => setBoardScale(Number(event.target.value))}
                />
                <strong>{Math.round(boardScale * 100)}%</strong>
              </label>
              <label>
                <span>Type</span>
                <select
                  value={paintType}
                  onChange={(event) => setPaintType(event.target.value as TileType)}
                  disabled={paintMode !== "tile"}
                >
                  {TILE_TYPES.map((type) => (
                    <option key={type} value={type}>
                      {type}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                <span>Power</span>
                <input
                  type="number"
                  min={0}
                  value={paintPower}
                  onChange={(event) => setPaintPower(Math.max(0, Number(event.target.value) || 0))}
                  disabled={paintMode !== "tile"}
                />
              </label>
            </div>
          </div>

          <div className="grid-shell" ref={gridShellRef}>
            <div
              className="grid"
              style={{
                gridTemplateColumns: `repeat(${level.width}, ${tileSize}px)`,
                gridAutoRows: `${tileSize}px`,
                width: "fit-content",
              }}
            >
              {[...level.tiles]
                .sort((a, b) => (b.y - a.y) || (a.x - b.x))
                .map((tile) => {
                  const steps = routeSteps.get(`${tile.x}:${tile.y}`) ?? [];
                  return (
                    <button
                      key={`${tile.x}-${tile.y}`}
                      className={`tile tile-${tile.type.toLowerCase()}${tileSize < 56 ? " tile-compact" : ""}${steps.length ? " tile-on-route" : ""}`}
                      aria-label={`${tile.type} tile, power ${tile.power}, at ${tile.x}, ${tile.y}`}
                      onClick={() => updateTile(tile.x, tile.y)}
                      style={{ width: `${tileSize}px`, height: `${tileSize}px` }}
                    >
                      {steps.length > 0 && <span className="route-marker" aria-hidden="true">{steps.join("/")}</span>}
                      <span className="tile-type">{tile.type.slice(0, 1)}</span>
                      <strong>{tile.power}</strong>
                      <small>
                        {tile.x},{tile.y}
                      </small>
                    </button>
                  );
                })}
            </div>
          </div>
        </section>

        <aside className="panel">
          <div className="validation-header">
            <h2>Validation</h2>
            <button className="validate-button" onClick={handleValidate}>Validate Level</button>
          </div>
          <div className="status-strip" role="status" aria-live="polite">
            <span className={validation === null ? "" : validation.errors.length === 0 ? "status-good" : "status-bad"}>
              {validation === null
                ? "Not checked"
                : validation.solvable === false
                  ? "Unsolvable"
                  : validation.errors.length === 0
                    ? "Solvable"
                    : `${validation.errors.length} errors`}
            </span>
            <span>{validation?.warnings.length ?? 0} warnings</span>
            {validation?.shortestPathStrokes !== undefined && (
              <span>Shortest route: {validation.shortestPathStrokes} strokes</span>
            )}
          </div>
          {validation?.routes && validation.routes.length > 0 && (
            <div className="route-controls" aria-label="Valid route preview">
              <button
                type="button"
                aria-label="Show previous route"
                onClick={() => setSelectedRouteIndex((index) => Math.max(0, index - 1))}
                disabled={selectedRouteIndex === 0}
              >
                Previous
              </button>
              <span>
                Route {selectedRouteIndex + 1} of {validation.routes.length}
                {activeRoute && ` · ${activeRoute.length - 1} strokes`}
              </span>
              <button
                type="button"
                aria-label="Show next route"
                onClick={() => setSelectedRouteIndex((index) => Math.min(validation.routes!.length - 1, index + 1))}
                disabled={selectedRouteIndex >= validation.routes.length - 1}
              >
                Next
              </button>
              <small>Numbered tiles show move order.</small>
              {validation.routesTruncated && (
                <small>Route search limit reached; there may be more than {validation.routes.length} routes.</small>
              )}
            </div>
          )}

          <section className="community-submit" aria-labelledby="community-submit-title">
            <h3 id="community-submit-title">Community submission</h3>
            <p>Send a validated level for review. Approved levels will become playable in the game.</p>
            {turnstileSiteKey ? (
              <div
                ref={turnstileContainerRef}
                className="turnstile-widget"
                data-action={turnstileAction}
                aria-label="Bot verification"
              />
            ) : (
              <p className="quiet">Submission service is not configured yet.</p>
            )}
            {turnstileError && <p className="submit-error" role="status">{turnstileError}</p>}
            <button
              className="submit-level-button"
              onClick={handleSubmitLevel}
              disabled={
                !submissionEndpoint || !turnstileSiteKey || !validation ||
                validation.errors.length > 0 || validation.solvable !== true ||
                !turnstileToken || isSubmittingLevel
              }
            >
              {isSubmittingLevel ? "Sending…" : "Send for Review"}
            </button>
            {submissionUrl && (
              <a className="submission-link" href={submissionUrl} target="_blank" rel="noreferrer">
                View submission
              </a>
            )}
          </section>

          <div className="summary-card">
            <h3>Special Tiles</h3>
            <p>Start: {level.startPosition.x},{level.startPosition.y}</p>
            <p>Hole: {level.holePosition.x},{level.holePosition.y}</p>
            <p>Start Power: {selectedTile.power}</p>
          </div>

          <div className="messages">
            <h3>Errors</h3>
            {validation === null ? (
              <p className="quiet">Run validation to check structure and playability.</p>
            ) : validation.errors.length === 0 ? (
              <p className="quiet">No errors.</p>
            ) : (
              <ul>
                {validation.errors.map((error) => (
                  <li key={error}>{error}</li>
                ))}
              </ul>
            )}

            <h3>Warnings</h3>
            {validation === null ? (
              <p className="quiet">Not checked.</p>
            ) : validation.warnings.length === 0 ? (
              <p className="quiet">No warnings.</p>
            ) : (
              <ul>
                {validation.warnings.map((warning) => (
                  <li key={warning}>{warning}</li>
                ))}
              </ul>
            )}
          </div>
        </aside>
      </section>
    </main>
  );
}

export default App;
