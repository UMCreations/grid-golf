import Ajv2020 from "ajv/dist/2020";
import levelSchema from "../../../level-authoring/schemas/level.schema.json";
import type {
  GridPosition,
  LevelAuthoringDto,
  TileAuthoringDto,
  TileType,
  ValidationResult,
} from "./types";

const schemaValidator = new Ajv2020({ allErrors: true }).compile(levelSchema);

export const TILE_TYPES: TileType[] = [
  "Standard",
  "Start",
  "Hole",
  "Wall",
  "Water",
  "Ice",
  "Sand",
  "Boost",
];

export function createEmptyLevel(width = 5, height = 5): LevelAuthoringDto {
  const tiles: TileAuthoringDto[] = [];

  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      tiles.push({
        x,
        y,
        type: "Standard",
        power: 1,
      });
    }
  }

  tiles[0].type = "Start";
  tiles[width * height - 1].type = "Hole";
  tiles[width * height - 1].power = 0;

  return {
    schemaVersion: 1,
    id: "adventure-001",
    name: "New Level",
    mode: "Adventure",
    difficulty: "Easy",
    width,
    height,
    startPosition: { x: 0, y: 0 },
    holePosition: { x: width - 1, y: height - 1 },
    levelPar: 5,
    tiles,
    metadata: {
      author: "",
      tags: [],
      notes: "",
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
  };
}

export function getTile(level: LevelAuthoringDto, position: GridPosition): TileAuthoringDto {
  const tile = level.tiles.find((entry) => entry.x === position.x && entry.y === position.y);
  if (!tile) {
    throw new Error(`Tile not found at (${position.x}, ${position.y})`);
  }
  return tile;
}

export function resizeLevel(level: LevelAuthoringDto, width: number, height: number): LevelAuthoringDto {
  const tiles: TileAuthoringDto[] = [];

  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const existing = level.tiles.find((entry) => entry.x === x && entry.y === y);
      tiles.push(
        existing ?? {
          x,
          y,
          type: "Standard",
          power: 1,
        },
      );
    }
  }

  const nextStart = clampPosition(level.startPosition, width, height);
  const nextHole = clampPosition(level.holePosition, width, height);

  const normalized = normalizeSpecialTiles({
    ...level,
    width,
    height,
    startPosition: nextStart,
    holePosition: nextHole,
    tiles,
  });

  return normalized;
}

export function normalizeLevelGrid(level: LevelAuthoringDto): LevelAuthoringDto {
  const tilesByPosition = new Map(level.tiles.map((tile) => [`${tile.x}:${tile.y}`, tile]));
  const tiles: TileAuthoringDto[] = [];

  for (let y = 0; y < level.height; y += 1) {
    for (let x = 0; x < level.width; x += 1) {
      tiles.push(
        tilesByPosition.get(`${x}:${y}`) ?? {
          x,
          y,
          type: "Standard",
          power: 1,
        },
      );
    }
  }

  return normalizeSpecialTiles({ ...level, tiles });
}

export function normalizeSpecialTiles(level: LevelAuthoringDto): LevelAuthoringDto {
  const tiles: TileAuthoringDto[] = level.tiles.map((tile) => {
    if (tile.x === level.startPosition.x && tile.y === level.startPosition.y) {
      return { ...tile, type: "Start", power: Math.max(1, tile.power) };
    }

    if (tile.x === level.holePosition.x && tile.y === level.holePosition.y) {
      return { ...tile, type: "Hole", power: 0 };
    }

    if (tile.type === "Start" || tile.type === "Hole") {
      return { ...tile, type: "Standard", power: Math.max(1, tile.power) };
    }

    return tile;
  });

  return { ...level, tiles };
}

export function validateLevel(value: unknown): ValidationResult {
  const errors: string[] = [];
  const warnings: string[] = [];

  if (!schemaValidator(value)) {
    return {
      errors: (schemaValidator.errors ?? []).map(
        ({ instancePath, message }) => `${instancePath || "Level"} ${message ?? "is invalid"}.`,
      ),
      warnings,
    };
  }

  const level = value as unknown as LevelAuthoringDto;
  const seen = new Set<string>();
  if (!level.id.trim()) errors.push("Level id is required.");
  if (!level.name.trim()) errors.push("Level name is required.");
  if (!isWithin(level.startPosition, level.width, level.height)) errors.push("Start position is out of bounds.");
  if (!isWithin(level.holePosition, level.width, level.height)) errors.push("Hole position is out of bounds.");
  if (level.startPosition.x === level.holePosition.x && level.startPosition.y === level.holePosition.y) {
    errors.push("Start and hole positions must be different.");
  }

  for (const tile of level.tiles) {
    const key = `${tile.x}:${tile.y}`;
    if (seen.has(key)) errors.push(`Duplicate tile at (${tile.x}, ${tile.y}).`);
    seen.add(key);

    if (!isWithin(tile, level.width, level.height)) errors.push(`Tile (${tile.x}, ${tile.y}) is out of bounds.`);
    if (tile.type === "Hole" && tile.power !== 0) {
      errors.push(`Hole tile at (${tile.x}, ${tile.y}) must have power 0.`);
    }

    if (tile.type === "Start" && (tile.x !== level.startPosition.x || tile.y !== level.startPosition.y)) {
      errors.push(`Start tile at (${tile.x}, ${tile.y}) does not match startPosition.`);
    }

    if (tile.type === "Hole" && (tile.x !== level.holePosition.x || tile.y !== level.holePosition.y)) {
      errors.push(`Hole tile at (${tile.x}, ${tile.y}) does not match holePosition.`);
    }
  }

  if (level.tiles.length < level.width * level.height) {
    warnings.push("Missing cells will be filled as Standard tiles with power 1.");
  }

  return { errors, warnings };
}

export function solveLevel(level: LevelAuthoringDto): { isSolvable: boolean; shortestPathStrokes: number } {
  // shortcut: mirrors PuzzleSolver.cs; keep transitions in sync when game movement rules change.
  const directions = [
    [0, 1], [0, -1], [-1, 0], [1, 0],
    [1, 1], [1, -1], [-1, 1], [-1, -1],
  ] as const;
  const tiles = new Map(level.tiles.map((tile) => [`${tile.x}:${tile.y}`, tile]));
  const tileAt = (x: number, y: number) => {
    if (x === level.holePosition.x && y === level.holePosition.y) {
      return { type: "Hole" as TileType, power: 0 };
    }
    if (x === level.startPosition.x && y === level.startPosition.y) {
      return { type: "Start" as TileType, power: tiles.get(`${x}:${y}`)?.power ?? 1 };
    }
    return tiles.get(`${x}:${y}`) ?? { type: "Standard" as TileType, power: 1 };
  };
  const inBounds = (x: number, y: number) => x >= 0 && x < level.width && y >= 0 && y < level.height;
  const queue = [{ x: level.startPosition.x, y: level.startPosition.y, modifier: 0, strokes: 0 }];
  const seen = new Set([`${level.startPosition.x}:${level.startPosition.y}:0`]);

  for (let index = 0; index < queue.length; index += 1) {
    const current = queue[index];
    if (current.x === level.holePosition.x && current.y === level.holePosition.y) {
      return { isSolvable: true, shortestPathStrokes: current.strokes };
    }

    const currentTile = tileAt(current.x, current.y);
    if (currentTile.power <= 0) continue;
    const power = Math.max(1, currentTile.power + current.modifier);

    for (const [dx, dy] of directions) {
      const nextX = current.x + dx * power;
      const nextY = current.y + dy * power;
      if (!inBounds(nextX, nextY)) continue;

      let finalX = nextX;
      let finalY = nextY;
      let nextModifier = 0;
      let nextTile = tileAt(nextX, nextY);

      if (level.mode === "Adventure" && nextTile.type === "Ice") {
        let safety = 100;
        while (inBounds(finalX, finalY) && nextTile.type === "Ice" && safety-- > 0) {
          finalX += dx * nextTile.power;
          finalY += dy * nextTile.power;
          if (!inBounds(finalX, finalY)) break;
          nextTile = tileAt(finalX, finalY);
        }

        if (!inBounds(finalX, finalY)) continue;
        if (nextTile.type === "Hole") {
          nextModifier = 0;
        } else if (nextTile.type === "Sand") {
          nextModifier = -1;
        } else if (nextTile.type === "Boost") {
          nextModifier = 1;
        }
      } else if (level.mode === "Adventure" && nextTile.type === "Sand") {
        nextModifier = -1;
      } else if (level.mode === "Adventure" && nextTile.type === "Boost") {
        nextModifier = 1;
      }

      const key = `${finalX}:${finalY}:${nextModifier}`;
      if (seen.has(key)) continue;
      seen.add(key);
      queue.push({ x: finalX, y: finalY, modifier: nextModifier, strokes: current.strokes + 1 });
    }
  }

  return { isSolvable: false, shortestPathStrokes: -1 };
}

function clampPosition(position: GridPosition, width: number, height: number): GridPosition {
  return {
    x: Math.min(Math.max(position.x, 0), width - 1),
    y: Math.min(Math.max(position.y, 0), height - 1),
  };
}

function isWithin(position: GridPosition, width: number, height: number): boolean {
  return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
}
