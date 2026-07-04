# PuzzleGolf Level Editor Web

This folder will contain the static web level editor hosted on GitHub Pages.

## Planned stack

- Vite
- React
- TypeScript

## Planned MVP

- grid editor
- tile paint/type/power editing
- start/hole placement
- metadata editing
- browser-side validation
- JSON import/export

## Source of truth

The web tool must use:

- `../../../level-authoring/schemas/level.schema.json`
- `../../../level-authoring/docs/LEVEL_FORMAT.md`

The web tool is an authoring client. It does not directly edit Unity assets.
