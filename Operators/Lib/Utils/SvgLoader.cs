#nullable enable
using Svg;

namespace Lib.Utils;

public static class SvgLoader
{
    public static bool TryLoad(FileResource file, SvgDocument? currentValue, [NotNullWhen(true)] out SvgDocument? newValue, [NotNullWhen(false)] out string? failureReason)
    {
        try
        {
            #pragma warning disable CS0618 // Use Open<T>(string, SvgOptions) - keeping old overload for now
            newValue = SvgDocument.Open<SvgDocument>(file.AbsolutePath, (Dictionary<string, string>?)null);
            #pragma warning restore CS0618
            failureReason = null;
            return true;
        }
        catch (Exception e)
        {
            newValue = null;
            failureReason = $"Failed to load svg file:" + e.Message;
            return false;
        }
    }
}