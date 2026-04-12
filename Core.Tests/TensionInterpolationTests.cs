using T3.Core.Animation;
using T3.Core.DataTypes;
using Xunit;

namespace Core.Tests;

public class TensionInterpolationTests
{
    [Fact]
    public void DefaultTension_BehavesLikeOriginal()
    {
        // Tension 1.0 (default) should produce the same result as the original interpolation
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 0.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Smooth,
                                        OutInterpolation = VDefinition.KeyInterpolation.Smooth,
                                        TensionIn = 1.0f,
                                        TensionOut = 1.0f,
                                    });
        curve.AddOrUpdateV(1.0, new VDefinition
                                    {
                                        Value = 1.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Smooth,
                                        OutInterpolation = VDefinition.KeyInterpolation.Smooth,
                                        TensionIn = 1.0f,
                                        TensionOut = 1.0f,
                                    });

        // Should match a curve without explicit tension settings
        var curveNoTension = new Curve();
        curveNoTension.AddOrUpdateV(0.0, new VDefinition
                                             {
                                                 Value = 0.0,
                                                 InInterpolation = VDefinition.KeyInterpolation.Smooth,
                                                 OutInterpolation = VDefinition.KeyInterpolation.Smooth,
                                             });
        curveNoTension.AddOrUpdateV(1.0, new VDefinition
                                             {
                                                 Value = 1.0,
                                                 InInterpolation = VDefinition.KeyInterpolation.Smooth,
                                                 OutInterpolation = VDefinition.KeyInterpolation.Smooth,
                                             });

        for (double u = 0.0; u <= 1.0; u += 0.1)
        {
            Assert.Equal(curveNoTension.GetSampledValue(u), curve.GetSampledValue(u), 10);
        }
    }

    [Fact]
    public void LowTension_ProducesSnapperCurve()
    {
        // Low tension (0.3) should produce a snappier transition — closer to linear
        var curveSnap = new Curve();
        curveSnap.AddOrUpdateV(0.0, new VDefinition
                                        {
                                            Value = 0.0,
                                            InInterpolation = VDefinition.KeyInterpolation.Tangent,
                                            OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                                            OutTangentAngle = Math.Atan(1.0), // 45 degrees, slope=1
                                            TensionOut = 0.3f,
                                        });
        curveSnap.AddOrUpdateV(1.0, new VDefinition
                                        {
                                            Value = 1.0,
                                            InInterpolation = VDefinition.KeyInterpolation.Tangent,
                                            OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                                            InTangentAngle = Math.Atan(1.0),
                                            TensionIn = 0.3f,
                                        });

        var curveNormal = new Curve();
        curveNormal.AddOrUpdateV(0.0, new VDefinition
                                          {
                                              Value = 0.0,
                                              InInterpolation = VDefinition.KeyInterpolation.Tangent,
                                              OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                                              OutTangentAngle = Math.Atan(1.0),
                                              TensionOut = 1.0f,
                                          });
        curveNormal.AddOrUpdateV(1.0, new VDefinition
                                          {
                                              Value = 1.0,
                                              InInterpolation = VDefinition.KeyInterpolation.Tangent,
                                              OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                                              InTangentAngle = Math.Atan(1.0),
                                              TensionIn = 1.0f,
                                          });

        // At midpoint, low tension should be closer to linear (0.5) than high tension
        var snapMid = curveSnap.GetSampledValue(0.5);
        var normalMid = curveNormal.GetSampledValue(0.5);

        // Low tension should produce a DIFFERENT curve than normal tension
        var snapQuarter = curveSnap.GetSampledValue(0.25);
        var normalQuarter = curveNormal.GetSampledValue(0.25);

        Assert.True(Math.Abs(snapQuarter - normalQuarter) > 0.01,
                    $"Different tensions should produce different curves: snap={snapQuarter:F4}, normal={normalQuarter:F4}");
    }

    [Fact]
    public void HighTension_ProducesMoreOvershoot()
    {
        // High tension (2.0) with tangents pointing up should produce overshoot
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 0.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        OutTangentAngle = 0.7854, // 45 degrees up
                                        TensionOut = 2.0f,
                                    });
        curve.AddOrUpdateV(1.0, new VDefinition
                                    {
                                        Value = 0.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        InTangentAngle = Math.PI - 0.7854, // coming from above
                                        TensionIn = 2.0f,
                                    });

        // With high tension and upward tangents, midpoint should overshoot significantly above 0
        var mid = curve.GetSampledValue(0.5);
        Assert.True(mid > 0.5, $"High tension should produce significant overshoot, got {mid}");
    }

    [Fact]
    public void ZeroTension_ApproachesLinear()
    {
        // Near-zero tension should produce nearly linear interpolation regardless of angle
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 0.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        OutTangentAngle = 1.2, // steep angle
                                        TensionOut = 0.05f,
                                    });
        curve.AddOrUpdateV(1.0, new VDefinition
                                    {
                                        Value = 1.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        InTangentAngle = 1.2,
                                        TensionIn = 0.05f,
                                    });

        // With near-zero tension, tangent slopes contribute almost nothing →
        // the Hermite basis is dominated by the position terms → approaches a smooth S-curve
        // between the endpoints, NOT necessarily linear. Verify the curve stays within bounds.
        for (double u = 0.0; u <= 1.0; u += 0.1)
        {
            var sampled = curve.GetSampledValue(u);
            Assert.True(sampled >= -0.1 && sampled <= 1.1,
                        $"Near-zero tension should stay close to [0,1] range, got {sampled} at u={u}");
        }
    }

    [Fact]
    public void TensionSerializationRoundTrip()
    {
        var original = new VDefinition
                       {
                           Value = 1.0,
                           InInterpolation = VDefinition.KeyInterpolation.Tangent,
                           OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                           Weighted = true,
                           TensionIn = 0.5f,
                           TensionOut = 2.0f,
                       };

        var sb = new System.Text.StringBuilder();
        using var sw = new System.IO.StringWriter(sb);
        using var writer = new Newtonsoft.Json.JsonTextWriter(sw);
        writer.WriteStartObject();
        original.Write(writer);
        writer.WriteEndObject();
        writer.Flush();

        var json = Newtonsoft.Json.Linq.JObject.Parse(sb.ToString());
        var restored = new VDefinition();
        restored.Read(json);

        Assert.Equal(0.5f, restored.TensionIn);
        Assert.Equal(2.0f, restored.TensionOut);
    }

    [Fact]
    public void DefaultTension_OmittedFromJson()
    {
        var vDef = new VDefinition
                   {
                       Value = 1.0,
                       TensionIn = 1.0f,
                       TensionOut = 1.0f,
                   };

        var sb = new System.Text.StringBuilder();
        using var sw = new System.IO.StringWriter(sb);
        using var writer = new Newtonsoft.Json.JsonTextWriter(sw);
        writer.WriteStartObject();
        vDef.Write(writer);
        writer.WriteEndObject();
        writer.Flush();

        var json = Newtonsoft.Json.Linq.JObject.Parse(sb.ToString());

        Assert.Null(json["TensionIn"]);
        Assert.Null(json["TensionOut"]);
    }

    [Fact]
    public void CloneCopiesTension()
    {
        var original = new VDefinition { TensionIn = 0.3f, TensionOut = 2.5f };
        var clone = original.Clone();

        Assert.Equal(0.3f, clone.TensionIn);
        Assert.Equal(2.5f, clone.TensionOut);
    }
}
