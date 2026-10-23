# PuzzleGolf Level Editor Web

The static web level editor is built for GitHub Pages at `https://umcreations.github.io/grid-golf/editor/`.

## Planned stack

- Vite
- React
- TypeScript

## Editor features

- grid editor
- tile paint/type/power editing
- start/hole placement
- metadata editing
- browser-side validation and solvability checks
- JSON import/export
- optional account-free community submission through a Cloudflare Worker
- maintainer queue to preview pending levels and create publish pull requests

## Source of truth

The web tool must use:

- `../../../level-authoring/schemas/level.schema.json`
- `../../../level-authoring/docs/LEVEL_FORMAT.md`

The web tool is an authoring client. It does not directly edit Unity assets. Community submissions are disabled until Turnstile and Worker settings are configured; see `worker/README.md`.

For the GitHub Pages build, set repository Actions variables `VITE_LEVEL_SUBMISSION_URL` and `VITE_TURNSTILE_SITE_KEY` in `UMCreations/grid-golf`. The site key and Worker URL are public values; never store Worker secrets in these variables. Maintainer review setup and Worker secrets are documented in `worker/README.md`.
