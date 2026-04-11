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

### CoreSettings Cleanup (this session)
- **Renamed** `GlobalMute`/`GlobalPlaybackVolume` → `AppMute`/`AppVolume` in CoreSettings
- **Moved** project-specific fields from `CoreSettings` into `ProjectSettings` sub-classes:
  - `AudioMixConfig`: SoundtrackMute, SoundtrackVolume, OperatorMute, OperatorVolume, AudioResyncThreshold
  - `ExportConfig`: DefaultWindowMode, EnablePlaybackControlWithKeyboard
  - `IoConfig`: DefaultOscPort
  - `PerformanceConfig`: TimeClipSuspending, SkipOptimization, EnableDirectXDebug, EnableBeatSyncProfiling
- **Added** `ProjectSettings.Current` static accessor (reads from `Playback.Current?.Settings ?? Defaults`)
- **Moved** `ProjectSettings` to `T3.Core.Settings` namespace (new `Core/Settings/` directory)
- **Moved** `FileLocations` and `UserData` to `T3.Core.Settings` namespace
- **CoreSettings** now only contains: AppMute, AppVolume, LimitMidiDeviceCapture, logging flags
- **RenderSettings**: Removed `ForNextExport` intermediary; `RenderSettings.Current` reads/writes directly from `SymbolUi.RenderSettings`. Fixed clone bug on composition switch.
- **Fixed** `AddInt` reset button never highlighting (missing `isDefault` check)
- **Wired up** RenderSettings defaults (FPS, Bitrate, MotionBlur, AutoIncrement, ExportAudio, FileFormat, ResolutionFactor)
- **Converted** `ProjectSettingsPopup` → `ProjectSettingsWindow` (proper dockable Window)
- **Added** all categories: Playback, Audio, Rendering, IO, Performance
- **Timeline gear icon** toggles window visibility
- **App menu** shows "Project Settings" with checkmark when focused op defines settings
- **Audio toggle** in toolbar now toggles `AppMute` (app-wide) instead of per-project SoundtrackMute

---

## Architecture

### Separation Principle
- **`ProjectSettings`** (in `.t3`, namespace `T3.Core.Settings`) — what Core/Player need: playback, audio mix, export, IO, performance
- **Editor-only settings** (in `.t3ui` via `SymbolUi`) — render export, view state (future)
- **`CoreSettings`** (global `projectSettings.json`) — app-level settings: AppMute/AppVolume, MIDI, logging
- **`UserSettings`** (global `userSettings.json`) — user preferences, UI state

### Current ProjectSettings structure
```
ProjectSettings (Core\Settings\ProjectSettings.cs, in .t3)
├── Enabled
├── Playback (PlaybackConfig)
│   ├── Bpm, AudioClips, AudioSource, Syncing
│   ├── AudioInputDeviceName, AudioGainFactor, AudioDecayFactor
│   └── EnableAudioBeatLocking, BeatLockAudioOffsetSec
├── Audio (AudioMixConfig)
│   ├── SoundtrackMute, SoundtrackVolume
│   ├── OperatorMute, OperatorVolume
│   └── AudioResyncThreshold
├── Export (ExportConfig)
│   ├── DefaultWindowMode
│   └── EnablePlaybackControlWithKeyboard
├── Io (IoConfig)
│   └── DefaultOscPort
└── Performance (PerformanceConfig)
    ├── TimeClipSuspending, SkipOptimization
    ├── EnableDirectXDebug
    └── EnableBeatSyncProfiling

SymbolUi.RenderSettings (Editor, in .t3ui under Settings > RenderExport)
├── FrameRate, StartInBars, EndInBars, TimeReference, TimeRange
├── RenderMode, Bitrate, ResolutionFactor, OverrideMotionBlurSamples
├── ExportAudio, AutoIncrementVersionNumber, CreateSubFolder, AutoIncrementSubFolder
├── FileFormat
└── VideoFilePath, SequenceFilePath, SequenceFileName, SequencePrefix
```

### Access patterns
- `ProjectSettings.Current` — canonical access for project-level config (null-safe, falls back to Defaults)
- `RenderSettings.Current` — reads directly from focused SymbolUi (lazy-inits with legacy migration)
- `CoreSettings.Config` — app-level audio, MIDI, logging
- `UserSettings.Config` — user preferences

---

## Next: Editor State Persistence

Store per-symbol editor state in `.t3ui` under `"Settings"`, alongside the existing `"RenderExport"` section. This enables restoring the full editor context when reopening a project.

### Output Window State
- Resolution, aspect ratio
- Camera mode (perspective/ortho), pinned camera operator
- Gizmo visibility, grid settings
- Multiple output windows: store as array

### Timeline State
- Zoom level, scroll position
- Loop range (start/end)
- Playback mode (play/pause)
- Pinned animation parameters
- Time display mode (bars/seconds/frames)

### Graph View State
- Selected operator(s)
- Canvas position and zoom
- Expanded/collapsed state of nodes

### Layout State
- Not just the layout index, but the full ImGui docking layout
- Allows restoring exact window arrangement per project

### Priority
These feed directly into the automatic visual testing pipeline (see `Plan_AutomaticTests.md`):
loading a test project → restoring editor state → capturing screenshot → comparing against reference.

---

## Deferred

### Default Project Settings
- `UserSettings.ConfigData` gets a `DefaultProjectSettings` field
- New projects initialize from `UserSettings.Config.DefaultProjectSettings`
- Project Settings Window uses user defaults for reset-to-default buttons
- Settings Window gets a "Default Project Settings" section

### Other
- Preferred resolution/aspect list per project
- Key-value parameter storage per project
