# Unity Setup Instructions (Grid Golf Puzzle - MVP)

Follow these steps exactly to set up the game in the Unity Editor using the scripts we just created. 

## 1. Setup the Tile Prefab
1. In the Hierarchy window, right-click and choose **2D Object > Sprites > Square**. 
2. Name the new object `TilePrefab`.
3. With `TilePrefab` selected, go to the Inspector and click **Add Component**. Search for `Tile` and add the script.
4. Right-click on `TilePrefab` in the Hierarchy and choose **UI > Text - TextMeshPro**.
    * *(If Unity prompts you to import TextMeshPro Essentials, click **Import TMP Essentials**.)*
5. Select the child `Text (TMP)` object in the Hierarchy. In the Inspector:
    * Change the Rect Transform Width and Height so the text box fits inside the square sprite (e.g., W: 1, H: 1 if it's world space, or adjust carefully).
    * Alternately, find the `Canvas` component that was automatically created on `TilePrefab`, change its Render Mode to **World Space**, and shrink its Width/Height down to 1x1. Scale the Text down to fit.
    * In the TextMeshPro component, set the Font Size to around `5` or `10`.
    * Change the Vertex Color to Black (so it's visible on the white square).
    * Set Alignment for both Horizontal and Vertical to **Center/Middle**.
6. Select `TilePrefab` in the Hierarchy again:
    * Drag the `Sprite Renderer` component from the Inspector into the **Sprite Renderer** field on the Tile script component.
    * Drag the child `Text (TMP)` object from the Hierarchy into the **Power Text** field on the Tile script component.
7. In your Project window, navigate to `Assets/Core/Prefabs`. Drag the `TilePrefab` from the Hierarchy into this folder to create a Prefab.
8. Delete the `TilePrefab` from the Hierarchy.

## 2. Setup the Ball Prefab
1. In the Hierarchy window, right-click and choose **2D Object > Sprites > Circle**.
2. Name this object `Ball`.
3. In the Inspector, change its `Transform Scale` to `X: 0.6, Y: 0.6, Z: 0.6` so it's slightly smaller than a tile.
4. If you want, change its `Sprite Renderer` Color to something bright like Red or Blue so it stands out.
5. Click **Add Component**, search for `BallController`, and add the script.
6. Drag the `Ball` from the Hierarchy into `Assets/Core/Prefabs` to create a Prefab.
7. You can leave the `Ball` in the Hierarchy for now.

## 3. Setup the Grid Manager (Game Controller)
1. In the Hierarchy, right-click and choose **Create Empty**. Name it `GridManager`.
2. Select `GridManager`, click **Add Component**, and add the `GridManager` script.
3. In the Inspector for the `GridManager` script:
    * Leave Width and Height at `5`.
    * Leave Tile Size at `1` and Spacing at `0.1`.
    * Drag you `TilePrefab` from the Project folder (`Assets/Core/Prefabs/TilePrefab`) into the **Tile Prefab** field.
    * Leave `Grid Parent` empty (the script will create one automatically).
    * Note the **Level Data (Temporary MVP)** section where `Start Position` is `X: 0, Y: 0` and `Hole Position` is `X: 4, Y: 4`.

## 4. Final Scene Setup
1. Select your `Main Camera` in the Hierarchy. 
2. In the Inspector, change the `Background` color to a darker neutral color like dark grey or blue-grey (so the white grid tiles are clearly visible).
3. Ensure the `Main Camera` Projection is set to **Orthographic**.
4. Adjust the Size (e.g., to `5` or `6`) and the Position (e.g., `X: 2.5, Y: 2.5`) so that the grid will be roughly centered in the Game view.

## 5. Play and Test
1. Press the ▶️ **Play** button at the top of the Unity Editor.
2. The grid will instantly spawn. Look at the grid:
    * The bottom-left tile `[0,0]` will have an "S" denoting the start.
    * The top-right tile `[4,4]` will have an "H" denoting the hole.
    * Other tiles will have random numbers between 1 and 3.
3. The `Ball` will instantly jump to the `S` tile.
4. Press `W`, `A`, `S`, `D` or the Arrow Keys to navigate the ball. 
   *(Note: The ball moves based on the number on the tile it's currently on. If you are on a tile with a "2", pressing Up will move you 2 spaces Up. If that move puts you off the grid, it won't move.)*

---
**Done testing?** Let me know. The next step is adding Win/Loss detection logic!
