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
- `PlaybackSettingsPopup` → `ProjectSettingsPopup` (class, ID, file)
- `DrawPlaybackSettings()` → `Draw()`
- UI title changed to "Project Settings"

### Commit 4: Added render export settings to .t3ui
- `RenderSettings` class now has `WriteToJson`/`ReadFromJson` using Newtonsoft auto-serialization
- Render path fields (`VideoFilePath`, `SequenceFilePath`, etc.) moved from `UserSettings` to `RenderSettings`
- `SymbolUi.RenderSettings` property stores per-symbol render config
- Serialized under `"Settings" > "RenderExport"` in .t3ui (PascalCase keys)
- `RenderWindow` syncs settings from/to `SymbolUi.RenderSettings` on composition change
- Legacy migration: reads old render paths from UserSettings on first load
- `RenderExportConfig` class was created then deleted — `RenderSettings` handles serialization directly

### Commit 5: RenderWindow modified tracking
- All draw methods (`DrawTimeSetup`, `DrawInnerContent`, `DrawVideoSettings`, `DrawImageSequenceSettings`, `DrawResolutionPopoverCompact`) return `bool`
- `modified |=` pattern flows through to `SyncSettingsToProject(modified)` which calls `FlagAsModified()` only when needed
- Post-render auto-increment in `RenderProcess` also flags as modified

### Commit 6: File format versioning
- New `Core\Model\SymbolFormatVersion.cs` with `Current = 1`, change log, and `WarnIfNewer()`
- Both .t3 and .t3ui write `"FormatVersion": 1` and `"TixlVersion": "x.y.z"` at the top
- On load, warns if file format is newer than what the editor supports
- Works automatically for copy/paste (uses same serialization path)
- `JsonKeys` in both `SymbolJson.cs` and `SymbolUiJson.cs` reordered to match serialization order

---

## Architecture

### Separation Principle
- **Core `ProjectSettings`** (in `.t3`) — what Core/Player need: playback, audio mix, export, debug
- **Editor-only settings** (in `.t3ui` via `SymbolUi`) — render export, view state
- **`CoreSettings`** (global `projectSettings.json`) — machine/editor-specific settings only
- **`UserSettings`** (global `userSettings.json`) — user preferences, UI state, default project settings

### Current ProjectSettings structure
```
ProjectSettings (Core\Operator\ProjectSettings.cs, in .t3)
├── Enabled
└── Playback (PlaybackConfig)
    ├── Bpm, AudioClips, AudioSource, Syncing
    ├── AudioInputDeviceName, AudioGainFactor, AudioDecayFactor
    └── EnableAudioBeatLocking, BeatLockAudioOffsetSec

SymbolUi.RenderSettings (Editor, in .t3ui under Settings > RenderExport)
├── FrameRate, StartInBars, EndInBars, TimeReference, TimeRange
├── RenderMode, Bitrate, ResolutionFactor, OverrideMotionBlurSamples
├── ExportAudio, AutoIncrementVersionNumber, CreateSubFolder, AutoIncrementSubFolder
├── FileFormat
└── VideoFilePath, SequenceFilePath, SequenceFileName, SequencePrefix
```

---

## Next: CoreSettings Cleanup

### Fields to move to `ProjectSettings` (new sub-classes)

**`ProjectSettings.AudioConfig`:**
- `SoundtrackMute`, `SoundtrackPlaybackVolume` — project audio mix
- `OperatorMute`, `OperatorPlaybackVolume` — project audio mix
- `AudioResyncThreshold` — already in ProjectSettingsPopup

**`ProjectSettings.ExportConfig`:**
- `DefaultWindowMode` (WindowMode enum) — export behavior
- `EnablePlaybackControlWithKeyboard` — export behavior

**`ProjectSettings.DebugConfig`:**
- `TimeClipSuspending` — operator behavior
- `SkipOptimization` — shader compilation
- `DefaultOscPort` — networking
- `EnableBeatSyncProfiling` — beat sync debugging

### Fields staying in CoreSettings
- `GlobalMute` → rename to `EditorMute`
- `GlobalPlaybackVolume` → rename to `EditorVolume`
- `EnableDirectXDebug` — machine-specific, requires restart
- `LimitMidiDeviceCapture` — machine-specific hardware
- `LogCompilationDetails` — consumed during startup before any symbol loads
- `LogAssemblyLoadingDetails` — same
- `LogFileEvents` — same
- `LogAssemblyVersionMismatches` — same

### How Core code accesses ProjectSettings
`Playback.Current.Settings` provides the active `ProjectSettings` at runtime (set via breadcrumb traversal in `PlaybackUtils`). Core code reads from there instead of `CoreSettings.Config`.

### Default Project Settings
- `UserSettings.ConfigData` gets a `DefaultProjectSettings` field
- New projects initialize from `UserSettings.Config.DefaultProjectSettings`
- ProjectSettingsPopup uses defaults for reset-to-default buttons
- Settings Window gets a "Default Project Settings" section

### Key files to modify
See detailed plan at `.claude/plans/sorted-humming-pretzel.md`

---

## Future Work

### UI Restructuring
- Project Settings popup gets a section panel on left (like Settings Window)
- Sections: Playback, Audio Mix, Export, Output

### Editor State Persistence (in .t3ui under Settings)
- Timeline state (zoom/scroll, mode, loop range, pinned anim params)
- Output settings (resolution, camera mode, gizmo, pinned camera)
- Selected operators

### Other
- Preferred resolution/aspect list per project
- Key-value parameter storage per project
