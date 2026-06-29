# PuzzleGolf Init Notes

## Purpose

This file is my working initialization note for the `PuzzleGolf` project. It captures the current structure, runtime flow, major systems, and the highest-value improvement areas so future work can start from a shared understanding instead of re-discovering the project each time.

## Project Summary

- Engine: Unity `2022.3.62f2`
- Game type: 2D top-down puzzle golf on a grid
- Core loop: choose one of 8 directions, move exactly the power value of the current tile, reach the hole within par/stroke constraints
- Current scope is larger than the original MVP docs:
  - Classic mode
  - Adventure mode with special tiles
  - Procedural generation
  - Handcrafted adventure levels
  - Save/progression
  - Tutorial/FTUE
  - Saga-map style progression UI
  - Audio/animation polish
  - Editor tooling

## Top-Level Structure

- `PuzzleGolf/Assets/Core/Scripts`
  - Main game state, progression, level generation, solver, save, audio
- `PuzzleGolf/Assets/Entities/Grid`
  - Grid creation, tile model, theme data
- `PuzzleGolf/Assets/Entities/Player`
  - Ball input, movement, trajectory, animation
- `PuzzleGolf/Assets/UI/Scripts`
  - Main menu, HUD, level select, tutorial, win/lose, settings, saga map
- `PuzzleGolf/Assets/Core/Scripts/Handcrafted`
  - ScriptableObject-based handcrafted level content and editor
- `PuzzleGolf/Assets/Editor`
  - Custom saga map generation/setup tools
- `PuzzleGolf/Assets/Art`, `Audio`, `UI/Prefabs`, `Core/Prefabs`
  - Production assets and prefabs
- `PuzzleGolf/Packages/manifest.json`
  - Unity packages
- Root docs
  - `GDD.md`, `Level_Generation_Plan.md`, `Adventure_Mode_*`, `UI_Architecture_Plan.md`

## Runtime Architecture

### Core flow

1. `LevelManager` loads/stores player profile and selected mode/level.
2. `MainMenuController` or other UI entry points select a mode and trigger gameplay.
3. `GridManager.InitializeGame()` decides whether to:
   - force tutorial,
   - resume a matching save,
   - or generate a fresh level.
4. `LevelGenerator` picks a generation strategy and returns `LevelData`.
5. `GridManager` builds tile GameObjects, fits the camera, and spawns the ball.
6. `BallController` handles keyboard/touch aiming, trajectory preview, and movement.
7. `GameManager` tracks strokes, win/loss, restart, and next-level transitions.
8. `UIManager` swaps visible panels based on gameplay state events.

### Important singleton-style managers

- `GameManager`
  - Owns win/loss/stroke state and gameplay events.
- `LevelManager`
  - Owns progression, profile, difficulty/mode selection, tutorial flags, stars/unlocks.
- `GridManager`
  - Owns active board, camera fitting, ball spawn, save/resume integration.
- `LevelGenerator`
  - Owns generator strategy selection.
- `UIManager`
  - Owns top-level screen/panel switching.
- `AudioManager`
  - Owns sound playback and settings application.

## Major Parts Of The Project

### 1. Gameplay and board simulation

- Key files:
  - `PuzzleGolf/Assets/Entities/Grid/GridManager.cs`
  - `PuzzleGolf/Assets/Entities/Grid/Tile.cs`
  - `PuzzleGolf/Assets/Entities/Player/BallController.cs`
  - `PuzzleGolf/Assets/Core/Scripts/GameManager.cs`
- Responsibility:
  - create the grid
  - represent tile power/type
  - process movement and preview path
  - handle win/loss and restart

### 2. Level generation and solvability

- Key files:
  - `PuzzleGolf/Assets/Core/Scripts/LevelGenerator.cs`
  - `PuzzleGolf/Assets/Core/Scripts/LevelGeneratorFactory.cs`
  - `PuzzleGolf/Assets/Core/Scripts/ClassicLevelGeneratorStrategy.cs`
  - `PuzzleGolf/Assets/Core/Scripts/SmartLevelGeneratorStrategy.cs`
  - `PuzzleGolf/Assets/Core/Scripts/AdventureLevelGeneratorStrategy.cs`
  - `PuzzleGolf/Assets/Core/Scripts/PuzzleSolver.cs`
- Responsibility:
  - deterministic procedural generation
  - difficulty shaping
  - tutorial generation
  - solvability checks
  - special-tile support in adventure mode

### 3. Progression, save, and profile state

- Key files:
  - `PuzzleGolf/Assets/Core/Scripts/LevelManager.cs`
  - `PuzzleGolf/Assets/Core/Scripts/SaveManager.cs`
- Responsibility:
  - unlocked levels
  - stars
  - last-played state
  - tutorial completion
  - separate classic vs adventure progression
  - serialized board resume via `PlayerPrefs`

### 4. UI and player flow

- Key files:
  - `PuzzleGolf/Assets/UI/Scripts/UIManager.cs`
  - `PuzzleGolf/Assets/UI/Scripts/MainMenuController.cs`
  - `TopHUDController.cs`
  - `LevelSelectionController.cs`
  - `TutorialController.cs`
  - `GameWinController.cs`
  - `GameOverController.cs`
  - `SettingsController.cs`
  - `SagaMapController.cs`
- Responsibility:
  - screen navigation
  - menu flow
  - tutorial gating
  - win/lose screens
  - level-selection and saga progression presentation

### 5. Content authoring tools

- Key files:
  - `PuzzleGolf/Assets/Editor/SagaMapEditorTool.cs`
  - `PuzzleGolf/Assets/Core/Scripts/Handcrafted/HandcraftedLevelSO.cs`
  - `PuzzleGolf/Assets/Core/Scripts/Handcrafted/Editor/HandcraftedLevelEditor.cs`
- Responsibility:
  - handcrafted level authoring
  - saga map UI scaffolding
  - editor-side content workflows

### 6. Presentation and polish

- Key files:
  - `PuzzleGolf/Assets/Core/Scripts/AudioManager.cs`
  - `PuzzleGolf/Assets/Entities/Grid/GridTheme.cs`
  - `PuzzleGolf/Assets/UI/Scripts/UIAnimationHelper.cs`
  - DOTween plugin and theme assets
- Responsibility:
  - audio feedback
  - animations
  - theme visuals
  - menu/game feel

## Current Strengths

- The repo already has a real game architecture, not just a prototype.
- Core puzzle rules are encoded in both runtime logic and a solver, which is a strong foundation.
- Adventure mode is separated at the design level from classic mode instead of being mixed into one ruleset.
- There is a reasonable content pipeline for handcrafted levels and saga-map progression.
- Save/resume is already integrated into board generation and scene startup.

## Main Risks / Technical Observations

### Heavy singleton coupling

Most systems call each other directly through `Instance` lookups. This makes the project easy to wire initially, but harder to test, reuse, or reason about as the number of states grows.

### Game logic and presentation are mixed

`GridManager`, `BallController`, and `Tile` combine game rules, animation timing, and visual behavior. This makes gameplay changes riskier because logic and presentation are not cleanly separated.

### Save format is fragile

`SaveManager` and `LevelManager` both depend on `PlayerPrefs` JSON blobs. That is fine for early-stage mobile games, but fragile for schema changes, debugging, and migration.

### Limited evidence of automated tests

The project includes the Unity test package, but there is no visible test suite for solver correctness, generation determinism, progression rules, or save compatibility.

### Documentation drift risk

The root docs still describe an MVP, while the codebase contains larger systems like adventure mode, solver-assisted generation, handcrafted levels, and a saga map. That gap will slow future changes unless documentation is aligned.

## Areas Of Improvement

### High priority

1. Separate game rules from Unity presentation code
   - Move board simulation and move validation into pure C# domain classes.
   - Keep `MonoBehaviour` scripts focused on scene wiring, animation, and input.

2. Add automated tests for the puzzle core
   - Test solver correctness.
   - Test level generation determinism for fixed seeds.
   - Test classic/adventure save-resume behavior.
   - Test progression unlock and star logic.

3. Formalize `LevelData` and save-versioning
   - Add a version field to serialized save/profile payloads.
   - Plan migrations for future changes to tile types or progression state.

4. Reduce singleton dependency
   - Introduce clearer ownership boundaries.
   - At minimum, centralize cross-system communication behind fewer interfaces or services.

### Medium priority

5. Document the real architecture
   - Update the README and/or add architecture docs that reflect the actual shipped systems.
   - Describe classic mode, adventure mode, handcrafted content, and editor workflows.

6. Clean up folder naming and consistency
   - Example: `LeveLData` casing is inconsistent.
   - There are also asset names with spaces and mixed naming styles that will make maintenance harder.

7. Improve scene/bootstrap clarity
   - Add a single startup/bootstrap doc listing required scene objects and prefab references.
   - Make it obvious which managers must exist in the main scene.

8. Strengthen editor tooling safety
   - Editor tools should validate required references more defensively.
   - Generated UI/content tools should be safer to rerun without accidental duplication.

### Lower priority but useful

9. Add telemetry/debug tools for generation
   - Seed display
   - generated path preview
   - solver result visualization
   - difficulty diagnostics

10. Improve mobile readiness
   - verify touch behavior on real aspect ratios
   - confirm safe areas and UI scaling
   - profile GC allocations during movement/trajectory updates

11. Introduce content balancing workflow
   - Define measurable difficulty targets.
   - Add simple batch validation for a range of generated levels.

## Suggested Next Work Order

1. Create a short architecture doc with scene/bootstrap requirements.
2. Add edit-mode tests for `PuzzleSolver`, `LevelData`, and progression rules.
3. Extract board-rule logic from `BallController` and `GridManager`.
4. Add save/profile versioning.
5. Align docs with the actual implemented feature set.

## Quick File Map For Future Work

- Start here for gameplay:
  - `PuzzleGolf/Assets/Core/Scripts/GameManager.cs`
  - `PuzzleGolf/Assets/Entities/Grid/GridManager.cs`
  - `PuzzleGolf/Assets/Entities/Player/BallController.cs`
- Start here for generation:
  - `PuzzleGolf/Assets/Core/Scripts/LevelGenerator.cs`
  - `PuzzleGolf/Assets/Core/Scripts/SmartLevelGeneratorStrategy.cs`
  - `PuzzleGolf/Assets/Core/Scripts/PuzzleSolver.cs`
- Start here for progression/UI:
  - `PuzzleGolf/Assets/Core/Scripts/LevelManager.cs`
  - `PuzzleGolf/Assets/UI/Scripts/UIManager.cs`
  - `PuzzleGolf/Assets/UI/Scripts/MainMenuController.cs`
- Start here for content tools:
  - `PuzzleGolf/Assets/Editor/SagaMapEditorTool.cs`
  - `PuzzleGolf/Assets/Core/Scripts/Handcrafted/`

## My Current Understanding In One Line

`PuzzleGolf` is a Unity puzzle game whose core strength is deterministic grid-puzzle generation plus progression systems, and whose main improvement need is better separation between gameplay logic, persistent state, and scene/UI code.
