# GitHub Pages Hosting

## Final URLs

After deployment, the site structure is:

- Home:
  - `https://<your-username>.github.io/PuzzleGolf/`
- Game:
  - `https://<your-username>.github.io/PuzzleGolf/game/`
- Level editor:
  - `https://<your-username>.github.io/PuzzleGolf/editor/`

## What This Repo Publishes

- `tools/level-editor-web/dist` -> `/editor/`
- `PuzzleGolf/Builds/WebGL` -> `/game/`
- `web-build/index.html` -> root landing page

## Important Repo Assumption

The editor base path is configured for a repo named `PuzzleGolf`:

- `tools/level-editor-web/vite.config.ts`

It uses:

```ts
base: "/PuzzleGolf/editor/"
```

If the GitHub repo name changes, update that path.

## One-Time Setup On GitHub

1. Push this repo to GitHub.
2. Open the repo on GitHub.
3. Go to `Settings > Pages`.
4. Under `Build and deployment`, set:
   - `Source: GitHub Actions`
5. Open the `Actions` tab and confirm the workflow runs.

## Files Used For Deployment

- Workflow:
  - `.github/workflows/deploy-pages.yml`
- Landing page:
  - `web-build/index.html`
- Unity game build:
  - `PuzzleGolf/Builds/WebGL`
- Level editor app:
  - `tools/level-editor-web`

## Before You Push

Make sure these are true:

1. The editor builds locally:

```bash
cd /Users/meharaj/Desktop/Umer/ProductionGames/PuzzleGolf/tools/level-editor-web
npm ci
npm run build
```

2. The Unity WebGL build exists here:

```text
PuzzleGolf/Builds/WebGL/index.html
PuzzleGolf/Builds/WebGL/Build/...
PuzzleGolf/Builds/WebGL/TemplateData/...
```

If `PuzzleGolf/Builds/WebGL/index.html` is missing, the GitHub workflow will fail on purpose.

## Publish Process

### Step 1: Build the level editor

```bash
cd /Users/meharaj/Desktop/Umer/ProductionGames/PuzzleGolf/tools/level-editor-web
npm ci
npm run build
```

### Step 2: Build the game in Unity

In Unity:

1. Open the project
2. Run:
   - `Tools > Puzzle Golf > WebGL > Apply Recommended Settings`
3. Run:
   - `Tools > Puzzle Golf > WebGL > Build Release`
4. Confirm the build lands in:
   - `PuzzleGolf/Builds/WebGL`

### Step 3: Commit and push everything

From repo root:

```bash
cd /Users/meharaj/Desktop/Umer/ProductionGames/PuzzleGolf
git add .
git commit -m "Set up GitHub Pages hosting for game and editor"
git push origin main
```

### Step 4: Wait for GitHub Actions

On GitHub:

1. Open `Actions`
2. Open `Deploy GitHub Pages`
3. Wait for:
   - editor build
   - Pages artifact upload
   - deployment success

### Step 5: Open the site

- Home:
  - `https://<your-username>.github.io/PuzzleGolf/`
- Game:
  - `https://<your-username>.github.io/PuzzleGolf/game/`
- Editor:
  - `https://<your-username>.github.io/PuzzleGolf/editor/`

## Updating Later

### To update only the editor

1. Change files in `tools/level-editor-web`
2. Build locally
3. Commit and push

### To update only the game

1. Rebuild WebGL in Unity into `PuzzleGolf/Builds/WebGL`
2. Commit and push

### To update both

1. Rebuild editor
2. Rebuild WebGL
3. Commit and push

## Common Problems

### 1. Editor loads with broken assets

Cause:
- wrong Vite `base` path

Fix:
- verify `tools/level-editor-web/vite.config.ts` uses:
  - `"/PuzzleGolf/editor/"`

### 2. Workflow fails because game is missing

Cause:
- `PuzzleGolf/Builds/WebGL/index.html` is not present

Fix:
- rebuild Unity WebGL and commit the output

### 3. WebGL game loads but fails at runtime

Possible causes:
- browser memory limits
- WebGL compression/serving compatibility
- Unity Web-specific runtime limitations

Fix:
- test desktop first
- then test mobile separately
- if needed, adjust Unity WebGL publishing settings
