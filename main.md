# Grid Golf Puzzle - Complete Project Overview

## 1. Project Overview & Core Mechanics
**Game Title:** Grid Golf (Working Title)
**Genre:** Top-down 2D Puzzle / Strategy
**Platform:** PC / Web / Mobile

**Core Concept:** A puzzle-based mini-golf game played on an X*Y grid. The grid tile the ball is resting on dictates the exact distance it will travel. The player chooses from 8 directions (orthogonal and diagonal) to reach the Hole exactly. The game heavily relies on "Exactly One Path" logic to ensure puzzles are tight and strategically demanding.

**Key Mechanics:**
* **Grid System:** Play takes place on an X*Y grid.
* **Power Count:** Each tile has a number (e.g., 1 to 8) that dictates the exact number of spaces the ball will travel in the chosen direction.
* **Movement:** 8-way movement (Up, Down, Left, Right, and Diagonals). The ball lerps across the grid to its destination.
* **Win/Loss Conditions:** 
  * Win: Landing exactly in the Hole tile.
  * Loss: Ball goes out of bounds, lands on a Water hazard tile, runs out of strokes (par limit), or attempts an invalid move (e.g., hitting a Wall).

## 2. Technical Setup & Architecture
**Engine:** Unity 2022 LTS or newer (2D Core Template).
**Architecture Pattern:** Clean architecture with dedicated directories for Core, LevelDesign, Entities, UI, Art, Audio, and Scenes.

**Core Systems:**
* **`GridManager`:** Manages the visual and logical grid of tiles (`Tile[,]`). Handles grid generation and rendering based on generated or handcrafted data.
* **`Tile`:** A data container and visual component storing grid position, power count, and `TileType`.
* **`BallController`:** Handles player input, move validation, tile effect resolution, and physical movement/animation across the grid.
* **`PuzzleSolver`:** A crucial BFS-based utility that mathematically validates level solvability. It ensures that procedurally generated levels have exactly one intended solution and no accidental shortcuts.
* **`GameManager`:** Controls the central game loop, state transitions (Play, Win, Loss), and coordinates other managers.

## 3. Level Generation Systems (The "Smart" Pipeline)
The game uses a sophisticated Strategy Pattern for level generation (`LevelGenerator.cs` + `ILevelGeneratorStrategy`). It has moved away from purely random generation to a highly controlled, AI-driven system.

**Generation Strategies:**
* **Reverse Pathfinding (Golden Path):** The core algorithm for procedural generation. It places the Hole, jumps backward by a valid distance `d`, assigns `d` to that tile, and repeats until it reaches the Start.
* **Smart Level Validation (`SmartLevelGeneratorStrategy`):** After the Golden Path is laid, the generator uses the `PuzzleSolver` to evaluate the board. It iteratively tests noise (non-path) tiles to guarantee that no unintended shortcuts exist, making the puzzle mathematically sound.
* **Strategic Deception (Noise Filling):** Instead of random "noise," the generator explicitly constructs deceptive features:
  * **False Branches:** Believable alternate paths that ultimately dead-end or trap the player.
  * **Logic Loops / Vortexes:** Areas that trap players in infinite loops if they misstep.
  * **High-Power Bias:** Noise tiles heavily skew toward maximum power values (e.g., 60% chance for max power), making accidental steps highly punitive.

## 4. Game Modes
### A. Classic Mode
* Endless procedural puzzles separated into difficulty tiers (Easy, Medium, Hard).
* **Sequential Progression:** Players must complete all levels in a tier to unlock the next one.
* Difficulty scales grid size, par limits, and the complexity of the generated Golden Path.

### B. Adventure Mode (Campaign)
A structured, Saga-style map featuring 100 levels divided into 9 thematic segments (Worlds).
* **Handcrafted + Procedural Blend:** Some levels are meticulously handcrafted via the `HandcraftedLevelEditor`, while others use deterministic procedural generation (fixed seeds like `9000 + levelIndex`) to guarantee consistent experiences across players.
* **Segmented Difficulty (`AdventureSegmentResolver`):** A config-driven system that gradually scales difficulty, maximum power (up to 8), hazard density, and grid size as the player progresses from World 1 to 9.
* **Earn Your Hazard:** Special tiles (Hazards) are introduced gradually. They are strategically placed on the Golden Path or alongside it, forcing players to calculate interactions (power drops, auto-slides, blocks).
* **Save System:** Player progress is tracked with a 1-3 star rating per level based on strokes taken vs Par.

## 5. Tile Types & Hazard Mechanics
Tile interactions are resolved via `TileEffectResolver` and the `ITileEffect` interface, allowing for extensible gameplay mechanics.
* **Standard:** Basic tile with a numeric power count.
* **Start:** Where the ball begins.
* **Hole:** The victory condition tile.
* **Ice (`IceTileEffect`):** Causes an auto-slide, forcing the ball to move one more step in the same direction.
* **Sand (`SandTileEffect`):** Reduces the ball's next shot power by 1 (minimum 1).
* **Boost (`BoostTileEffect`):** Increases the ball's next shot power by 1.
* **Water:** A fatal hazard. Landing here triggers an immediate Game Over.
* **Wall:** An unpassable blockade. The ball cannot land on or path through it.

## 6. UI & Experience Architecture
The UI follows a decoupled, **Canvas-based component architecture**. Instead of a monolithic manager, each panel has its own dedicated controller script that listens to events from the `GameManager` and `LevelManager`.

**Main Canvas Panels & Controllers:**
* **TopBarHUD (`TopHUDController`):** Displays strokes, par, level info, restart, and menu options.
* **Main Menu (`MainMenuController`):** Title screen, mode selection, and persistent settings.
* **Saga Map (`SagaMapController`, `LevelSelectionController`, `LevelNodeController`):** Handles visual progression, unlocking logic, and star displays for Adventure Mode.
* **End Game States (`GameWinController`, `GameOverController`):** Manages victory (score/stars, next level) and failure (retry, specific loss reasons) states.
* **Visual Polish (`GridTheme.cs`):** A ScriptableObject-based theme system that defines color palettes, tile sprites (including numbered standard tiles and specific hazard visuals), and background padding for different worlds or moods.

## 7. Editor Tooling
* **`HandcraftedLevelEditor`:** A custom Unity Editor window that provides a visual grid-painting interface. It allows designers to rapidly paint Start, Hole, Standard (with specific powers), and Hazard tiles, set Par limits, and save the data as `LevelData` ScriptableObjects for seamless integration into the Adventure Mode pipeline.
