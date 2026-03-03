# Adventure Mode — Development & Implementation Plan

> **Status:** Planning Phase — Ready for implementation.
> **Author:** Umer / Meharaj
> **Date:** 2026-03-03
> **Based on:** `Adventure_Mode_Plan.md`

This document breaks down the execution of the Adventure Mode Redesign into actionable phases. It is designed to be implemented sequentially, ensuring that base systems are stable before introducing complex pathing logic.

---

## Phase 1: Core Configuration & Save Data
**Objective:** Define the data structures that control segment progression and track player state.

1. **Create `AdventureSegmentConfig`**
   - Create a C# class or `ScriptableObject` that holds parameters for a segment:
     - `int startLevel`, `int endLevel`
     - `string themeName`
     - `int minGridSize`, `int maxGridSize`
     - `int minPathLength`, `int maxPathLength`
     - `int maxPower`
     - `int maxHazardsOnPath`, `int maxHazardsInNoise`
     - `List<TileType> allowedHazards`
2. **Create `AdventureSegmentResolver`**
   - Create a static helper class with a method `GetConfigForLevel(int levelIndex)` that returns the correct `AdventureSegmentConfig` based on the 1-100 table setup.
3. **Setup Save Data Structure**
   - Create functions to load/save player progress (e.g., using `PlayerPrefs` or JSON).
   - Track: `HighestUnlockedLevel`, and `Level_{ID}_Stars` (1, 2, or 3).
   - Ensure Classic Mode saves remain entirely separate.

---

## Phase 2: Algorithm Foundation & Determinism
**Objective:** Update the core generator strategy to use fixed seeds and the segment configurations.

1. **Modify `AdventureLevelGeneratorStrategy.GenerateLevel`**
   - Change the `Random.InitState` logic.
   - Remove `DateTime.Now.Ticks`. Replace with `int seed = 9000 + levelIndex;`
2. **Inject the Segment Config**
   - Remove the old `switch (difficulty)` block inside the generator.
   - Fetch the config utilizing `AdventureSegmentResolver.GetConfigForLevel(levelIndex)`.
   - Extract `width`, `height`, `pathLength`, and `maxPower` dynamically from the config rather than from the generic difficulty enum.
3. **Restructure Method Flow**
   - Ensure the generator clearly executes in order:
     1. Place Hole.
     2. Generate Golden Path (standard tiles only for now).
     3. Generate Noise (Standard tiles only for now).

---

## Phase 3: Minimal Hazard Noise System
**Objective:** Implement the "Earn Your Hazard" logic for noise tiles and establish strict hazard budgets.

1. **Implement Hazard Budget Tracking**
   - Track `noiseHazardsPlaced = 0` during the generation step.
   - Reference `config.maxHazardsInNoise`.
2. **Assign Core Powers to Empty Tiles**
   - Iterate all non-path tiles and assign them a random valid power (`1` to `MaxPower`).
3. **Build False Trails (Highest Priority Noise)**
   - Create a method `GenerateFalseTrails(LevelData level, config)`.
   - Iterate through the Golden Path. Occasionally branch off 1-2 empty valid tiles simulating a path, matching the power needed to reach the false tiles.
   - If the budget allows, assign a hazard (from `allowedHazards`) to the end or middle of these false trails. Increment `noiseHazardsPlaced`.
4. **Path-Adjacent Hazards (Second Priority Noise)**
   - Scan tiles specifically adjacent (Up, Down, Left, Right) to the Golden Path.
   - If `noiseHazardsPlaced < config.maxHazardsInNoise`, roll to assign an allowed hazard here to act as a trap. Increment counter.
5. **Finalize as Standard**
   - Iterate through all remaining tiles. If they do not have a specific hazard assigned yet, force them to `TileType.Standard`.

---

## Phase 4: Hazards ON the Golden Path (Complex)
**Objective:** Inject hazards securely into the required solution path, adjusting tile powers so the level remains solvable.

1. **Hazard Selection in Path Generation**
   - While building the Golden Path backward from the Hole, track `pathHazardsPlaced`.
   - Randomly decide (if budget allows) to assign a hazard to the "retreated" tile `P`.
2. **Implement Power Compensation (Sand & Boost)**
   - **Sand:** If tile `P` is Sand, the engine must look at the tile generated *prior* to `P` in the forward path (which is the *next* jump backward during generation). It must increase that jump's required power by 1.
   - **Boost:** If tile `P` is Boost, the generator must *reduce* the required jump power for the preceding tile by 1.
3. **Implement Auto-Slide Connection (Ice)**
   - If an Ice tile is placed at `P`, the generation backward jump cannot just go anywhere.
   - The backward jump must come from the opposite direction of the forward slide, accounting for the +1 step forced movement from the Ice tile.
   - Validate that the previous path tile lands precisely on `P`, triggering the slide to the next correct tile.

---

## Phase 5: Saga Map UI Integration
**Objective:** Build the player-facing front-end to support the Saga progression.

1. **Create `SagaMapController`**
   - Build a UI Canvas for a scrollable generic map.
   - Instantiate 100 `LevelNode` prefabs dynamically or statically via a ScrollRect.
2. **Implement Node Visual States**
   - Bind `LevelNode` states: Locked (gray), Unlocked (pulsing), Completed (Stars shown).
   - Use Save Data from Phase 1 to populate these states on load.
3. **Hook into GameManager**
   - Clicking an "Unlocked" or "Completed" node should invoke `GameManager` to load Adventure Mode, passing the specific `levelIndex` (1-100).
   - Upon winning a level, calculate Par (Level Par vs `currentStrokes`). Award 1, 2, or 3 stars, save the result, and present the "Next Level" button if the player is ready to proceed.
4. **Visual Polish**
   - Add theme backgrounds to the saga map chunks based on the `AdventureSegmentConfig.themeName`.
   - Ensure DOTween animations are clean for node unlocking and star pop-ups.
