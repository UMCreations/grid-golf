# Level Format

## Purpose

This document defines the first shared JSON format for handcrafted PuzzleGolf levels authored outside Unity.

## File Location

Suggested path:

- `level-authoring/levels/adventure/adventure-001.json`
- `level-authoring/levels/classic/classic-001.json`

## Top-Level Structure

```json
{
  "schemaVersion": 1,
  "id": "adventure-001",
  "name": "First Trap",
  "mode": "Adventure",
  "difficulty": "Easy",
  "width": 5,
  "height": 5,
  "startPosition": { "x": 0, "y": 0 },
  "holePosition": { "x": 4, "y": 4 },
  "levelPar": 5,
  "tiles": [
    { "x": 0, "y": 0, "type": "Start", "power": 2 }
  ],
  "metadata": {
    "author": "Umer",
    "tags": ["tutorial", "intro"],
    "notes": "Introduces sand after a safe first move",
    "createdAt": "2026-07-04T10:00:00Z",
    "updatedAt": "2026-07-04T10:00:00Z"
  }
}
```

## Field Rules

### Header

- `schemaVersion`
  - integer
  - required
  - current value: `1`
- `id`
  - string
  - required
  - should be unique across all level files
- `name`
  - string
  - required
- `mode`
  - `Classic` or `Adventure`
- `difficulty`
  - `Easy`, `Medium`, or `Hard`
- `width`, `height`
  - integers
  - minimum `1`
- `levelPar`
  - integer
  - minimum `1`

### Positions

- `startPosition`
  - required
- `holePosition`
  - required
- both must be inside grid bounds

### Tiles

Each tile entry has:

- `x`
- `y`
- `type`
- `power`

Tile rules:

- all tile coordinates must be unique
- every coordinate inside the grid should be representable after normalization
- `Hole` must have `power = 0`
- `power >= 0`
- `Start` and `Hole` may appear in the tile list for clarity, but importer normalizes them from positions too

### Metadata

Optional but recommended:

- `author`
- `tags`
- `notes`
- `createdAt`
- `updatedAt`

## Tile Types

Current supported tile types:

- `Standard`
- `Start`
- `Hole`
- `Wall`
- `Water`
- `Ice`
- `Sand`
- `Boost`

## Validation Rules

- exactly one start position
- exactly one hole position
- no duplicate tile coordinates
- all positions must be in bounds
- no invalid tile type values
- `Hole` power must be `0`
- if `Start` or `Hole` tiles are present in `tiles`, they must match `startPosition` and `holePosition`

## Unity Import Behavior

- Missing cells are normalized as:
  - `Standard`
  - `power = 1`
- `startPosition` and `holePosition` override tile type at those coordinates
- Imported assets are intended to feed `HandcraftedLevelSO` and `LevelData`
- Recommended imported asset location:
  - `PuzzleGolf/Assets/Core/LeveLData/Imported/`
- The Unity importer now defaults to:
  - `Assets/Core/LeveLData/Imported`

## Versioning

Future schema changes must increment `schemaVersion` and add explicit migration logic in Unity import/export code.
