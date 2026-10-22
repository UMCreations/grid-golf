import { useEffect, useMemo, useRef, useState, type SetStateAction } from "react";
import {
  createEmptyLevel,
  getTile,
  normalizeLevelGrid,
  normalizeSpecialTiles,
  resizeLevel,
  solveLevel,
  TILE_TYPES,
  validateLevel,
} from "./levelData";
import type { Difficulty, GameMode, LevelAuthoringDto, TileType, ValidationResult } from "./types";

type PaintMode = "tile" | "start" | "hole";
type EditorValidation = ValidationResult & { solvable?: boolean; shortestPathStrokes?: number };
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
  const [paintType, setPaintType] = useState<TileType>("Standard");
  const [paintPower, setPaintPower] = useState(1);
  const [paintMode, setPaintMode] = useState<PaintMode>("tile");
  const [boardScale, setBoardScale] = useState(1);
  const [turnstileToken, setTurnstileToken] = useState("");
  const [turnstileError, setTurnstileError] = useState("");
  const [isSubmittingLevel, setIsSubmittingLevel] = useState(false);
  const [submissionUrl, setSubmissionUrl] = useState("");
  const gridShellRef = useRef<HTMLDivElement>(null);
  const turnstileContainerRef = useRef<HTMLDivElement>(null);
  const turnstileWidgetRef = useRef<string | null>(null);
  const [gridShellWidth, setGridShellWidth] = useState(0);
  const submissionEndpoint = import.meta.env.VITE_LEVEL_SUBMISSION_URL?.trim() ?? "";
  const turnstileSiteKey = import.meta.env.VITE_TURNSTILE_SITE_KEY?.trim() ?? "";

  const selectedTile = useMemo(() => getTile(level, level.startPosition), [level]);
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
  }

  function handleValidate() {
    const result: EditorValidation = validateLevel(level);
    if (result.errors.length === 0) {
      const solution = solveLevel(level);
      result.solvable = solution.isSolvable;
      if (solution.isSolvable) {
        result.shortestPathStrokes = solution.shortestPathStrokes;
      } else {
        result.errors.push("No playable route from start to hole.");
      }
    }
    setValidation(result);
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
                .map((tile) => (
                  <button
                    key={`${tile.x}-${tile.y}`}
                    className={`tile tile-${tile.type.toLowerCase()}${tileSize < 56 ? " tile-compact" : ""}`}
                    aria-label={`${tile.type} tile, power ${tile.power}, at ${tile.x}, ${tile.y}`}
                    onClick={() => updateTile(tile.x, tile.y)}
                    style={{ width: `${tileSize}px`, height: `${tileSize}px` }}
                  >
                    <span className="tile-type">{tile.type.slice(0, 1)}</span>
                    <strong>{tile.power}</strong>
                    <small>
                      {tile.x},{tile.y}
                    </small>
                  </button>
                ))}
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
