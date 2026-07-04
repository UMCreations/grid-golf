export type TileType =
  | "Standard"
  | "Start"
  | "Hole"
  | "Wall"
  | "Water"
  | "Ice"
  | "Sand"
  | "Boost";

export type Difficulty = "Easy" | "Medium" | "Hard";
export type GameMode = "Classic" | "Adventure";

export interface GridPosition {
  x: number;
  y: number;
}

export interface TileAuthoringDto {
  x: number;
  y: number;
  type: TileType;
  power: number;
}

export interface LevelMetadataDto {
  author?: string;
  tags?: string[];
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface LevelAuthoringDto {
  schemaVersion: 1;
  id: string;
  name: string;
  mode: GameMode;
  difficulty: Difficulty;
  width: number;
  height: number;
  startPosition: GridPosition;
  holePosition: GridPosition;
  levelPar: number;
  tiles: TileAuthoringDto[];
  metadata?: LevelMetadataDto;
}

export interface ValidationResult {
  errors: string[];
  warnings: string[];
}
