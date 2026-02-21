# UI System Architecture & Implementation Plan

This document outlines the architecture and step-by-step guide for building out the User Interface (UI) for the Grid Golf Puzzle game.

---

## 1. UI Architecture Overview

To maintain a clean and scalable UI system (especially for mobile), we will use a **Canvas-based component architecture**. Instead of a monolithic UIManager, each screen or "Panel" will be managed by its own dedicated controller script. This ensures single responsibility and makes the UI highly modular.

### A. The Canvas Structure
We will have a single main Canvas in the game scene that houses different "Panels". Each panel has its own controller script attached to it.

*   `MainCanvas` (Canvas, Canvas Scaler set to Scale With Screen Size)
    *   `TopBarHUD` (Panel) -> Controlled by **`TopHUDController.cs`**
        *   `StrokesText` (TMP_Text)
        *   `LevelParText` (TMP_Text)
        *   `RestartButton` (Button - quick retry)
        *   `MenuButton` (Button - pause/return to menu)
    *   `MainMenuPanel` (Panel) -> Controlled by **`MainMenuController.cs`**
        *   `GameTitle` (TMP_Text)
        *   `PlayButton` (Button)
    *   `GameWinPanel` (Panel) -> Controlled by **`GameWinController.cs`**
        *   `VictoryText` (TMP_Text)
        *   `Stars/ScoreDisplay` (Images/TMP_Text)
        *   `NextLevelButton` (Button)
        *   `MenuButton` (Button)
    *   `GameOverPanel` (Panel) -> Controlled by **`GameOverController.cs`**
        *   `FailureText` (TMP_Text)
        *   `RetryButton` (Button)
        *   `MenuButton` (Button)

### B. Core Scripts
1.  **Event-Driven / Direct Reference**: Each controller (`TopHUDController`, `GameOverController`, etc.) will either listen to C# events from the `GameManager` (e.g., `OnHoleReachedEvent`, `OnStrokeMadeEvent`) or be directly connected via Unity Events, rather than passing through a monolithic UIManager.
2.  **`GameManager.cs` (Updates)**: Will be updated with Action events (delegates). For example, when `CheckStrokeLimit()` resolves to a loss, it will invoke an `OnGameOver` event. The `GameOverController` simply listens for this event and displays its panel.

---

## 2. Step-by-Step Implementation Plan

### Phase 1: Top HUD Component
1.  **Create Script**: Write `TopHUDController.cs` with references to the Top HUD text fields and buttons.
2.  **Unity Setup**: Set up the `MainCanvas` in Unity with the Canvas Scaler configured for 1080x1920 (mobile portrait).
3.  **Build Top HUD**: Create the UI elements for the Top Bar.
4.  **Integration**: Update `GameManager.cs` to expose an Action (event) for stroke updates. Have `TopHUDController` subscribe to it to update the UI.

### Phase 2: Game Over & Win Screens
1.  **Add Panels**: Add two full-screen semi-transparent black panels to the Canvas (`GameWinPanel`, `GameOverPanel`).
2.  **Create Scripts**: Write `GameWinController.cs` and `GameOverController.cs`.
3.  **Add UI Elements**: Add titles, scores, and Retry/Next buttons to these panels, linking them to their respective controllers.
4.  **Integration**: Update `GameManager` to expose win/loss events. Subscribe the controllers to these events to show themselves.

### Phase 3: Main Menu Integration
1.  **Add Main Menu Panel**: Create a `MainMenuPanel` and `MainMenuController.cs`.
2.  **Game State Overhaul**: Update `GameManager` to support a "Menu" state where the grid isn't active or input isn't allowed until the player hits "Play" on the Main Menu.
3.  **Flow**: Connect the "Play" button in `MainMenuController` to trigger `GameManager.StartGame()`.

---

## 3. Next Actions
Once approved, we will immediately execute **Phase 1** to get the Top HUD displaying your strokes in real-time, managed entirely by its own `TopHUDController`.
