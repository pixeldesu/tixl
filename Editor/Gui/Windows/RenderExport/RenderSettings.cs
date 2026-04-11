#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace T3.Editor.Gui.Windows.RenderExport;

internal sealed class RenderSettings
{
    public static readonly RenderSettings ForNextExport = new();

    [JsonConverter(typeof(StringEnumConverter))]
    public TimeReferences TimeReference;
    public float StartInBars;
    public float EndInBars = 4f;
    public float FrameRate = 60f;
    public int OverrideMotionBlurSamples = -1;

    [JsonConverter(typeof(StringEnumConverter))]
    public RenderModes RenderMode = RenderModes.Video;
    public int Bitrate = 25_000_000;
    public bool AutoIncrementVersionNumber = true;
    public bool CreateSubFolder = true;
    public bool AutoIncrementSubFolder = true;
    public bool ExportAudio = true;
    [JsonConverter(typeof(StringEnumConverter))]
    public ScreenshotWriter.FileFormats FileFormat;
    [JsonConverter(typeof(StringEnumConverter))]
    public TimeRanges TimeRange = TimeRanges.Custom;
    public float ResolutionFactor = 1f;

    // Render paths — persisted per-project in .t3ui
    public string VideoFilePath = "./Render/render-v01.mp4";
    public string SequenceFilePath = "./ImageSequence/";
    public string SequenceFileName = "v01";
    public string SequencePrefix = "render";

    public void CopyFrom(RenderSettings other)
    {
        TimeReference = other.TimeReference;
        StartInBars = other.StartInBars;
        EndInBars = other.EndInBars;
        FrameRate = other.FrameRate;
        OverrideMotionBlurSamples = other.OverrideMotionBlurSamples;
        RenderMode = other.RenderMode;
        Bitrate = other.Bitrate;
        AutoIncrementVersionNumber = other.AutoIncrementVersionNumber;
        CreateSubFolder = other.CreateSubFolder;
        AutoIncrementSubFolder = other.AutoIncrementSubFolder;
        ExportAudio = other.ExportAudio;
        FileFormat = other.FileFormat;
        TimeRange = other.TimeRange;
        ResolutionFactor = other.ResolutionFactor;
        VideoFilePath = other.VideoFilePath;
        SequenceFilePath = other.SequenceFilePath;
        SequenceFileName = other.SequenceFileName;
        SequencePrefix = other.SequencePrefix;
    }

    public RenderSettings Clone()
    {
        var clone = new RenderSettings();
        clone.CopyFrom(this);
        return clone;
    }

    #region Serialization

    internal void WriteToJson(JsonTextWriter writer)
    {
        writer.WritePropertyName("RenderExport");
        writer.WriteRawValue(JsonConvert.SerializeObject(this, Formatting.Indented));
    }

    internal static RenderSettings? ReadFromJson(JToken parentToken)
    {
        var token = parentToken["RenderExport"];
        if (token == null)
            return null;

        return token.ToObject<RenderSettings>();
    }

    #endregion

    internal enum RenderModes
    {
        Video,
        ImageSequence
    }

    internal enum TimeReferences
    {
        Bars,
        Seconds,
        Frames
    }

    internal enum TimeRanges
    {
        Custom,
        Loop,
        Soundtrack,
    }

    internal readonly struct QualityLevel
    {
        internal QualityLevel(double bits, string title, string description)
        {
            MinBitsPerPixelSecond = bits;
            Title = title;
            Description = description;
        }

        internal readonly double MinBitsPerPixelSecond;
        internal readonly string Title;
        internal readonly string Description;
    }
}
