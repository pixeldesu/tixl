using System;

namespace T3.Core.IO;

/// <summary>
/// Global application settings shared across Core, Editor and Player.
/// Saved to projectSettings.json in the settings directory.
/// </summary>
public sealed class CoreSettings : Settings<CoreSettings.ConfigData>
{
    public CoreSettings(bool saveOnQuit) : base("projectSettings.json", saveOnQuit)
    {
    }

    public sealed class ConfigData
    {
        public bool TimeClipSuspending = true;
        public float AudioResyncThreshold = 0.04f;

        public bool EnablePlaybackControlWithKeyboard = true;

        public bool SkipOptimization;
        public bool EnableDirectXDebug;

        public bool LogAssemblyVersionMismatches = false;

        public string LimitMidiDeviceCapture = null;
        public WindowMode DefaultWindowMode = WindowMode.Fullscreen;
        public int DefaultOscPort = 8000;

        // Logging
        public bool LogCompilationDetails = false;
        public bool LogAssemblyLoadingDetails = false;
        public bool LogFileEvents = false;

        // Profiling
        public bool EnableBeatSyncProfiling = false;

        // Audio
        public bool GlobalMute = false;
        public float GlobalPlaybackVolume = 1;
        public bool SoundtrackMute = false;
        public float SoundtrackPlaybackVolume = 0.5f;
        public bool OperatorMute = false;
        public float OperatorPlaybackVolume = 1;
    }
}

[Serializable]
public record ExportSettings(Guid OperatorId, string ApplicationTitle, WindowMode WindowMode, CoreSettings.ConfigData ConfigData, string Author, Guid BuildId, string EditorVersion);

public enum WindowMode { Windowed, Fullscreen }
