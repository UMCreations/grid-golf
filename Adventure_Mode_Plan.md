# Adventure Mode — Complete Redesign Plan

> **Status:** Planning Phase — Do NOT implement yet.
> **Author:** Umer / Meharaj
> **Date:** 2026-03-03

---

## 1. Overview

This document defines the new design for the Adventure Mode game loop. The core idea is to give Adventure Mode its own **structured, handcrafted identity** — distinct from Classic Mode — through a:

- **Saga-style level map** (think Candy Crush / Golf Clash campaign)
- **Procedural but seeded level generation** per level index so the same player always fights the same puzzle until they beat it
- **Gradual, segment-based hazard introduction** (not random like the old system)
- **Hazard-integrated Golden Paths** — the player must navigate *through* special tiles, not avoid them
- **Minimal Hazard Design** — every hazard placed on a level must serve a purpose; no hazard is ever placed just to fill space

---

## 2. Core Design Principle: Minimal Hazards

> **"Every tile that exists must have a reason to exist."**

This is the foundational rule for the new Adventure Mode generator.

### The Problem with Random Hazard Fills
The old system sprinkled special tiles across the board based on a percentage chance. This caused levels where:
- A corner had a random Sand tile the player would never even reach.
- Ice tiles were scattered without intention — the player had no reason to think about them.
- Hazards felt like visual decoration, not mechanical challenges.

### The New Rule: "Earn Your Hazard"
Every special tile placed on a level board must satisfy **at least one** of these conditions:

| Condition | Meaning |
|-----------|--------|
| **On the Golden Path** | The hazard is part of the required solution. The player *must* interact with it. |
| **On a Deliberate False Trail** | The hazard is placed on a convincing dead-end path, to punish players who don't plan ahead. |
| **Adjacent to the Golden Path** | The hazard is within 1 tile of the solution path, making it a credible threat the player must actively avoid. |

Any noise tile that does **not** land in one of the above categories is filled with a **plain Standard numbered tile** — no hazard.

### Per-Segment Hazard Budgets
Each segment defines a strict **maximum hazard count** per level — the generator will never place more special tiles than this limit, regardless of board size.

| Segment | Levels   | Max Hazards on Path | Max Hazards in Noise |
|---------|----------|---------------------|----------------------|
| 1       | 1 – 8    | 0                   | 0                    |
| 2       | 9 – 15   | 1 (Sand)            | 0                    |
| 3       | 16 – 25  | 2 (Sand)            | 2 (Sand only)        |
| 4       | 26 – 30  | 2 (Ice)             | 2 (Sand only)        |
| 5       | 31 – 40  | 3 (Sand + Ice)      | 3 (Sand + Ice)       |
| 6       | 41 – 50  | 3 (any)             | 4 (Sand + Ice + Boost)|
| 7       | 51 – 65  | 4 (any)             | 5 (all)              |
| 8       | 66 – 80  | 5 (any)             | 6 (all)              |
| 9       | 81 – 100 | 6 (any)             | 8 (all)              |

> Noise hazards are placed first on **false trails and path-adjacent tiles**, then stopped once the budget is exhausted. Remaining noise tiles default to Standard numbers.

---

## 2. The Saga Map

### Concept
Instead of a flat difficulty picker, the player is presented with a **linear, scrollable saga map**. Each node on the map represents one level. Nodes unlock sequentially — you cannot skip ahead.

### Node States
| State     | Visual Cue                        |
|-----------|-----------------------------------|
| Locked    | Greyed out, padlock icon          |
| Available | Fully lit, animated "pulse"       |
| Completed | Green star(s) shown on the node   |
| Current   | Highlighted border / glow effect  |

### Star Rating (Per Level)
Each level awards 1–3 stars:
- ⭐ 1 Star — Completed (made it to the hole in any number of strokes)
- ⭐⭐ 2 Stars — Completed at or under Par
- ⭐⭐⭐ 3 Stars — Completed under Par (Par - 1 or better)

### Map Structure
The map is divided into **named "Worlds"** / Segments. Each world has a distinct visual theme matching the hazards introduced in that segment.

| World | Levels   | Theme Name          | Dominant Hazard |
|-------|----------|---------------------|-----------------|
| 1     | 1 – 8    | The Fairway         | Numbers only    |
| 2     | 9 – 15   | Sandy Shores        | Sand introduced |
| 3     | 16 – 25  | Deep Desert         | Sand mastery    |
| 4     | 26 – 30  | Frozen Peaks        | Ice introduced  |
| 5     | 31 – 40  | Arctic Hazard       | Sand + Ice      |
| 6     | 41 – 50  | Launch Pad Valley   | Boost introduced|
| 7     | 51 – 65  | The Gauntlet        | All hazards     |
| 8     | 66 – 80  | Master's Course     | All hazards, extreme difficulty |
| 9     | 81 – 100 | The Final Hole      | Expert — all at max |

> **Note:** The total planned campaign is **100 levels**. Worlds can be extended later.

---

## 3. Level Generation: Fixed Seed Per Level Index

### The Problem with the Old System
The old Adventure Generator used `DateTime.Now.Ticks` as its seed - meaning the board changed every time the player loaded the level. This felt unfair — if a player lost and retried, they faced a completely different puzzle.

### New Approach: Stable Adventure Seeds
Adventure Mode will use a **stable, deterministic seed** per level:

```csharp
int seed = 9000 + levelIndex; // Adventure seed range: 9000–9100+
Random.InitState(seed);
```

> The `9000` offset separates the seed space from Classic Mode (which uses `difficulty * 1000 + levelIndex`), preventing any collision between the two modes' generated levels.

### What This Means in Practice
- **Same level = Same puzzle, every time, for every player.**
- The player learns the layout of a level and improves on it over multiple attempts, just like a hand-crafted level.
- Stars and completion are tracked per `levelIndex` in `PlayerPrefs` or a save system.

---

## 4. Segment-Based Hazard Introduction Plan

Instead of randomly rolling for special tiles anywhere on the board, the grid is divided into clear **level segments**. Each segment has a locked set of rules controlling exactly which tile types can appear and with what density.

### Segment Definitions

---

### Segment 1: The Fairway (Levels 1 – 8)
**Goal:** Teach core mechanics. No hazards. Pure number-based puzzles.

| Parameter   | Value                             |
|-------------|-----------------------------------|
| Grid Size   | 5×5                               |
| Path Length | 3 – 4 strokes                     |
| Max Power   | 2 (small, controlled moves)       |
| Noise Fill  | Standard numbered tiles only      |
| Special Tiles | **None**                        |
| Par Offset  | `pathLength + 2` (very forgiving) |

**Design Intent:** The player should feel confident and learn how to read the numbered tiles to find the correct path.

---

### Segment 2: Sandy Shores (Levels 9 – 15)
**Goal:** Introduce the Sand tile for the first time. Sand is always on the Golden Path.

| Parameter   | Value                                     |
|-------------|-------------------------------------------|
| Grid Size   | 5×5 → 6×6                                |
| Path Length | 4 – 5 strokes                             |
| Max Power   | 2 – 3                                     |
| Noise Fill  | Standard tiles only                       |
| Special Tiles | **Sand (on path only, 1 tile per level)**|
| Par Offset  | `pathLength + 2`                          |

**Design Intent:** Player encounters one Sand tile directly on the Golden Path. They must account for the -1 power reduction in their strategy. Noise tiles are still plain numbers to keep distractions minimal.

**Sand Tile Effect Recap:** Landing on Sand reduces the tile power for the next shot by 1 (minimum 1).

---

### Segment 3: Deep Desert (Levels 16 – 25)
**Goal:** Sand mastery. Multiple Sand tiles now appear on the Golden Path. Noise tiles also start having Sand.

| Parameter   | Value                                                  |
|-------------|--------------------------------------------------------|
| Grid Size   | 6×6                                                   |
| Path Length | 5 – 7 strokes                                          |
| Max Power   | 3 – 4                                                  |
| Noise Fill  | Mostly Standard, ~20% chance of Sand on noise tiles   |
| Special Tiles | **Sand (1–2 tiles on Golden Path)**                 |
| Par Offset  | `pathLength + 2`                                       |

**Design Intent:** The player must now chain their thinking — if they hit one Sand tile, the power drop will carry into the next move. Chaining two Sand tiles creates a cascading constraint the player must plan around.

---

### Segment 4: Frozen Peaks (Levels 26 – 30)
**Goal:** Introduce Ice. Ice is placed on the Golden Path. Sand is now only on noise tiles.

| Parameter   | Value                                                  |
|-------------|--------------------------------------------------------|
| Grid Size   | 6×6 → 7×7                                             |
| Path Length | 5 – 7 strokes                                          |
| Max Power   | 3 – 4                                                  |
| Noise Fill  | ~25% Sand on noise tiles                              |
| Special Tiles | **Ice (1–2 tiles on Golden Path)**, Sand on noise   |
| Par Offset  | `pathLength + 2`                                       |

**Design Intent:** After mastering Sand, Ice is the new learning curve. Ice triggers an automatic extra slide in the same direction. The player must account for this forced movement and still land on target. Keeping Sand in the noise but off the path lets the player focus on learning Ice without extra confusion.

**Ice Tile Effect Recap:** Landing on Ice triggers an auto-slide — the ball automatically moves one additional step in the same incoming direction (if valid).

---

### Segment 5: Arctic Hazard (Levels 31 – 40)
**Goal:** Sand and Ice appear together for the first time, both in noise and on the Golden Path.

| Parameter   | Value                                                       |
|-------------|-------------------------------------------------------------|
| Grid Size   | 7×7                                                        |
| Path Length | 7 – 9 strokes                                               |
| Max Power   | 4                                                           |
| Noise Fill  | ~30% Sand, ~15% Ice on noise tiles                         |
| Special Tiles | **Sand + Ice mixed (1–3 tiles on Golden Path combined)** |
| Par Offset  | `pathLength + 2`                                            |

**Design Intent:** This is the first real test of multi-hazard awareness. A path might go: Number → Sand → Ice → Number → Hole. The player must calculate how the power drop from Sand affects where they end up after the Ice slide.

---

### Segment 6: Launch Pad Valley (Levels 41 – 50)
**Goal:** Introduce Boost. Boost adds +1 to the next shot's power.

| Parameter   | Value                                                         |
|-------------|---------------------------------------------------------------|
| Grid Size   | 7×7 → 8×8                                                   |
| Path Length | 8 – 10 strokes                                               |
| Max Power   | 4 – 5                                                         |
| Noise Fill  | ~25% Sand, ~20% Ice, ~10% Boost on noise tiles              |
| Special Tiles | **Boost (1 tile on Golden Path)**, Sand + Ice in noise    |
| Par Offset  | `pathLength + 2`                                              |

**Design Intent:** Boost opens up new strategic options — a carefully placed Boost on the path lets the player "supercharge" a tile they couldn't normally reach. However, in the noise, Boost becomes a trap that overshoots the player off the board.

**Boost Tile Effect Recap:** Landing on Boost increases the next shot's power by 1.

---

### Segment 7: The Gauntlet (Levels 51 – 65)
**Goal:** All three hazards in play. Difficulty ramps hard.

| Parameter   | Value                                                              |
|-------------|----------------------------------------------------------------------|
| Grid Size   | 8×8                                                               |
| Path Length | 9 – 11 strokes                                                    |
| Max Power   | 5                                                                  |
| Noise Fill  | ~30% Sand, ~25% Ice, ~20% Boost on noise tiles                  |
| Special Tiles | **2–4 mixed hazard tiles on Golden Path**                      |
| Par Offset  | `pathLength + 1` (tighter, more punishing par)                   |

---

### Segment 8: Master's Course (Levels 66 – 80)
**Goal:** Expert-level. Dense hazard boards, larger grids.

| Parameter   | Value                                                              |
|-------------|----------------------------------------------------------------------|
| Grid Size   | 8×8 → 9×9                                                        |
| Path Length | 10 – 12 strokes                                                   |
| Max Power   | 5                                                                  |
| Noise Fill  | ~35% Sand, ~30% Ice, ~25% Boost on noise tiles                  |
| Special Tiles | **3–5 mixed hazard tiles on Golden Path**                      |
| Par Offset  | `pathLength + 1`                                                   |

---

### Segment 9: The Final Hole (Levels 81 – 100)
**Goal:** True mastery challenge. All hazards at maximum density.

| Parameter   | Value                                                              |
|-------------|----------------------------------------------------------------------|
| Grid Size   | 9×9                                                               |
| Path Length | 11 – 14 strokes                                                   |
| Max Power   | 5                                                                  |
| Noise Fill  | ~40% Sand, ~35% Ice, ~30% Boost on noise tiles                  |
| Special Tiles | **4–6 mixed hazard tiles interwoven on Golden Path**           |
| Par Offset  | `pathLength` (exact — zero room for extra strokes)               |

---

## 5. Key Design Change: Hazard-Integrated Golden Path

### Old Behaviour
The old generator forced all Golden Path tiles to be `TileType.Standard`. Hazards only appeared in the noise. The player could theoretically ignore hazards entirely if they found the number sequence.

### New Behaviour — Hazards ON the Golden Path
The redesigned generator will **intentionally place hazard tiles on the Golden Path itself**. This means:

- The player is **guaranteed to need to interact with hazards** to complete the level.
- The puzzle becomes about understanding *which* hazard helps you and *which* one traps you.
- This turns each level into a logic challenge that requires understanding tile effects, not just counting numbers.

### How Hazards on the Path Work

#### Sand on Path
If a Sand tile is on the golden path at position `P`, then the tile *before* P in the path must have its number pre-adjusted so that `power - 1` (the Sand reduction) still lands the player on the correct next tile.

> **Example:** The next tile in the path is 4 spaces away. Normally tile P-1 would be `4`. But with a Sand tile at P, the generator must make the tile *prior to P* have power `5`, because Sand reduces it to `5 - 1 = 4`. The player still makes it.

#### Ice on Path
If an Ice tile is on the path at position `P`, the generator must verify that the auto-slide from P (one extra step in the incoming direction) lands the ball on the correct next tile in the Golden Path.

> **Example:** Player slides into P from the left. Ice auto-slides them one more step to the right. The generator places the next golden path tile at `P + 1` (one step in the same direction).

#### Boost on Path
If a Boost tile is on the path at position `P`, the tile *before* P is assigned power `N-1`, where `N` is the actual distance to the next golden path tile. The Boost adds +1 to make it exact.

> **Example:** Next tile is 3 spaces away. Without Boost, the player needs to be on a `3` tile. With Boost at P, the preceding tile only needs `2`, because `2 + 1 (boost) = 3`.

---

## 7. Noise Fill Strategy

All non-golden-path tiles are "noise" tiles. Their job is to confuse and trap the player. The fill logic follows a **strict priority order** to ensure hazards are minimal and purposeful:

### Step 1 — Assign Powers First
Every empty tile is assigned a random power between `1` and `MaxPower`. This happens **before** any tile types are decided.

### Step 2 — Build False Trails (Highest Priority)
For each tile on the Golden Path, the generator tries to build a 1–2 step "false trail" branching off in a plausible direction. These false trail tiles:
- Get the **same or similar power** as their neighboring Golden Path tile (confusingly believable)
- Are the **first candidates** to receive hazard tiles from the segment's noise hazard budget
- Create realistic-looking dead ends that punish impulsive players

### Step 3 — Place Hazards on Path-Adjacent Tiles (Second Priority)
After false trails are built, any remaining hazard budget is spent on tiles that are **directly adjacent (1 tile away)** to the Golden Path. These act as near-miss traps.

### Step 4 — Fill the Rest as Standard
All remaining empty tiles that did **not** receive a hazard are filled as **plain Standard numbered tiles**. No hazard is placed here just to fill space.

> **Hazard Budget Rule:** The generator tracks a running count of hazards placed. Once the segment's `Max Hazards in Noise` limit is reached (see Section 2 table), all further noise tiles are Standard, no exceptions.

### Why This Matters
This approach guarantees that:
- Players on small boards (5×5) never face an overwhelming number of hazards
- Hazards always feel **intentional** — either guarding the real path or tempting a false one
- The visual reading of a board is **clean** — hazard clusters point to interesting choices, not random clutter

---

## 7. Implementation Notes (For Later)

> **Do not implement anything now.** These are notes for when development begins.

- Create a `AdventureLevelConfig` ScriptableObject or config struct with all per-segment parameters (grid size range, path length range, max power, noise chances, path hazard count range).
- Create a `AdventureSegmentResolver` static helper that takes `levelIndex` and returns the correct `AdventureLevelConfig`.
- Refactor `AdventureLevelGeneratorStrategy` to:
  - Accept the resolved config struct.
  - Support placing hazard tiles directly onto the golden path.
  - Implement the power-compensation logic for Sand and Boost path tiles.
  - Implement the auto-slide continuation check for Ice path tiles.
- Update the `LevelSelectionController` to render the saga map from a list of level node states stored in save data.
- Store Adventure progress in a dedicated save key separate from Classic Mode, e.g., `"AdventureLevel_X_Stars"`.

---

## 9. Summary

| Feature                        | Old System                        | New System                                |
|-------------------------------|-----------------------------------|-------------------------------------------|
| Level Seed                    | Time-based (random every time)    | Fixed per level index (stable)            |
| Hazard Placement              | Random noise tiles only           | Intentional — hazards on golden path too  |
| Hazard Quantity               | Random % fill (could be many)     | **Strict per-level budget — only what's needed** |
| Difficulty Curve              | Flat (just easier/harder params)  | Segmented (10 named worlds, 100 levels)   |
| Hazard Introduction           | All at once based on difficulty    | Gradual — one new type per world          |
| Noise Tile Design             | Random scatter                    | False trails + path-adjacent only         |
| Map UI                        | Simple level list                 | Saga-style scrollable world map           |
| Par System                    | Fixed offset                      | Tightens per world (more punishing later) |
| Player Experience             | Different map every retry         | Same map per level, mastery rewarded      |
