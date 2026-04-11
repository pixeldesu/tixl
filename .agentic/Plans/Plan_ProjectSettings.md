# Saving Project settings

## Status quo

1. We currently use Playback-Settings to define the soundtrack, timeline style and BPM-Settings. 
2. These playback-settings are stored for symbols
3. When working with the graph, we transverse the current graph breadcrumps up until we find a symbol with a playback definition. This allows to nest projects (e.g. a demo of demos) and see to most relevant inner playback settings and soundtrack.
4. We should extend this behaviour:

What's currently missing:
1. Every time, I restart TiXL, these settings are restored
   1. After opening a project, the last active operator nesting from an IdPath
   2. The rough view area of the graph
2. From user-setting (shared by all projects)
   1. last active layout (from user settings)
   2. Visible Ui ELements (e.g. MiniMap etc)
   3. Timeline Visibily
   4. MirrorUiOn2ndScreen
   5. RenderVideoFilePath
   6. RenderSequenceFilePath
   7. RenderSequenceFileName
   8. RenderSequencePrefix
3. Not saved:
   1. Render settings (time range, fps, quality settings, etc)
   2. Output settings (Resolution, Gizmo, Pinning op, Camera Mode, Pinned camera)
   3. Selected operators
   4. Timeline-View
      1. Zoom/Scroll
      2. Mode (Dopesheet / Curve)
      3. Current TimePlayback time
      4. Loop Range
      5. Pinned Anim Parameters

Some of these settings (e.g. output resolution / aspect ratio ) are directly relevant for player export

## Notes on Implementation

1. Playback settings should be renamed to Project-Settings
2. Playback settings format should be change to a layout with a section pannel on left like the settings window, with sections for
   1. Playback
   2. Project specific settings (currently in Settings Window)
   3. Probably more options like
      1. a preferred target resolution or aspect list 
      2. A key value storage for parameters
3. The PlaybackSettings-Popup probably needs some structure and refactoring
4. I'm not really sure, where these settings should be serialized. Options would be:
   1. separate projectsettings.json -> Awkward and would need some link to symbol ui
   2. inside .t3 <-- probably the most easiest, but might pollute with view specific settings
   3. inside .t3ui
   4. split between t3 and t3ui (e.g. for view specific settings)
5. The current projectSettings.json must die
   1. This might be tricky, because over time this got polluted with settings that are not project specific but required by core and player which don't have access to UserSettings
6. When reading old symbols.t3 files the Playback settings should be converted
7. It might also be a good idea to introduce a file format version number to the .cs, .t3 and .t3ui files.
