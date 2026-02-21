# Game Design Document: Grid Golf Puzzle

## 1. Overview
**Game Title:** Grid Golf (Working Title)  
**Genre:** Top-down 2D Puzzle / Strategy  
**Platform:** PC / Web / Mobile (Engine agnostic)  
**Core Concept:** A puzzle-based mini-golf game played on an X*Y grid. Instead of choosing how hard to hit the ball, the grid tile the ball is currently resting on dictates the exact distance the ball will travel. The goal is to reach the hole in the fewest moves possible using 8-way movement (horizontal, vertical, and diagonal).

---

## 2. Core Gameplay Mechanics

### 2.1 The Grid
* The game takes place on an **X by Y grid** (e.g., 8x8, 10x10).
* **Start Point (S):** The specific grid cell where the ball spawns at the start of the level.
* **Hole (H):** The target grid cell. Landing exactly on this cell completes the level.

### 2.2 Movement Logic (The "Power Count" System)
* Every playable grid cell has a **Power Count** (an integer value like 1, 2, 3, 4).
* When the ball is on a grid cell, the player chooses from **8 directions** (Up, Down, Left, Right, Up-Left, Up-Right, Down-Left, Down-Right).
* The ball will travel **exactly** the number of grid spaces equal to the cell's Power Count. 
  * *Orthogonal Example:** If on a "3" tile, it can move 3 tiles straight Up.
  * *Diagonal Example:** If on a "3" tile, it can move 3 tiles diagonally (e.g., 3 tiles Up AND 3 tiles Right, moving perfectly corner-to-corner).
* The ball must land **exactly** in the Hole to win. It cannot overshoot.

### 2.3 Win / Loss Conditions
* **Win:** The ball lands perfectly in the Hole.
* **Loss / Reset:** 
  * The player makes a move that would send the ball outside the boundaries of the grid (Out of Bounds).
  * The player gets stuck in an unwinnable loop (can manually restart the level).

---

## 3. Controls
* **Mouse / Touch (Recommended for 8-way):** Click/Swipe and drag on the ball. An arrow will appear pointing in one of the 8 directions. Release to hit.
* **Keyboard:** 
  * Numpad keys (1-9, excluding 5) map perfectly to 8 directions.
  * Arrow keys or WASD (Pressing W+D together creates an Up-Right diagonal move).
* **UI:** A simple "Restart Level" button (Shortcut: `R` key).

---

## 4. Visual Design & UI (MVP Phase)
* **Camera:** Fixed top-down orthographic view.
* **Art Style:** Minimalist 2D. 
  * Grid lines clearly visible.
  * Ball: White circle.
  * Hole: Black circle with a small flag.
  * Tiles: Colored squares with a number text overlay indicating their "Power Count".
* **UI Overlay:** Current Level Number, Stroke Counter (Moves taken).

---

## 5. Grid Example (Mockup)
*S = Start (Power 2), H = Hole, Numbers = Power Count*

|   |   |   |   |   |
|---|---|---|---|---|
| 1 | 2 | 1 | 3 | H |
| 2 | S | 3 | 1 | 2 |
| 1 | 4 | 2 | 2 | 1 |

*Movement Examples from **S (which is a 2)**:*
* *Move Right:* The ball moves exactly 2 spaces right, landing on the **1**.
* *Move Up-Right:* The ball moves exactly 2 spaces diagonally up-and-right, landing on the **H (Hole - WIN!)**.

---

## 6. Development Roadmap (Improving Moving Forward)

### Phase 1: Minimum Viable Product (MVP)
- [ ] Render a basic X*y grid.
- [ ] Implement the Start point and the Hole.
- [ ] Assign static "Power Count" numbers to grid cells.
- [ ] Implement 8-way movement logic (Orthogonal and Diagonal grid math).
- [ ] Add Win state (landing in hole) and boundary constraints (can't move off grid).

### Phase 2: Level Design & Progression
- [ ] Create 5 to 10 playable puzzle levels.
- [ ] Implement a "Par" system (target number of strokes to win).
- [ ] Implement level transitions (moving from Level 1 to Level 2 upon win).

### Phase 3: Obstacles & Advanced Tiles
- [ ] **Wall/Blocker Tiles:** Tiles the ball cannot pass through (blocks diagonal paths as well).
- [ ] **Water Hazards:** If the ball lands here, it's a penalty/reset.
- [ ] **Sand Traps:** Modifies the power of the tile (e.g., reduces outgoing power to 1).
- [ ] **Wind / Conveyor Tiles:** Automatically pushes the ball one extra space in a specific direction when it lands.

### Phase 4: Polish & Game Feel
- [ ] Add ball rolling animations and lerping (smooth movement between grids, including diagonal paths).
- [ ] Draw a "trajectory line" when the player is aiming to show exactly where the ball will land before they hit it.
- [ ] Add sound effects (golf club hit, ball rolling, ball falling in hole).
- [ ] Add particle effects for winning a level.
