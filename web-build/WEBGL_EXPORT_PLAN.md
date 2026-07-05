# WebGL Export Plan

## Goal

Prepare PuzzleGolf for professional-grade Unity WebGL export with a repeatable setup flow.

## Current Project Notes

- Unity version: `2022.3.62f2`
- Current build scene:
  - `Assets/Scenes/SampleScene.unity`
- Current project already has Web-specific default resolution fields in `ProjectSettings.asset`

## Recommended WebGL Settings

Based on Unity Web documentation for WebGL:

- Platform:
  - Build target: `WebGL`
- Scripting backend:
  - `IL2CPP`
- Compression for GitHub Pages:
  - `Disabled`
- Data caching:
  - enabled
- Debug symbols:
  - off for release builds
- Exception support:
  - minimize for release
- Code generation:
  - optimize for size
- Code optimization:
  - optimize for disk size / LTO
- Managed stripping:
  - medium by default for safety
- Template:
  - default template first, custom template later if needed
- Web resolution:
  - default target around `1280x720`

## Browser/Platform Constraints To Respect

- WebGL has tighter memory constraints than native builds
- Mobile browser support is limited compared to desktop
- iOS Safari and Android Chrome should be treated as validation targets
- Audio behavior differs on WebGL
- Some runtime/platform APIs are not equivalent to native builds

## Implementation In Repo

We are using an editor-side configuration tool so the team can apply WebGL settings consistently:

- `Assets/Editor/WebGLBuildConfigurator.cs`

Menu items:

- `Tools/Puzzle Golf/WebGL/Apply GitHub Pages Release Settings`
- `Tools/Puzzle Golf/WebGL/Apply GitHub Pages Development Settings`
- `Tools/Puzzle Golf/WebGL/Build Release`
- `Tools/Puzzle Golf/WebGL/Build Development`

## Team Workflow

1. Open the project in Unity.
2. Run:
   - `Tools > Puzzle Golf > WebGL > Apply GitHub Pages Release Settings`
3. Switch platform to WebGL if Unity prompts for it.
4. Test in Editor if needed.
5. Run:
   - `Tools > Puzzle Golf > WebGL > Build Release`
6. Serve the output with a proper static host or local web server.

### Development build option

For a debug-friendly web build that is still GitHub Pages-safe:

1. Run:
   - `Tools > Puzzle Golf > WebGL > Apply GitHub Pages Development Settings`
2. Then run:
   - `Tools > Puzzle Golf > WebGL > Build Development`

## Important Hosting Note

This tool now deletes the existing `Builds/WebGL` folder before building so stale `.br` or `.unityweb` artifacts do not survive across rebuilds.

## Next Steps After Base Export

- Test desktop browsers first
- Validate mobile browser behavior separately
- Add a custom WebGL template for branded loading UI
- Add runtime-safe web-specific input/audio adjustments if needed
- Profile memory and texture sizes for web performance
