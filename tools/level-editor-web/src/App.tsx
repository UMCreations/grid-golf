import { useMemo, useState } from "react";
import {
  createEmptyLevel,
  getTile,
  normalizeSpecialTiles,
  resizeLevel,
  TILE_TYPES,
  validateLevel,
} from "./levelData";
import type { Difficulty, GameMode, LevelAuthoringDto, TileType } from "./types";

type PaintMode = "tile" | "start" | "hole";

function App() {
  const [level, setLevel] = useState<LevelAuthoringDto>(() => createEmptyLevel());
  const [paintType, setPaintType] = useState<TileType>("Standard");
  const [paintPower, setPaintPower] = useState(1);
  const [paintMode, setPaintMode] = useState<PaintMode>("tile");
  const [boardScale, setBoardScale] = useState(1);

  const validation = useMemo(() => validateLevel(level), [level]);
  const selectedTile = useMemo(() => getTile(level, level.startPosition), [level]);

  function updateTile(x: number, y: number) {
    setLevel((current) => {
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

  function handleImport(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file) return;

    void file.text().then((text) => {
      const parsed = JSON.parse(text) as LevelAuthoringDto;
      setLevel(normalizeSpecialTiles(parsed));
    });
  }

  function handleExport() {
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
    URL.revokeObjectURL(url);
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
        </div>
        <div className="hero-actions">
          <button className="primary" onClick={() => setLevel(createEmptyLevel(level.width, level.height))}>
            New Level
          </button>
          <button onClick={handleExport}>Export JSON</button>
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
              onChange={(event) => setLevel({ ...level, id: event.target.value })}
            />
          </label>
          <label>
            <span>Name</span>
            <input
              value={level.name}
              onChange={(event) => setLevel({ ...level, name: event.target.value })}
            />
          </label>
          <div className="pair">
            <label>
              <span>Mode</span>
              <select
                value={level.mode}
                onChange={(event) => setLevel({ ...level, mode: event.target.value as GameMode })}
              >
                <option value="Classic">Classic</option>
                <option value="Adventure">Adventure</option>
              </select>
            </label>
            <label>
              <span>Difficulty</span>
              <select
                value={level.difficulty}
                onChange={(event) => setLevel({ ...level, difficulty: event.target.value as Difficulty })}
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
                  setLevel((current) => resizeLevel(current, width, current.height));
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
                  setLevel((current) => resizeLevel(current, current.width, height));
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
              onChange={(event) => setLevel({ ...level, levelPar: Math.max(1, Number(event.target.value) || 1) })}
            />
          </label>
          <label>
            <span>Author</span>
            <input
              value={level.metadata?.author ?? ""}
              onChange={(event) =>
                setLevel({
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
                setLevel({
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
                setLevel({
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

          <div className="grid-shell">
            <div
              className="grid"
              style={{
                gridTemplateColumns: `repeat(${level.width}, minmax(0, ${72 * boardScale}px))`,
                width: "fit-content",
              }}
            >
              {[...level.tiles]
                .sort((a, b) => (b.y - a.y) || (a.x - b.x))
                .map((tile) => (
                  <button
                    key={`${tile.x}-${tile.y}`}
                    className={`tile tile-${tile.type.toLowerCase()}`}
                    onClick={() => updateTile(tile.x, tile.y)}
                    style={{ width: `${72 * boardScale}px` }}
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
          <h2>Validation</h2>
          <div className="status-strip">
            <span className={validation.errors.length === 0 ? "status-good" : "status-bad"}>
              {validation.errors.length === 0 ? "Valid" : `${validation.errors.length} errors`}
            </span>
            <span>{validation.warnings.length} warnings</span>
          </div>

          <div className="summary-card">
            <h3>Special Tiles</h3>
            <p>Start: {level.startPosition.x},{level.startPosition.y}</p>
            <p>Hole: {level.holePosition.x},{level.holePosition.y}</p>
            <p>Start Power: {selectedTile.power}</p>
          </div>

          <div className="messages">
            <h3>Errors</h3>
            {validation.errors.length === 0 ? (
              <p className="quiet">No errors.</p>
            ) : (
              <ul>
                {validation.errors.map((error) => (
                  <li key={error}>{error}</li>
                ))}
              </ul>
            )}

            <h3>Warnings</h3>
            {validation.warnings.length === 0 ? (
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
