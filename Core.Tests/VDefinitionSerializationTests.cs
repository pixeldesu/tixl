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
                           InInterpolation = VDefinition.KeyInterpolation.Smooth,
                           OutInterpolation = VDefinition.KeyInterpolation.Horizontal,
                           InTangentAngle = 1.23,
                           OutTangentAngle = 4.56,
                           Weighted = true,
                           BrokenTangents = true
                       };

        var json = WriteToJson(original);
        var restored = new VDefinition();
        restored.Read(json);

        Assert.Equal(original.Value, restored.Value);
        Assert.Equal(original.InInterpolation, restored.InInterpolation);
        Assert.Equal(original.OutInterpolation, restored.OutInterpolation);
        Assert.Equal(original.InTangentAngle, restored.InTangentAngle);
        Assert.Equal(original.OutTangentAngle, restored.OutTangentAngle);
        Assert.True(restored.Weighted);
        Assert.True(restored.BrokenTangents);
    }

    [Fact]
    public void ReadLegacyJsonWithoutNewFields()
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

        Assert.Equal(VDefinition.KeyInterpolation.Smooth, vDef.InInterpolation);
        Assert.Equal(VDefinition.KeyInterpolation.Linear, vDef.OutInterpolation);
        Assert.Equal(1.0, vDef.Value);
        Assert.False(vDef.Weighted);
        Assert.False(vDef.BrokenTangents);
    }

    [Fact]
    public void ReadLegacyConstant()
    {
        var json = new JObject
                   {
                       ["Value"] = 5.0,
                       ["InType"] = "Constant",
                       ["OutType"] = "Constant",
                       ["InEditMode"] = "Constant",
                       ["OutEditMode"] = "Constant",
                       ["InTangentAngle"] = 0.0,
                       ["OutTangentAngle"] = 0.0
                   };

        var vDef = new VDefinition();
        vDef.Read(json);

        Assert.Equal(VDefinition.KeyInterpolation.Constant, vDef.InInterpolation);
        Assert.Equal(VDefinition.KeyInterpolation.Constant, vDef.OutInterpolation);
    }

    [Fact]
    public void ReadLegacySplineCubic()
    {
        var json = new JObject
                   {
                       ["Value"] = 0.0,
                       ["InType"] = "Spline",
                       ["OutType"] = "Spline",
                       ["InEditMode"] = "Cubic",
                       ["OutEditMode"] = "Cubic",
                       ["InTangentAngle"] = 0.0,
                       ["OutTangentAngle"] = 0.0
                   };

        var vDef = new VDefinition();
        vDef.Read(json);

        Assert.Equal(VDefinition.KeyInterpolation.Cubic, vDef.InInterpolation);
        Assert.Equal(VDefinition.KeyInterpolation.Cubic, vDef.OutInterpolation);
    }

    [Fact]
    public void ReadLegacySplineTangent()
    {
        var json = new JObject
                   {
                       ["Value"] = 0.0,
                       ["InType"] = "Spline",
                       ["OutType"] = "Spline",
                       ["InEditMode"] = "Tangent",
                       ["OutEditMode"] = "Horizontal",
                       ["InTangentAngle"] = 1.5,
                       ["OutTangentAngle"] = 3.14
                   };

        var vDef = new VDefinition();
        vDef.Read(json);

        Assert.Equal(VDefinition.KeyInterpolation.Tangent, vDef.InInterpolation);
        Assert.Equal(VDefinition.KeyInterpolation.Horizontal, vDef.OutInterpolation);
    }

    [Fact]
    public void ReadNewFormatDirectly()
    {
        var json = new JObject
                   {
                       ["Value"] = 2.0,
                       ["InInterpolation"] = "Cubic",
                       ["OutInterpolation"] = "Smooth",
                       ["InTangentAngle"] = 0.0,
                       ["OutTangentAngle"] = 0.0,
                       ["Weighted"] = true,
                       ["BrokenTangents"] = false
                   };

        var vDef = new VDefinition();
        vDef.Read(json);

        Assert.Equal(VDefinition.KeyInterpolation.Cubic, vDef.InInterpolation);
        Assert.Equal(VDefinition.KeyInterpolation.Smooth, vDef.OutInterpolation);
        Assert.True(vDef.Weighted);
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
