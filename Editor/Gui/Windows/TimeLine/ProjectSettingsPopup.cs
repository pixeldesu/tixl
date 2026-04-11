#nullable enable
using System.Text;
using System.Text.RegularExpressions;
using ImGuiNET;
using ManagedBass;
using ManagedBass.Wasapi;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.IO;
using T3.Core.Operator;
using T3.Core.Settings;
using T3.Editor.Gui.Audio;
using T3.Editor.Gui.Input;
using T3.Editor.Gui.Interaction.Timing;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.Editor.UiModel.InputsAndTypes;

namespace T3.Editor.Gui.Windows.TimeLine;

/// <summary>
/// Draws playback settings
/// </summary>
/// <remarks>
/// Controlling the primary soundtrack is finicky:
/// - "Add soundtrack" adds a <see cref="AudioClipResourceHandle"/> with empty filepath.
/// - When modifying the path we use to resolve the path (i.e. verify if file exists) before setting the filepath.
/// - If valid and set, <see cref="AudioEngine"/> will then load them in CompleteFrame.
///
/// </remarks>
internal static class ProjectSettingsPopup
{
    /// <returns>true if composition was modified</returns>
    internal static bool Draw(Instance? composition)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2, 2));
        ImGui.SetNextWindowSize(new Vector2(650, 500) * T3Ui.UiScaleFactor);
        if (!ImGui.BeginPopupContextItem(ProjectSettingsPopupId))
        {
            ImGui.PopStyleVar(1);
            return false;
        }

        FrameStats.Current.OpenedPopUpName = ProjectSettingsPopupId;
        FrameStats.Current.OpenedPopupHovered = ImGui.IsWindowHovered();
        FrameStats.Current.IsItemContextMenuOpen = true;

        var modified = DrawContent(composition); 
            

        ImGui.EndPopup();
        ImGui.PopStyleVar(1);
        return modified;
    }

    
    private static bool DrawContent(Instance? composition)
    {
        var modified = false;
        ImGui.PushFont(Fonts.FontLarge);
        ImGui.TextUnformatted("Project Settings");
        ImGui.PopFont();

        if (composition == null)
        {
            CustomComponents.EmptyWindowMessage("no composition active");
            return modified;
        }

        FormInputs.SetIndentToLeft();

        PlaybackUtils.FindProjectSettingsForInstance(composition, out var compositionWithSettings, out var settings);

        // Main toggle with composition name
        var isEnabledForCurrent = compositionWithSettings == composition && settings is { Enabled: true };

        if (FormInputs.AddCheckBox("Specify settings for", ref isEnabledForCurrent))
        {
            modified = true;
            if (isEnabledForCurrent)
            {
                settings = composition.Symbol.ProjectSettings;
                if (settings == null)
                {
                    settings = new ProjectSettings();
                    composition.Symbol.ProjectSettings = settings;
                }

                compositionWithSettings = composition;
                settings.Enabled = true;
                Playback.Current.Settings = settings;
            }
            else
            {
                // ReSharper disable once PossibleNullReferenceException
                settings.Enabled = false;
            }
        }

        ImGui.SameLine();
        ImGui.PushFont(Fonts.FontBold);
        ImGui.TextUnformatted(composition.Symbol.Name);
        ImGui.PopFont();

        // Explanation hint
        var hint = "";
        if (isEnabledForCurrent)
        {
            hint = "You're defining new settings for this Project Operator.";
        }
        else if (compositionWithSettings != null && compositionWithSettings != composition)
        {
            hint = $"Inheriting settings from {compositionWithSettings.Symbol.Name}";
        }


        FormInputs.AddHint(hint);

        if (isEnabledForCurrent)
        {
            modified |= DrawSettings(composition, settings, compositionWithSettings);
        }
        else
        {
            CustomComponents.EmptyWindowMessage("No settings");
            //ImGui.EndPopup();
            //ImGui.PopStyleVar(1);
            FormInputs.SetIndentToParameters();
        }

        return modified;
    }

    
    private enum Categories
    {
        Playback,
        Rendering,
        Audio,
        Io,
        Performance,
    }

    private static Categories _activeCategory;

    private static bool DrawSettings(Instance composition, ProjectSettings settings,
        Instance? compositionWithSettings)
    {
        var modified = false;

        ImGui.BeginChild("categories", new Vector2(120 * T3Ui.UiScaleFactor, -1),
            ImGuiChildFlags.Borders,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground);
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f));
            FormInputs.AddSegmentedButtonWithLabel(ref _activeCategory, "", 110 * T3Ui.UiScaleFactor);
            ImGui.PopStyleVar();
        }
        ImGui.EndChild();

        ImGui.SameLine();
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(20, 5));
        ImGui.BeginChild("content", new Vector2(0, 0), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoBackground);
        {
            FormInputs.SetIndentToParameters();
            switch (_activeCategory)
            {
                case Categories.Playback:
                    modified |= DrawPlaybackSettings(composition, settings, compositionWithSettings);
                    break;
                case Categories.Audio:
                    modified |= DrawAudioSettings(settings);
                    break;
                case Categories.Rendering:
                    modified |= DrawRenderingSettings(settings);
                    break;
                case Categories.Io:
                    modified |= DrawIoSettings(settings);
                    break;
                case Categories.Performance:
                    modified |= DrawPerformanceSettings(settings);
                    break;
            }
        }

        ImGui.EndChild();
        ImGui.PopStyleVar();
        return modified;
    }

    private static bool DrawAudioSettings(ProjectSettings settings)
    {
        var modified = false;
        var audio = settings.Audio;
        var defaults = ProjectSettings.Defaults.Audio;

        FormInputs.AddSectionHeader("Audio Mix");

        modified |= FormInputs.AddFloat("Soundtrack Volume",
            ref audio.SoundtrackVolume, 0f, 10f, 0.01f, true, true,
            "Volume level for the project soundtrack.",
            defaults.SoundtrackVolume);

        modified |= FormInputs.AddCheckBox("Mute Soundtrack",
            ref audio.SoundtrackMute,
            "Mute the soundtrack audio.",
            defaults.SoundtrackMute);

        FormInputs.AddVerticalSpace();

        modified |= FormInputs.AddFloat("Operator Volume",
            ref audio.OperatorVolume, 0f, 1f, 0.01f, true, true,
            "Volume level for operator-generated audio.",
            defaults.OperatorVolume);

        modified |= FormInputs.AddCheckBox("Mute Operators",
            ref audio.OperatorMute,
            "Mute all operator audio output.",
            defaults.OperatorMute);

        FormInputs.AddVerticalSpace();

        modified |= FormInputs.AddFloat("Resync Threshold",
            ref audio.AudioResyncThreshold, 0.001f, 0.1f, 0.001f, true, true,
            "If audio playback drifts too far from the animation it will be resynced. A normal range is between 0.02s and 0.05s.",
            defaults.AudioResyncThreshold);

        return modified;
    }

    private static bool DrawRenderingSettings(ProjectSettings settings)
    {
        var modified = false;
        var export = settings.Export;
        var defaults = ProjectSettings.Defaults.Export;

        FormInputs.AddSectionHeader("Export");
        CustomComponents.HelpText("These settings apply when exporting as executable.");
        FormInputs.AddVerticalSpace();

        modified |= FormInputs.AddEnumDropdown(ref export.DefaultWindowMode,
            "Window Mode",
            "The default window mode when running the exported executable.",
            defaults.DefaultWindowMode);

        modified |= FormInputs.AddCheckBox("Enable Playback Control",
            ref export.EnablePlaybackControlWithKeyboard,
            "Users can use cursor left/right to skip through time\nand space key to pause playback\nof exported executable.",
            defaults.EnablePlaybackControlWithKeyboard);

        return modified;
    }

    private static bool DrawIoSettings(ProjectSettings settings)
    {
        var modified = false;
        var io = settings.Io;
        var defaults = ProjectSettings.Defaults.Io;

        FormInputs.AddSectionHeader("OSC");
        CustomComponents.HelpText("Tooll listens for OSC messages on the default port.\nYou can also use the OscInput operator for other ports.");
        FormInputs.AddVerticalSpace();

        modified |= FormInputs.AddInt("Default Port", ref io.DefaultOscPort,
            0, 65535, 1,
            "If a valid port is set, Tooll will listen for OSC messages on this port by default.\nChanging the port requires a restart.",
            defaults.DefaultOscPort);

        return modified;
    }

    private static bool DrawPerformanceSettings(ProjectSettings settings)
    {
        var modified = false;
        var perf = settings.Performance;
        var defaults = ProjectSettings.Defaults.Performance;

        FormInputs.AddSectionHeader("Performance");
        FormInputs.AddVerticalSpace();

        modified |= FormInputs.AddCheckBox("Suspend inactive time clips",
            ref perf.TimeClipSuspending,
            "Avoids dirty flag evaluation of the graph behind inactive TimeClips. Only relevant for complex projects with multiple timeline parts.",
            defaults.TimeClipSuspending);

        modified |= FormInputs.AddCheckBox("Skip Shader Optimization",
            ref perf.SkipOptimization,
            "Makes working with shader graphs easier by skipping HLSL optimization.",
            defaults.SkipOptimization);

        modified |= FormInputs.AddCheckBox("Enable DirectX Debug Mode",
            ref perf.EnableDirectXDebug,
            "Adds debug information to shaders and buffers for tools like RenderDoc.\nCan impact rendering performance. Requires a restart.",
            defaults.EnableDirectXDebug);

        modified |= FormInputs.AddCheckBox("Profile Beat Syncing",
            ref perf.EnableBeatSyncProfiling,
            "Logs beat sync timing to IO Window.",
            defaults.EnableBeatSyncProfiling);

        return modified;
    }

    private static bool DrawPlaybackSettings(Instance composition, ProjectSettings settings,
        Instance? compositionWithSettings)
    {
        var modified = false;
        
        FormInputs.AddSectionHeader("Playback");
        
        //FormInputs.SetIndentToParameters();

        var playback = settings.Playback;

        if (FormInputs.AddSegmentedButtonWithLabel(ref playback.AudioSource, "Audio Source"))
        {
            modified = true;
            UpdatePlaybackAndTimeline(settings);
        }

        FormInputs.AddVerticalSpace();

        ImGui.Separator();

        switch (playback.AudioSource)
        {
            case ProjectSettings.AudioSources.ProjectSoundTrack:
            {
                if (!settings.TryGetMainSoundtrack(compositionWithSettings, out var soundtrackHandle))
                {
                    if (ImGui.Button("Add soundtrack to composition"))
                    {
                        modified = true;
                        playback.AudioClips.Add(new SoundtrackClipDefinition()
                        {
                            IsSoundtrack = true,
                        });
                        _tempSoundtrackFilepathForEdit = string.Empty;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(soundtrackHandle.Clip.FilePath))
                    {
                        _tempSoundtrackFilepathForEdit = string.Empty;
                    }
                    else
                    {
                        var isSoundtrackFileValid = soundtrackHandle.TryGetFileResource(out _);
                        if (isSoundtrackFileValid)
                        {
                            if (ImGui.IsWindowAppearing())
                            {
                                _tempSoundtrackFilepathForEdit = soundtrackHandle.Clip.FilePath;
                            }
                        }
                        else
                        {
                            Log.Warning($"Removing invalid soundtrack file: {soundtrackHandle.Clip.FilePath}");
                            soundtrackHandle.Clip.FilePath = string.Empty;
                            modified = true;
                        }
                    }

                    var editResult = FilePickingUi.DrawTypeAheadSearch(FileOperations.FilePickerTypes.File,
                        AllFilesAudioFilesMp3WavOggMp3WavOgg,
                        ref _tempSoundtrackFilepathForEdit,
                        showAssetFolderToggle:false);


                    var filepathModified = (editResult & InputEditStateFlags.Modified) != 0;
                    if (filepathModified)
                    {
                        modified = true;
                        if (!string.IsNullOrEmpty(_tempSoundtrackFilepathForEdit))
                        {
                        }
                        else
                        {
                        }
                    }

                    FormInputs.ApplyIndent();
                    if (ImGui.Button("Reload"))
                    {
                        AudioEngine.ReloadSoundtrackClip(soundtrackHandle);
                        AudioImageFactory.ResetImageCache();
                        modified = true;
                        filepathModified = true;
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Remove"))
                    {
                        playback.AudioClips.Remove(soundtrackHandle.Clip);
                        modified = true;
                    }

                    FormInputs.AddVerticalSpace();

                    if (FormInputs.AddFloat("BPM",
                            ref soundtrackHandle.Clip.Bpm,
                            0,
                            1000,
                            0.02f,
                            true, true,
                            "In T3 animation units are in bars.\nThe BPM rate controls the animation speed of your project.",
                            120))
                    {
                        Playback.Current.Bpm = soundtrackHandle.Clip.Bpm;
                        playback.Bpm = soundtrackHandle.Clip.Bpm;
                        modified = true;
                    }

                    var soundtrackStartTime = (float)soundtrackHandle.Clip.StartTime;

                    if (FormInputs.AddFloat("Offset",
                            ref soundtrackStartTime,
                            -100,
                            100,
                            0.02f,
                            false, true,
                            "Offsets the beginning of the soundtrack in seconds.",
                            0))
                    {
                        soundtrackHandle.Clip.StartTime = soundtrackStartTime;
                        modified = true;
                    }

                    FormInputs.AddEnumDropdown(ref UserSettings.Config.TimeDisplayMode, "Display Timeline in");

                    modified |= FormInputs.AddFloat("Audio Decay", ref playback.AudioDecayFactor,
                        0.001f,
                        1f,
                        0.01f,
                        true, true,
                        "The decay factors controls the impact of [AudioReaction] when AttackMode. Good values strongly depend on style, loudness and variation of input signal.",
                        0.9f);

                    if (filepathModified)
                    {
                        composition.Symbol.GetSymbolUi().FlagAsModified();
                        AudioEngine.ReloadSoundtrackClip(soundtrackHandle);
                        UpdateBpmFromSoundtrackConfig(soundtrackHandle.Clip);
                        UpdatePlaybackAndTimeline(settings);
                    }
                }

                break;
            }
            case ProjectSettings.AudioSources.ExternalDevice:
            {
                FormInputs.AddVerticalSpace();

                if (FormInputs.AddSegmentedButtonWithLabel(ref playback.Syncing, "Sync Mode"))
                {
                    UpdatePlaybackAndTimeline(settings);
                    modified = true;
                }

                if (playback.Syncing == ProjectSettings.SyncModes.Tapping)
                {
                    FormInputs.SetIndentToParameters();
                    FormInputs.AddHint("""
                                       Tap the [Sync] button on every beat.
                                       The right click on measure to resync and refine.
                                       """);


                    modified |= FormInputs.AddCheckBox("Enable audio beat lock",
                        ref playback.EnableAudioBeatLocking,
                        """
                        If enabled, the editor will look for transient bass, hihats and snares and attempt to look the playback onto the incoming audio signal.
                        To use this, start by tapping the base beat (e.g. with X) then tap the beginning of the bar with (e.g. with X).
                        From now on, you will see the BPM be constantly sliding to look onto the beat).
                        """,
                        true
                    );
                    FormInputs.AddVerticalSpace();
                }

                if (!playback.EnableAudioBeatLocking)
                {
                    modified |= FormInputs.AddFloat("BPM",
                        ref playback.Bpm,
                        0,
                        1000,
                        0.02f,
                        true, true,
                        """
                        In T3 animation units are in bars.
                        The BPM rate controls the animation speed of your project.
                        """,
                        120);
                }

                FormInputs.SetIndentToParameters();
                modified |= FormInputs.AddFloat("Beat Sync Offset (sec)",
                    ref playback.BeatLockAudioOffsetSec,
                    -1f, 1f, 0.001f,
                    true, true,
                    """
                    When using beat lock through audio analysis, you can slightly offset the phase.

                    This might be useful to tighten the sync between audio and video, e.g. if the visual output is delayed by video-processing devices.
                    """,
                    0);


                FormInputs.AddVerticalSpace();

                modified |= FormInputs.AddFloat("Audio Gain", ref playback.AudioGainFactor , 0.01f, 100, 0.01f, true, true,
                    "Can be used to adjust the input signal (e.g. in live situation where the input level might vary.",
                    1);

                modified |= FormInputs.AddFloat("Audio Decay", ref playback.AudioDecayFactor,
                    0.001f,
                    1f,
                    0.01f,
                    true, true,
                    "The decay factors controls the impact of [AudioReaction] when AttackMode. Good values strongly depend on style, loudness and variation of input signal.",
                    0.9f);

                // Input meter - aligned to match form input fields (with tooltip + reset button space like Audio Gain)
                var level = playback.AudioGainFactor * WasapiAudioInput.DecayingAudioLevel * 0.03f;
                var normalizedLevel = level / 644f;
                FormInputs.DrawInputLabel("Input Level");
                var inputSize = FormInputs.GetAvailableInputSize(" ", true, true); // Pass tooltip + hasReset to account for 2 icon spaces
                var cursorScreenPos = ImGui.GetCursorScreenPos();
                AudioLevelMeter.DrawAbsoluteWithinBounds("", normalizedLevel, ref _smoothedLevel, 2f, cursorScreenPos.X, cursorScreenPos.X + inputSize.X);

                FormInputs.DrawInputLabel("Input Device");
                ImGui.BeginGroup();

                if (ImGui.BeginCombo("##SelectDevice", playback.AudioInputDeviceName, ImGuiComboFlags.HeightLarge))
                {
                    foreach (var d in WasapiAudioInput.InputDevices)
                    {
                        var isSelected = d.DeviceInfo.Name == playback.AudioInputDeviceName;
                        if (ImGui.Selectable($"{d.DeviceInfo.Name}", isSelected, ImGuiSelectableFlags.NoAutoClosePopups))
                        {
                            Bass.Configure(Configuration.UpdateThreads, false);
                            playback.AudioInputDeviceName = d.DeviceInfo.Name;
                            modified = true;
                            CoreSettings.Save();
                            //WasapiAudioInput.StartInputCapture(d);
                            T3.Core.Audio.AudioEngine.OnAudioDeviceChanged(); // <-- Ensure audio engine resets on device change
                        }

                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.PushFont(Fonts.FontSmall);
                            var sb = new StringBuilder();
                            var di = d.DeviceInfo;

                            var fields = typeof(WasapiDeviceInfo).GetProperties();
                            foreach (var f in fields)
                            {
                                sb.Append(f.Name);
                                sb.Append(": ");
                                sb.Append(f.GetValue(di));
                                sb.Append("\n");
                            }

                            ImGui.TextUnformatted(sb.ToString());
                            ImGui.PopFont();
                            ImGui.EndTooltip();
                        }
                    }
                    ImGui.EndCombo();
                }

                if (!string.IsNullOrEmpty(playback.AudioInputDeviceName)
                    &&playback.AudioInputDeviceName != WasapiAudioInput.ActiveInputDeviceName)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, UiColors.StatusWarning.Rgba);
                    ImGui.TextUnformatted(playback.AudioInputDeviceName + " (NOT FOUND)");
                    ImGui.PopStyleColor();
                }

                ImGui.EndGroup();
                break;
            }
        }

        return modified;
    }

    private static void UpdatePlaybackAndTimeline(ProjectSettings settings)
    {
        var playback = settings.Playback;
        if (playback.AudioSource == ProjectSettings.AudioSources.ProjectSoundTrack)
        {
            Playback.Current = T3Ui.DefaultTimelinePlayback;

            if (playback.AudioClips.Count > 0)
            {
                // Don't call Bass.Free() directly - this destroys all operator streams!
                // Instead, ensure the mixer is properly initialized
                // AudioMixerManager handles BASS initialization internally
                Playback.Current.Bpm = playback.AudioClips[0].Bpm;
                if (Playback.Current.Settings != null)
                    Playback.Current.Settings.Playback.Syncing = ProjectSettings.SyncModes.Timeline;
            }

            UserSettings.Config.ShowTimeline = true;
        }
        else
        {
            if (playback.Syncing == ProjectSettings.SyncModes.Tapping)
            {
                Playback.Current = T3Ui.DefaultBeatTimingPlayback;
                UserSettings.Config.ShowTimeline = false;
                UserSettings.Config.EnableIdleMotion = true;
                // Don't call Bass.Free() directly - this destroys all operator streams!
                Playback.Current.PlaybackSpeed = 1;
            }
            else
            {
                Playback.Current = T3Ui.DefaultTimelinePlayback;
                UserSettings.Config.ShowTimeline = true;
                Playback.Current.PlaybackSpeed = 0;
            }
        }
    }

    private static void UpdateBpmFromSoundtrackConfig(SoundtrackClipDefinition? audioClip)
    {
        if (audioClip == null || string.IsNullOrEmpty(audioClip.FilePath))
        {
            Log.Error("Can't detected BPM-rate from empty undefined audio-clip filename");
            return;
        }

        var matchBpmPattern = new Regex(@"(\d+\.?\d*)bpm");
        var result = matchBpmPattern.Match(audioClip.FilePath);
        if (!result.Success)
            return;

        if (float.TryParse(result.Groups[1].Value, out var bpm))
        {
            Log.Debug($"Using bpm-rate {bpm} from filename.");
            audioClip.Bpm = bpm;
        }
    }

    /** We use this for modification inside the input field and checking if path is valid before actually assigning it to the soundtrack */
    private static string? _tempSoundtrackFilepathForEdit = string.Empty;

    private static float _smoothedLevel;
    public const string ProjectSettingsPopupId = "##PlaybackSettings";
    private const string AllFilesAudioFilesMp3WavOggMp3WavOgg = "mp3,wav,ogg";
}
