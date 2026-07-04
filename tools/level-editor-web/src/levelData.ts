import type {
  GridPosition,
  LevelAuthoringDto,
  TileAuthoringDto,
  TileType,
  ValidationResult,
} from "./types";

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

export function validateLevel(level: LevelAuthoringDto): ValidationResult {
  const errors: string[] = [];
  const warnings: string[] = [];
  const seen = new Set<string>();

  if (level.schemaVersion !== 1) {
    errors.push(`schemaVersion must be 1, got ${level.schemaVersion}.`);
  }

  if (!level.id.trim()) errors.push("Level id is required.");
  if (!level.name.trim()) errors.push("Level name is required.");
  if (level.width < 1 || level.height < 1) errors.push("Width and height must be at least 1.");
  if (level.levelPar < 1) errors.push("Par must be at least 1.");
  if (!isWithin(level.startPosition, level.width, level.height)) errors.push("Start position is out of bounds.");
  if (!isWithin(level.holePosition, level.width, level.height)) errors.push("Hole position is out of bounds.");

  let startCount = 0;
  let holeCount = 0;

  for (const tile of level.tiles) {
    const key = `${tile.x}:${tile.y}`;
    if (seen.has(key)) errors.push(`Duplicate tile at (${tile.x}, ${tile.y}).`);
    seen.add(key);

    if (!isWithin(tile, level.width, level.height)) errors.push(`Tile (${tile.x}, ${tile.y}) is out of bounds.`);
    if (tile.power < 0) errors.push(`Tile (${tile.x}, ${tile.y}) has negative power.`);

    if (tile.type === "Start") startCount += 1;
    if (tile.type === "Hole") holeCount += 1;

    if (tile.type === "Hole" && tile.power !== 0) {
      errors.push(`Hole tile at (${tile.x}, ${tile.y}) must have power 0.`);
    }
  }

  if (level.tiles.length !== level.width * level.height) {
    warnings.push("Tile count does not exactly match width * height. Missing cells should be normalized.");
  }

  if (startCount !== 1) errors.push(`Expected exactly 1 Start tile, found ${startCount}.`);
  if (holeCount !== 1) errors.push(`Expected exactly 1 Hole tile, found ${holeCount}.`);

  const startTile = getTile(level, level.startPosition);
  if (startTile.type !== "Start") errors.push("startPosition does not map to a Start tile.");

  const holeTile = getTile(level, level.holePosition);
  if (holeTile.type !== "Hole") errors.push("holePosition does not map to a Hole tile.");

  return { errors, warnings };
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
