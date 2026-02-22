# Level Generation Algorithm Plan (Grid Golf Puzzle)

This document outlines the design and implementation plan for an automated level generator (bot) that creates solvable grid puzzles with varying difficulties (Easy, Medium, Hard).

## 1. Core Concepts & Generation Strategy

Generating a grid-based puzzle where movement is dictated by the tile's own value requires ensuring that at least one valid path exists from the Start (`S`) to the Hole (`H`). 

**Generation Approach: Reverse Pathfinding (Backtracking)**
Instead of placing numbers randomly and hoping a path exists, the bot will generate a level by working *backwards* from the Hole to the Start.
1. Place the Hole (`H`) on a random valid tile.
2. Jump backwards by a chosen distance ($d$) to an empty tile. 
3. Assign the distance $d$ to that empty tile (so when playing forward, the player moves $d$ steps to reach the previous tile).
4. Repeat this backward jumping for $N$ steps to form the "Golden Path". The final landed tile becomes the Start (`S`).
5. Fill the remaining empty tiles with "distractor" numbers that either lead to dead ends or loop around.

## 2. Difficulty Parameters

The difficulty of a level depends on the grid size, the length of the optimal path, the magnitude of the movement numbers, and the complexity of the distractors.

### A. Easy Difficulty
*   **Grid Size:** 5x5
*   **Optimal Path Length:** 3 to 4 strokes (moves).
*   **Max Number (Power):** 2
*   **Distractors:** Mostly random numbers. High chance of placing numbers that push the player out of bounds (immediate dead ends, making the correct path obvious).
*   **Target Player:** Beginners learning the basic mechanics.

### B. Medium Difficulty
*   **Grid Size:** 6x6 or 7x7
*   **Optimal Path Length:** 5 to 7 strokes.
*   **Max Number (Power):** 3 or 4
*   **Distractors:** "False Trails". The algorithm will generate secondary, shorter backward paths from the main path that eventually lead nowhere, forcing the player to plan ahead rather than just taking the first valid move.
*   **Target Player:** Players comfortable with the mechanics looking for a moderate challenge.

### C. Hard Difficulty
*   **Grid Size:** 8x8 or larger.
*   **Optimal Path Length:** 8 to 12 strokes.
*   **Max Number (Power):** 1 to 5 (creating massive jumps that easily overshoot).
*   **Distractors:** "Traps and Loops". High density of active tiles. False paths that run parallel to the real path but end just 1 tile away from the hole. The player must carefully calculate the exact landing spots.
*   **Target Player:** Veterans.

## 3. Algorithm Implementation Steps (C#)

### Phase 1: The Level Generator Class Core
Create a static or singleton `LevelGenerator` class that takes difficulty as an enum parameter (`Difficulty.Easy`, `Difficulty.Medium`, `Difficulty.Hard`) and returns a generated `LevelData` object containing the grid setup, Start, and Hole positions.

### Phase 2: Reverse Walk Algorithm (The Golden Path)
1. Initialize a 2D array of integers to represent the grid (`0` = empty).
2. Randomly select a coordinate for `H` (Hole).
3. Set `CurrentPos` = `H`.
4. Loop `PathLength` times:
    * Choose a random orthogonal direction (Up, Down, Left, Right).
    * Choose a random distance $d$ between `1` and `MaxPower`.
    * Calculate `NextPos` by moving $d$ steps in the chosen direction.
    * Check if `NextPos` is within grid bounds AND is currently empty.
    * If valid, set the grid value at `NextPos` to $d$, then update `CurrentPos` = `NextPos`.
    * If no valid moves exist, backtrack or retry the generation.
5. Set the final `CurrentPos` as `S` (Start).

### Phase 3: Noise & Distractor Generation
Depending on the difficulty, fill the remaining `0` (empty) spaces:
*   **Random Fill:** Just iterate through empty tiles and place a random number `1` to `MaxPower`.
*   **Smart False Trails (Medium/Hard):** Pick a tile on the "Golden Path" and do a short reverse walk from it to create a convincing subset path that looks right but wastes strokes (Par limit must be strict!).

### Phase 4: Integration with GameManager
Update the `GameManager` and `GridManager` so that when a level is requested, it queries the `LevelGenerator` with the player's selected or current skill difficulty, builds the grid dynamically from the returned 2D array, and sets the Par based on the optimal path length (e.g., `Par = PathLength + 1`).

## 4. Next Actions
1.  Review and approve this plan.
2.  Begin Phase 1 & 2 by creating `LevelGenerator.cs` in the `Assets/Core/Scripts/` folder and implementing the reverse pathfinding logic.

---

## 5. Visual Polish & Tweening Ideas

To make the generated levels feel dynamic and premium, we can heavily rely on tweening (using a library like **DOTween**) when a new level is presented to the player.

### A. Dynamic Board Assembly (Cascade Pop-In)
Instead of the grid instantly appearing, we can tween the scale of each tile from `0` to `1` using a bouncy ease (e.g., `Ease.OutBack` in DOTween). 
*   **Staggered Delay:** By adding a slight delay multiplied by the tile's Grid position (`x + y * delay`), the board can beautifully "build" itself in a diagonal wave from bottom-left to top-right.

### B. Number Flips / Reveals
When a level loads, all tiles could initially spawn face down. We can tween their rotation along the Y-axis (`Rotate(0, 90, 0)` then change sprite/text, then `Rotate(0, 0, 0)`) sequentially to create a cool flipping effect that reveals the numbers.

### C. Player Ball "Drop In"
Instead of the ball just spawning, it can start high on the Y-axis (or Z-axis depending on 2D setup) and tween downwards onto the `Start` tile. Upon hitting the tile, we can apply a rapid "Squash and Stretch" tween (`Scale Y` down, `Scale X` up, then bounce back to normal) for a juicy impact.

### D. The "Golden Path" Hint (Easy Mode)
For the beginner difficulty, we can tween a glowing line renderer or particle system that rapidly traces the perfect path from the Start to the Hole, and then smoothly fades out using an alpha tween before the player makes their first move.

### E. Continuous Pulsing
The Hole tile (and perhaps the current tile the player is on) can have a constant, infinite, gentle scaling pulse (e.g., scale to `1.05` and back to `1.0` taking 1 second) to draw the eye and keep the board feeling alive.
