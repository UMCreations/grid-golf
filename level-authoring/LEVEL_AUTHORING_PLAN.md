# Level Authoring Plan

## Goal

Build a team-usable web level editor for PuzzleGolf that:

- runs as a static site on GitHub Pages
- uses JSON as the source-of-truth level format
- imports into Unity for testing and gameplay
- supports future expansion into shared review and collaboration workflows

## Source Of Truth

- Canonical format: JSON files under `level-authoring/levels/`
- Schema: `level-authoring/schemas/level.schema.json`
- Unity import layer converts JSON into runtime-compatible assets/data

## Repository Structure

```text
level-authoring/
  LEVEL_AUTHORING_PLAN.md
  docs/
    LEVEL_FORMAT.md
  schemas/
    level.schema.json
  levels/
    .gitkeep

tools/
  level-editor-web/
    (future static web app)

PuzzleGolf/Assets/Core/Scripts/LevelImport/
  LevelAuthoringDto.cs
  LevelAuthoringValidator.cs
  LevelAuthoringConverter.cs
  Editor/
    LevelJsonAssetUtility.cs
```

## Phases

### Phase 1: Shared format and Unity foundation

- [x] Decide JSON as the source-of-truth format
- [x] Create schema/documentation folders
- [x] Define first version of level JSON schema
- [x] Define Unity DTOs for import/export
- [x] Add Unity validation layer
- [x] Add Unity conversion layer
- [x] Add basic Unity editor import/export utility

### Phase 2: Static web tool MVP

- [ ] Scaffold static frontend under `tools/level-editor-web`
- [ ] Render editable grid
- [ ] Support tile paint/type/power editing
- [ ] Support start/hole placement
- [ ] Support metadata editing
- [ ] Validate against local rules
- [ ] Import/export JSON files in browser
- [ ] Add GitHub Pages deployment

### Phase 3: Team workflow hardening

- [ ] Roundtrip test: JSON -> Unity asset -> runtime
- [ ] Roundtrip test: existing `HandcraftedLevelSO` -> JSON -> Unity asset
- [ ] Mark imported assets as generated
- [ ] Add level library conventions and naming rules
- [ ] Add review checklist for authored levels

### Phase 4: Better design tooling

- [ ] Add solver preview
- [ ] Add path preview
- [ ] Add dead-end warnings
- [ ] Add alternate-solution warnings
- [ ] Add copy/paste, fill, mirror, rotate
- [ ] Add archetype/tags/notes support in UI

## Task List

### Format and schema

- [x] Define level header fields
- [x] Define tile payload format
- [x] Define metadata block
- [x] Define validation rules
- [ ] Add schema evolution strategy for `schemaVersion`

### Unity integration

- [x] Create DTOs
- [x] Create validation report/result model
- [x] Create DTO <-> `HandcraftedLevelSO` conversion
- [x] Create DTO <-> `LevelData` conversion
- [x] Add JSON import utility
- [x] Add JSON export utility
- [x] Default imported assets to `Assets/Core/LeveLData/Imported`
- [ ] Add importer support for batch folder import
- [ ] Add generated-asset marker fields

### Web tool

- [ ] Choose stack: Vite + React + TypeScript
- [ ] Create app shell
- [ ] Build grid canvas/editor
- [ ] Build inspector panel
- [ ] Build validation panel
- [ ] Build import/export actions
- [ ] Build GitHub Pages deployment workflow

## Decisions

- Host the web tool on GitHub Pages
- Keep the web tool in this repo for now
- Keep Unity as a consumer of authored JSON, not the source of truth
- Use file-based workflow first, no backend/server

## Immediate Next Steps

1. Finish the Unity import/export roundtrip test path.
2. Scaffold the static web tool in `tools/level-editor-web`.
3. Implement browser-side schema-aware editing.
