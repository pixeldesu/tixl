# Project Settings Refactoring

## Completed Work

### Commit 1: Renamed global `ProjectSettings` → `CoreSettings`
- Class `ProjectSettings` in `Core\IO` renamed to `CoreSettings`
- Keeps `projectSettings.json` as the file name for backward compat
- `ExportSettings` record now references `CoreSettings.ConfigData`
- All ~30+ consumer files updated

### Commit 2: Renamed per-symbol `PlaybackSettings` → `ProjectSettings`
- `Core\Operator\PlaybackSettings` → `Core\Operator\ProjectSettings`
- Fields restructured into `PlaybackConfig` sub-class: `settings.Bpm` → `settings.Playback.Bpm`
- `Symbol.PlaybackSettings` → `Symbol.ProjectSettings`
- Backward-compatible JSON reading: tries `"ProjectSettings"` key first, falls back to `"PlaybackSettings"`
- New format writes nested `"Playback"` sub-object

### Commit 3: Renamed popup + updated plan
- `PlaybackSettingsPopup` → `ProjectSettingsPopup`
- UI title changed to "Project Settings"

---

## Architecture: Separation Principle

- **Core `ProjectSettings`** (in `.t3`) — holds what Core/Player need: playback, audio, timing
- **Editor-only settings** (in `.t3ui` via `SymbolUi`) — render export, view state, etc.
- **`CoreSettings`** (global `projectSettings.json`) — app-wide settings. Many of these should eventually migrate into per-symbol ProjectSettings.

---

## Next Steps

### Render Export Settings (Commit 4)
- Create `RenderExportConfig` class in `Editor\UiModel`
- Serialize into `.t3ui` via `SymbolUi.RenderExport`
- Wire `RenderSettings.ForNextExport` to sync with `RenderExportConfig`
- Move render paths from `UserSettings` into per-symbol `.t3ui`

### CoreSettings Cleanup
The current `CoreSettings.ConfigData` (exported via `ExportSettings` record to Player) contains a mix of concerns:

**Should become per-project (move to `ProjectSettings`):**
- `TimeClipSuspending` — project-specific behavior
- `AudioResyncThreshold` — project-specific audio tuning
- `EnablePlaybackControlWithKeyboard` — project-specific
- `SkipOptimization` — project-specific build setting
- `DefaultWindowMode` — project-specific (Fullscreen vs Windowed for export)
- `DefaultOscPort` — project-specific
- `SoundtrackMute`, `SoundtrackPlaybackVolume` — project audio mix
- `OperatorMute`, `OperatorPlaybackVolume` — project audio mix
- `EnableBeatSyncProfiling` — project-specific debug
- Logging flags (`LogCompilationDetails`, `LogAssemblyLoadingDetails`, `LogFileEvents`, `LogAssemblyVersionMismatches`) — arguably project-specific

**Should stay as editor/machine settings (move to `UserSettings` or keep in `CoreSettings`):**
- `GlobalMute` → rename to `EditorMute` to prevent accidentally muting exported projects
- `GlobalPlaybackVolume` → rename to `EditorGlobalVolume` (same reason)
- `LimitMidiDeviceCapture` — machine-specific hardware config
- `EnableDirectXDebug` — used in `ProgramWindows.cs` for DX debug layer, editor-specific

**Dead code — remove:**
- `EnableMidiSnapshotIndication` — zero consumers, obsolete

**ExportSettings record:**
- Currently bundles full `CoreSettings.ConfigData` for Player. As settings migrate to `ProjectSettings`, the Player should read them from the exported symbol's `ProjectSettings` instead.

### Default Project Settings
- Add a "Default Project Settings" section to the Settings Window
- These defaults are used when creating a new project / new Home operator
- User can configure preferred BPM, audio source, OSC port, window mode, etc. as defaults

### UI Restructuring
- Project Settings popup should get a section panel on left (like Settings Window)
- Sections: Playback, Audio Mix, Export, Output
- Add new settings: preferred resolution/aspect list, key-value parameter storage

### State Persistence (not yet persisted)
- Render settings (time range, fps, quality) → `.t3ui` via `RenderExportConfig`
- Output settings (resolution, camera mode, gizmo, pinned camera)
- Timeline state (zoom/scroll, mode, loop range, pinned anim params)
- Selected operators

### File Format Versioning
- Consider adding a version number to `.t3` and `.t3ui` files for future migration support
