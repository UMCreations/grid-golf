# Grid Golf Puzzle - Complete Project Overview

## 1. Project Overview & Core Mechanics
**Game Title:** Grid Golf (Working Title)
**Genre:** Top-down 2D Puzzle / Strategy
**Platform:** PC / Web / Mobile

**Core Concept:** A puzzle-based mini-golf game played on an X*Y grid. The grid tile the ball is resting on dictates the exact distance it will travel. The player chooses from 8 directions (orthogonal and diagonal) to reach the Hole exactly.

**Key Mechanics:**
* **Grid System:** Play takes place on an X*Y grid.
* **Power Count:** Each tile has a number that dictates the exact number of spaces the ball will travel.
* **Movement:** 8-way movement (Up, Down, Left, Right, Diagonals).
* **Win/Loss:** Player wins by landing exactly in the hole. Player loses or must restart if the ball goes out of bounds or gets stuck.

## 2. Technical Setup & Architecture
**Engine:** Unity 2022 LTS or newer (2D Core Template).
**Architecture Pattern:** Clean architecture with dedicated directories for Core, LevelDesign, Entities, UI, Art, Audio, and Scenes.

**Core Systems:**
* **`GridManager`:** Manages the grid of tiles (`Tile[,]`). Spawns tiles based on level data.
* **`Tile`:** Stores grid position, power count, and tile type (Standard, Start, Hole, Hazards).
* **`BallController`:** Handles input, validation, and physical movement (lerping).
* **`ScriptableObjects`:** Used for defining `LevelData` (grid size, tiles, par).
* **`GameManager`:** Handles game loops, win/loss states.

## 3. Level Generation Strategy
The game features an automated level generator that uses **Reverse Pathfinding (Backtracking)** to ensure solvable puzzles.
1. Place Hole on a random valid tile.
2. Jump backwards by distance `d` to an empty tile and assign `d` to it.
3. Repeat to form the "Golden Path" up to the Start.
4. Fill remaining tiles with noise/distractors based on difficulty (Easy, Medium, Hard).

## 4. Adventure Mode (Campaign)
Adventure Mode offers a structured, handcrafted feel using a **linear Saga-style map** with 100 levels divided into 9 segments (Worlds).

**Key Features:**
* **Fixed Seeds:** Each level uses a deterministic seed (`9000 + levelIndex`) so it is the same puzzle every time for every player.
* **Segmented Hazard Introduction:** Hazards (Sand, Ice, Boost) are introduced gradually across worlds.
* **Earn Your Hazard (Minimal Hazards):** Every special tile must have a reason to exist. They are placed either on the Golden Path, on deliberate false trails, or adjacent to the path.
* **Hazards on the Golden Path:** The player must actively interact with hazards to solve the puzzle, turning levels into complex logic challenges where tile effects (power drops, auto-slides, boosts) must be calculated.
* **Save System:** Player progress is tracked with a 1-3 star rating per level based on Par.

## 5. UI System Architecture
The UI follows a **Canvas-based component architecture**. Instead of a monolithic manager, each panel has its own dedicated controller script that listens to events from the `GameManager`.

**Main Canvas Panels:**
* **TopBarHUD (`TopHUDController`):** Displays strokes, level par, restart, and menu buttons.
* **MainMenuPanel (`MainMenuController`):** Game title and Play button.
* **GameWinPanel (`GameWinController`):** Victory text, score/stars, next level button.
* **GameOverPanel (`GameOverController`):** Failure text and retry button.

## 6. Implementation Status
Based on the current project directory, significant implementation has already taken place:
* **Core Systems:** Implemented `GameManager.cs`, `LevelManager.cs`, `AudioManager.cs`, and `SaveManager.cs`.
* **Level Generation (Strategy Pattern):** Fully structured with `LevelGenerator.cs` using different strategies (`ClassicLevelGeneratorStrategy.cs`, `AdvancedLevelGeneratorStrategy.cs`, `AdventureLevelGeneratorStrategy.cs`) and a factory pattern.
* **Adventure Mode Integration:** Implemented `AdventureSegmentConfig.cs` and `AdventureSegmentResolver.cs` for procedural progression.
* **Entities:**
    * **Grid:** `GridManager.cs`, `Tile.cs`, and `GridTheme.cs` are functional.
    * **Player:** `BallController.cs` handles movement logic.
    * **Hazards:** Logic for special tiles is cleanly separated via `ITileEffect.cs`, `TileEffects.cs`, and `TileEffectResolver.cs`.
* **UI Controllers:** The UI architecture is fully built out with specific controllers:
    * `TopHUDController.cs`, `MainMenuController.cs`, `SettingsController.cs`, `TutorialController.cs`
    * Saga Map progression: `SagaMapController.cs`, `LevelSelectionController.cs`, `LevelCardController.cs`, `LevelNodeController.cs`
    * Game States: `GameWinController.cs`, `GameOverController.cs`
    * Helpers: `UIManager.cs`, `UIAnimationHelper.cs`, `FeedbackManager.cs`, `CustomUIToggle.cs`
