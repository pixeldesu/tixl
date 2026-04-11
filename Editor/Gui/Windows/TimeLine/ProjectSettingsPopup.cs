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
            modified |= DrawPlaybackSettings(composition, settings, modified, compositionWithSettings);
        }
        else
        {
            CustomComponents.EmptyWindowMessage("No settings");
            ImGui.EndPopup();
            ImGui.PopStyleVar(1);
            FormInputs.SetIndentToParameters();
        }

        return modified;
    }


    private static bool DrawPlaybackSettings(Instance composition, ProjectSettings settings, bool modified,
        Instance? compositionWithSettings)
    {
        FormInputs.SetIndentToParameters();

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

                    if (FormInputs.AddFloat("Resync Threshold",
                            ref CoreSettings.Config.AudioResyncThreshold,
                            0.001f,
                            0.1f,
                            0.001f,
                            true, true,
                            "If audio playbacks drifts too far from the animation playback it will be resynced. If the threshold for this is too low you will encounter audio glitches. If the threshold is too large you will lose precision. A normal range is between 0.02s and 0.05s.",
                            CoreSettings.Defaults.AudioResyncThreshold))

                    {
                        modified = true;
                    }

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
