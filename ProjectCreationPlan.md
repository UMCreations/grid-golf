# Project Implementation & Creation Plan: Grid Golf Puzzle

This document details the technical approach and step-by-step plan for developing "Grid Golf Puzzle" in Unity, based on the Game Design Document.

---

## 1. Project Setup
*   **Engine Version:** Unity 2022 LTS or newer (recommended for stability).
*   **Template:** 2D Core Template.
*   **Version Control:** Git initialized with Unity `.gitignore` and `.gitattributes` (LFS) configured.
*   **Build Targets:** Initially target PC/Mac standalone. Ensure UI scales accordingly for future Web/Mobile builds.
*   **Input System:** The new Unity Input System package is recommended for easier adaptation to mobile swipe controls later.

---

## 2. Recommended Directory Structure
A clean architecture is vital. We will organize `Assets/` as follows:

```text
Assets/
├── Core/                   # Main game loops, Managers (GameManager, LevelManager)
│   ├── Scripts/
│   └── Prefabs/
├── LevelDesign/            # ScriptableObjects and Data defining levels
│   └── Levels/
├── Entities/               # Interactive objects
│   ├── Grid/               # GridManager, Tile class, Tile prefabs
│   └── Player/             # BallController, input handling
├── UI/                     # UI scripts, Canvas prefabs, main menus
├── Art/                    # Sprites, Textures, Materials
│   └── Sprites/
├── Audio/                  # SFX and Music
└── Scenes/                 # MainMenu, GameLevel, TestScene
```

---

## 3. Core Systems & Architecture

### A. The Grid System (`GridManager`)
*   **Responsibility:** Spawns, tracks, and manages the grid of tiles.
*   **Data Structure:** A 2D array `Tile[,] gridArray` to store logical references to every tile by their `(x, y)` coordinate.
*   **Spawning:** Reads level data and instantiates Tile prefabs at specific world coordinates (e.g., `x * tileSize, y * tileSize`).

### B. The Tile Object (`Tile`)
*   **Responsibility:** Stores data about a specific cell.
*   **Properties:**
    *   `Vector2Int gridPosition`: Its `(x, y)` index in the grid.
    *   `int powerCount`: The exact movement value of this tile.
    *   `TileType type`: Enum (Standard, Start, Hole, Wall, Water, etc.).
*   **Visuals:** Handles updating its sprite/text based on its `powerCount` and `type`.

### C. The Player/Ball (`BallController`)
*   **Responsibility:** Handles input aiming, movement validation, and physical movement.
*   **Properties:**
    *   `Vector2Int currentGridPosition`: Where the ball currently is.
    *   `bool isMoving`: To prevent input while animating.
*   **Movement Logic:** 
    1. Reads `powerCount` of the tile at `currentGridPosition`.
    2. Calculates target grid position: `currentGridPosition + (directionVector * powerCount)`.
    3. Checks if target is out of bounds or obstructed.
    4. If valid, lerps world position to the center of the target tile.

### D. Level Data Management (`ScriptableObjects`)
*   Use `ScriptableObject` to define levels outside of scenes.
*   A `LevelData` SO would contain:
    *   Grid Width & Height.
    *   An array/list of specialized Tile definitions (to map power counts, start positions, and hole positions to grid coordinates).
    *   Par score.

---

## 4. Step-by-Step Action Plan (Phase 1: MVP)

**Step 1: Foundational Grid Generation**
1.  Create the `Tile` prefab (a square sprite + a Canvas with a TextMeshPro text element for the number).
2.  Write `Tile.cs` to hold coordinates and power values.
3.  Write `GridManager.cs` to generate an $X \times Y$ grid of tiles and store them in a 2D array.

**Step 2: Start & Hole Tiles**
1.  Extend the `GridManager` to allow coloring or designating specific grid coordinates as the "Start" (S) and "Hole" (H).

**Step 3: The Ball & Input**
1.  Create the `Ball` prefab (a white circle sprite).
2.  Write `BallController.cs`. Snap the ball to the "Start" tile's world position on `Start()`.
3.  Implement basic keyboard input (Numpad or WASD) to register an 8-way directional vector (e.g., `Vector2Int(1, 1)` for Up-Right).

**Step 4: Movement Math & Logic**
1.  In `BallController.cs`, when input is received:
    *   Get the `Tile` underneath the ball via `GridManager`.
    *   Determine the destination coordinate.
    *   Validate to ensure it doesn't fall off the grid.
    *   Update `currentGridPosition` and immediately teleport the ball to the new Tile for now (we'll add smooth lerping right after).

**Step 5: Win / Loss States & Game Loop**
1.  Write `GameManager.cs`.
2.  When the ball finishes returning its move, check if `currentGridPosition == GridManager.HolePosition`. If yes -> **Win**.
3.  If a player attempts an invalid move (off-grid), either ignore the input, or count it as a **Loss** based on design preference.
4.  Implement a "Restart" button that resets the ball to the Start tile.

**Step 6: MVP Polish**
1.  Add `Vector3.Lerp` coroutine to smoothly move the ball from Tile A to Tile B instead of teleporting.
2.  Implement a stroke counter UI.

---

## 5. Next Steps
Once the Phase 1 MVP is fully functional (you can load a hardcoded grid, move around based on tile power, and fall into the hole), we will move to **Phase 2**, which entails building a custom Editor script or Level Editor to easily design multiple levels without hardcoding values.
