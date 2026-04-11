using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Animation;
using Xunit;

namespace Core.Tests;

public class VDefinitionSerializationTests
{
    [Fact]
    public void WriteReadRoundTrip()
    {
        var original = new VDefinition
                       {
                           Value = 3.14,
                           InType = VDefinition.Interpolation.Spline,
                           OutType = VDefinition.Interpolation.Constant,
                           InEditMode = VDefinition.EditMode.Smooth,
                           OutEditMode = VDefinition.EditMode.Horizontal,
                           InTangentAngle = 1.23,
                           OutTangentAngle = 4.56,
                           Weighted = true,
                           BrokenTangents = true
                       };

        var json = WriteToJson(original);
        var restored = new VDefinition();
        restored.Read(json);

        Assert.Equal(original.Value, restored.Value);
        Assert.Equal(original.InType, restored.InType);
        Assert.Equal(original.OutType, restored.OutType);
        Assert.Equal(original.InEditMode, restored.InEditMode);
        Assert.Equal(original.OutEditMode, restored.OutEditMode);
        Assert.Equal(original.InTangentAngle, restored.InTangentAngle);
        Assert.Equal(original.OutTangentAngle, restored.OutTangentAngle);
        Assert.True(restored.Weighted);
        Assert.True(restored.BrokenTangents);
    }

    [Fact]
    public void ReadLegacyJsonWithoutWeightedAndBrokenTangents()
    {
        var json = new JObject
                   {
                       ["Value"] = 1.0,
                       ["InType"] = "Spline",
                       ["OutType"] = "Linear",
                       ["InEditMode"] = "Smooth",
                       ["OutEditMode"] = "Linear",
                       ["InTangentAngle"] = 0.5,
                       ["OutTangentAngle"] = 1.0
                   };

        var vDef = new VDefinition();
        vDef.Read(json);

        Assert.False(vDef.Weighted);
        Assert.False(vDef.BrokenTangents);
        Assert.Equal(1.0, vDef.Value);
        Assert.Equal(VDefinition.Interpolation.Spline, vDef.InType);
    }

    [Fact]
    public void ReadJsonWithTrueValues()
    {
        var json = new JObject
                   {
                       ["Value"] = 2.0,
                       ["InType"] = "Linear",
                       ["OutType"] = "Linear",
                       ["InEditMode"] = "Linear",
                       ["OutEditMode"] = "Linear",
                       ["InTangentAngle"] = 0.0,
                       ["OutTangentAngle"] = 0.0,
                       ["Weighted"] = true,
                       ["BrokenTangents"] = true
                   };

        var vDef = new VDefinition();
        vDef.Read(json);

        Assert.True(vDef.Weighted);
        Assert.True(vDef.BrokenTangents);
    }

    [Fact]
    public void ReadJsonWithExplicitFalseValues()
    {
        var json = new JObject
                   {
                       ["Value"] = 0.0,
                       ["InType"] = "Linear",
                       ["OutType"] = "Linear",
                       ["InEditMode"] = "Linear",
                       ["OutEditMode"] = "Linear",
                       ["InTangentAngle"] = 0.0,
                       ["OutTangentAngle"] = 0.0,
                       ["Weighted"] = false,
                       ["BrokenTangents"] = false
                   };

        var vDef = new VDefinition();
        vDef.Read(json);

        Assert.False(vDef.Weighted);
        Assert.False(vDef.BrokenTangents);
    }

    private static JObject WriteToJson(VDefinition vDef)
    {
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        using var writer = new JsonTextWriter(sw);
        writer.WriteStartObject();
        vDef.Write(writer);
        writer.WriteEndObject();
        writer.Flush();
        return JObject.Parse(sb.ToString());
    }
}
