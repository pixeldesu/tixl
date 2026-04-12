using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Animation;
using T3.Core.DataTypes;
using Xunit;

namespace Core.Tests;

public class CurveSerializationTests
{
    [Fact]
    public void CurveRoundTrip_PreservesAllKeyProperties()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(1.0, new VDefinition
                                    {
                                        Value = 42.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Smooth,
                                        OutInterpolation = VDefinition.KeyInterpolation.Cubic,
                                        InTangentAngle = 1.5,
                                        OutTangentAngle = 2.7,
                                        Weighted = true,
                                        BrokenTangents = true
                                    });

        var restored = RoundTrip(curve);
        var key = restored.GetVDefinitions()[0];

        Assert.Equal(42.0, key.Value);
        Assert.Equal(VDefinition.KeyInterpolation.Smooth, key.InInterpolation);
        Assert.Equal(VDefinition.KeyInterpolation.Cubic, key.OutInterpolation);
        Assert.Equal(1.5, key.InTangentAngle, 10);
        Assert.Equal(2.7, key.OutTangentAngle, 10);
        Assert.True(key.Weighted);
        Assert.True(key.BrokenTangents);
    }

    [Fact]
    public void CurveRoundTrip_PreservesCurveMappings()
    {
        var curve = new Curve();
        curve.PreCurveMapping = CurveUtils.OutsideCurveBehavior.Cycle;
        curve.PostCurveMapping = CurveUtils.OutsideCurveBehavior.Oscillate;
        curve.AddOrUpdateV(0.0, new VDefinition { Value = 1.0 });

        var restored = RoundTrip(curve);

        Assert.Equal(CurveUtils.OutsideCurveBehavior.Cycle, restored.PreCurveMapping);
        Assert.Equal(CurveUtils.OutsideCurveBehavior.Oscillate, restored.PostCurveMapping);
    }

    [Fact]
    public void CurveRoundTrip_DefaultValuesOmitted()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 1.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Linear,
                                        OutInterpolation = VDefinition.KeyInterpolation.Linear,
                                        // Defaults: InTangentAngle=0, OutTangentAngle=0, Weighted=false, BrokenTangents=false
                                    });

        var json = WriteCurveToJson(curve);
        var keysArray = json["Curve"]!["Keys"] as JArray;
        var keyJson = keysArray![0]!;

        // These should be omitted (default values)
        Assert.Null(keyJson["InTangentAngle"]);
        Assert.Null(keyJson["OutTangentAngle"]);
        Assert.Null(keyJson["Weighted"]);
        Assert.Null(keyJson["BrokenTangents"]);

        // These should still be present
        Assert.NotNull(keyJson["Value"]);
        Assert.NotNull(keyJson["InInterpolation"]);
        Assert.NotNull(keyJson["OutInterpolation"]);
    }

    [Fact]
    public void CurveRoundTrip_OmittedDefaultsRestoreCorrectly()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 5.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Linear,
                                        OutInterpolation = VDefinition.KeyInterpolation.Linear,
                                    });

        var restored = RoundTrip(curve);
        var key = restored.GetVDefinitions()[0];

        Assert.Equal(5.0, key.Value);
        Assert.Equal(0.0, key.InTangentAngle);
        Assert.Equal(0.0, key.OutTangentAngle);
        Assert.False(key.Weighted);
        Assert.False(key.BrokenTangents);
    }

    [Fact]
    public void CurveRoundTrip_MultipleKeys()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition { Value = 1.0 });
        curve.AddOrUpdateV(1.0, new VDefinition { Value = 2.0, OutInterpolation = VDefinition.KeyInterpolation.Constant });
        curve.AddOrUpdateV(2.0, new VDefinition { Value = 3.0, BrokenTangents = true });

        var restored = RoundTrip(curve);
        var keys = restored.GetVDefinitions();

        Assert.Equal(3, keys.Count);
        Assert.Equal(1.0, keys[0].Value);
        Assert.Equal(VDefinition.KeyInterpolation.Constant, keys[1].OutInterpolation);
        Assert.True(keys[2].BrokenTangents);
    }

    [Fact]
    public void ReadLegacyCurve_WithoutFormatVersion()
    {
        var legacyJson = JObject.Parse("""
        {
            "Curve": {
                "PreCurve": "Constant",
                "PostCurve": "Constant",
                "Keys": [
                    {
                        "Time": 0.0,
                        "Value": 1.5,
                        "InType": "Spline",
                        "OutType": "Spline",
                        "InEditMode": "Smooth",
                        "OutEditMode": "Smooth",
                        "InTangentAngle": 0.7854,
                        "OutTangentAngle": 3.927
                    },
                    {
                        "Time": 1.0,
                        "Value": 3.0,
                        "InType": "Linear",
                        "OutType": "Constant",
                        "InEditMode": "Linear",
                        "OutEditMode": "Constant",
                        "InTangentAngle": 0.0,
                        "OutTangentAngle": 0.0
                    }
                ]
            }
        }
        """);

        var curve = new Curve();
        curve.Read(legacyJson);

        var keys = curve.GetVDefinitions();
        Assert.Equal(2, keys.Count);

        // First key: Spline+Smooth → Smooth
        Assert.Equal(1.5, keys[0].Value);
        Assert.Equal(VDefinition.KeyInterpolation.Smooth, keys[0].InInterpolation);
        Assert.Equal(VDefinition.KeyInterpolation.Smooth, keys[0].OutInterpolation);
        Assert.Equal(0.7854, keys[0].InTangentAngle, 4);

        // Second key: Linear+Linear / Constant+Constant
        Assert.Equal(3.0, keys[1].Value);
        Assert.Equal(VDefinition.KeyInterpolation.Linear, keys[1].InInterpolation);
        Assert.Equal(VDefinition.KeyInterpolation.Constant, keys[1].OutInterpolation);
    }

    [Fact]
    public void ReadLegacyCurve_ThenReSave_ProducesNewFormat()
    {
        var legacyJson = JObject.Parse("""
        {
            "Curve": {
                "PreCurve": "Cycle",
                "PostCurve": "Constant",
                "Keys": [
                    {
                        "Time": 0.0,
                        "Value": 1.0,
                        "InType": "Spline",
                        "OutType": "Spline",
                        "InEditMode": "Cubic",
                        "OutEditMode": "Cubic",
                        "InTangentAngle": 0.5,
                        "OutTangentAngle": 3.64
                    }
                ]
            }
        }
        """);

        // Load legacy
        var curve = new Curve();
        curve.Read(legacyJson);

        // Re-save
        var newJson = WriteCurveToJson(curve);
        var keyJson = (newJson["Curve"]!["Keys"] as JArray)![0]!;

        // Should have new format fields
        Assert.NotNull(keyJson["InInterpolation"]);
        Assert.NotNull(keyJson["OutInterpolation"]);
        Assert.Equal("Cubic", keyJson["InInterpolation"]!.Value<string>());
        Assert.Equal("Cubic", keyJson["OutInterpolation"]!.Value<string>());

        // Should NOT have old format fields
        Assert.Null(keyJson["InType"]);
        Assert.Null(keyJson["OutType"]);
        Assert.Null(keyJson["InEditMode"]);
        Assert.Null(keyJson["OutEditMode"]);
    }

    private static Curve RoundTrip(Curve curve)
    {
        var json = WriteCurveToJson(curve);
        var restored = new Curve();
        restored.Read(json);
        return restored;
    }

    private static JObject WriteCurveToJson(Curve curve)
    {
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        using var writer = new JsonTextWriter(sw);
        writer.WriteStartObject();
        curve.Write(writer);
        writer.WriteEndObject();
        writer.Flush();
        return JObject.Parse(sb.ToString());
    }
}
